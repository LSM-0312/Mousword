using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// Stage 2 (2 - 1 ~ 2 - 5) 자동 생성 도구
//
// 메뉴: Mousword > Stage 2 > Build All
//   1) mousword_stage2_tileset_ver2 로 Tile 에셋 생성 (Assets/Picture/2Stage_Tile)
//   2) mousword_2bg 로 패럴랙스 배경 프리팹 생성 (Background(Stage2).prefab)
//   3) 1-4 씬을 템플릿으로 복사 -> Stage2Layouts/*.txt 레이아웃대로 타일 배치,
//      스폰포인트/문/포탈/몬스터/PlayerChecker/카메라 바운드 설정 -> Assets/Scenes/Map/2Stage 에 저장
//   4) Build Settings 에 2 - 1 ~ 2 - 5 등록
//
// 레이아웃 수정: Assets/Scripts/Editor/Stage2Layouts/*.txt 를 고친 뒤 Build All 다시 실행
//   (씬을 통째로 다시 만드므로 씬에서 직접 손댄 내용은 덮어써짐)
//
// 레이아웃 문자
//   #  돌 벽(Ground). Stage2_Wall Rule Tile 로 칠해져서 상하좌우 이웃으로 외곽선(타일셋 0~3,0~3),
//      대각선 이웃으로 안쪽 꼭짓점(4~7,0~1)이 자동 선택됨
//   1 2  오르막 경사 (2칸에 1칸 상승, 아래는 #)   3 4  내리막 경사
//   1234 / qwer / asdf / zxcv  다이아몬드 바위 (4x4 를 다 쓰면 떠 있는 바위, zxcv 만 쓰면 천장에 매달린 바위)
//      경사 타일은 실제 모양대로 충돌(Physics Shape), 몸통은 칸 전체 충돌
//   =  나무 발판(Platform, 아래에서 통과 가능). 끝이 허공이면 마감 타일이 자동으로 붙음
//      ※ 발판은 공중에 뜨면 안 됨: 한쪽 끝이 벽(#)에 붙어 있거나, T 아래로 기둥이 바닥까지 내려가야 함
//   T  기둥이 연결된 발판 칸   |  기둥   _  기둥 받침(바닥 바로 위)
//   5 6 7 8 9  바닥 돌멩이 장식 (타일셋 8~9열, 충돌 없음)
//   M  시작 시 스폰되는 슬라임(3칸 = 1마리)
//   W  PlayerChecker 진입 시 스폰되는 슬라임(3칸 = 1마리), 다 잡으면 문이 열림
//   S  플레이어 시작 위치
//   D  보스방 입구 문(2칸 폭, 3칸 높이) 왼쪽 아래. 문 위 3칸(천장 홈)은 자동으로 비워짐
//   P  포탈 트리거 왼쪽 아래
//   .  빈 공간
// 헤더
//   @next=다음 씬 이름 (비우면 포탈 비활성)
//   @checker=x,y,w,h  전투방 트리거 (셀 단위)
//   @rocks=x,y x,y ...  (선택) 배경용 다이아몬드 바위(4x4, 충돌 없음) 왼쪽 아래 좌표 목록
public static class Stage2Builder
{
    private const string TemplateScenePath = "Assets/Scenes/Map/1Stage/1 - 4.unity";
    private const string GameManagerSourceScenePath = "Assets/Scenes/Map/1Stage/1 - 1.unity";
    private const string OutputSceneDir = "Assets/Scenes/Map/2Stage";
    private const string LayoutDir = "Assets/Scripts/Editor/Stage2Layouts";
    private const string TilesetPath = "Assets/Picture/mousword_stage2_tileset_ver2.png";
    private const string TileDir = "Assets/Picture/2Stage_Tile";
    private const string PalettePath = "Assets/Picture/2Stage_Tile/2Stage_Tile.prefab";
    private const string WallRuleTilePath = "Assets/Picture/2Stage_Tile/Stage2_Wall.asset";
    private const string Stage1PalettePath = "Assets/Picture/1Stage_Tile/1Stage_Tile.prefab";
    private const string BackgroundDir = "Assets/Prefab/Background/2Stage";
    private const string BackgroundPrefabPath = "Assets/Prefab/Background/Background(Stage2).prefab";
    private const string GameManagerPrefabPath = "Assets/Prefab/GameManager.prefab";
    private const string PlayerPrefabPath = "Assets/Prefab/Player 1(MapScene Test).prefab";
    private const string RockDropClipPath = "Assets/Prefab/Animator/RockDrop.anim";

    private static readonly string[] RoomNames = { "2 - 1", "2 - 2", "2 - 3", "2 - 4", "2 - 5" };

    private const int TileSize = 16;
    private const int DoorWidth = 2;
    private const int DoorHeight = 3;

    // ------------------------------------------------------------------ menu

    [MenuItem("Mousword/Stage 2/Build All (Tiles + Background + Scenes)", priority = 0)]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }
        BuildAllInternal();
    }

    // 배치 모드 실행용: Unity.exe -batchmode -projectPath . -executeMethod Stage2Builder.BuildAllBatch -quit
    public static void BuildAllBatch()
    {
        BuildAllInternal();
    }

    [MenuItem("Mousword/Stage 2/Rebuild Current Room (txt -> scene)", priority = 1)]
    public static void RebuildCurrentRoom()
    {
        string current = SceneManager.GetActiveScene().name;
        if (Array.IndexOf(RoomNames, current) < 0)
        {
            EditorUtility.DisplayDialog("Stage 2", "Stage 2 방(2 - 1 ~ 2 - 5) 씬을 연 상태에서 실행하세요.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Stage 2", $"{current} 씬을 레이아웃 txt 기준으로 다시 만듭니다.\n씬에서 직접 수정한 내용은 사라집니다.", "다시 만들기", "취소"))
        {
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }
        try
        {
            Dictionary<Vector2Int, Tile> tiles = EnsureTiles();
            GameObject gameManagerPrefab = EnsureGameManagerPrefab();
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject backgroundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefabPath);
            BuildRoom(current, Layout.Load($"{LayoutDir}/{current}.txt"), tiles, ReadDoorAnimation(), gameManagerPrefab, playerPrefab, ref backgroundPrefab);
            EditorSceneManager.OpenScene($"{OutputSceneDir}/{current}.unity", OpenSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogError("[Stage2Builder] 실패: " + e);
        }
    }

    [MenuItem("Mousword/Stage 2/Create Tile Palette", priority = 20)]
    public static void CreatePaletteMenu()
    {
        Dictionary<Vector2Int, Tile> tiles = EnsureTiles();
        CreatePalette(tiles);
    }

    [MenuItem("Mousword/Stage 2/Open 2 - 1 and Play", priority = 40)]
    public static void PlayFromFirstRoom()
    {
        string path = $"{OutputSceneDir}/2 - 1.unity";
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Stage 2", "2 - 1 씬이 없습니다. 먼저 Build All 을 실행하세요.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void BuildAllInternal()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Stage 2", "Tiles", 0f);
            Dictionary<Vector2Int, Tile> tiles = EnsureTiles();
            try
            {
                CreatePalette(tiles);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Stage2Builder] 타일 팔레트 생성 실패(씬 생성에는 영향 없음): {e.Message}");
            }

            EditorUtility.DisplayProgressBar("Stage 2", "GameManager prefab", 0.1f);
            GameObject gameManagerPrefab = EnsureGameManagerPrefab();
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                throw new Exception($"플레이어 프리팹을 찾을 수 없음: {PlayerPrefabPath}");
            }

            DoorAnim doorAnim = ReadDoorAnimation();

            if (!AssetDatabase.IsValidFolder(OutputSceneDir))
            {
                AssetDatabase.CreateFolder("Assets/Scenes/Map", "2Stage");
            }

            // 목록에서 빠진 방(예: 2 - 2) 씬 정리
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { OutputSceneDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Array.IndexOf(RoomNames, Path.GetFileNameWithoutExtension(path)) < 0)
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log($"[Stage2Builder] 사용하지 않는 씬 삭제: {path}");
                }
            }

            GameObject backgroundPrefab = null;
            for (int i = 0; i < RoomNames.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Stage 2", $"Building {RoomNames[i]}", 0.2f + 0.7f * i / RoomNames.Length);
                Layout layout = Layout.Load($"{LayoutDir}/{RoomNames[i]}.txt");
                BuildRoom(RoomNames[i], layout, tiles, doorAnim, gameManagerPrefab, playerPrefab, ref backgroundPrefab);
            }

            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene($"{OutputSceneDir}/{RoomNames[0]}.unity", OpenSceneMode.Single);
            Debug.Log("[Stage2Builder] Stage 2 생성 완료: " + string.Join(", ", RoomNames));
        }
        catch (Exception e)
        {
            Debug.LogError("[Stage2Builder] 실패: " + e);
            throw;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ------------------------------------------------------------------ tiles

    private static Dictionary<Vector2Int, Tile> EnsureTiles()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TilesetPath);
        if (texture == null)
        {
            throw new Exception($"타일셋을 찾을 수 없음: {TilesetPath}");
        }

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(TilesetPath).OfType<Sprite>().ToArray();
        if (sprites.Length < 40)
        {
            throw new Exception($"타일셋 슬라이스가 안 되어 있음 (스프라이트 {sprites.Length}개). {TilesetPath} 를 Reimport 해주세요.");
        }

        if (!AssetDatabase.IsValidFolder(TileDir))
        {
            AssetDatabase.CreateFolder("Assets/Picture", "2Stage_Tile");
        }

        var result = new Dictionary<Vector2Int, Tile>();
        foreach (Sprite sprite in sprites)
        {
            int col = Mathf.RoundToInt(sprite.rect.x) / TileSize;
            int row = (texture.height - Mathf.RoundToInt(sprite.rect.y)) / TileSize - 1;
            var key = new Vector2Int(col, row);

            string path = $"{TileDir}/stage2_r{row}c{col}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = GetColliderType(col, row);
            EditorUtility.SetDirty(tile);
            result[key] = tile;
        }
        EnsureWallRuleTile(result);
        AssetDatabase.SaveAssets();
        return result;
    }

    private static Dictionary<Vector2Int, Tile> LoadTiles()
    {
        var result = new Dictionary<Vector2Int, Tile>();
        foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { TileDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path); // stage2_r{row}c{col}
            if (!name.StartsWith("stage2_r"))
            {
                continue;
            }
            int c = name.IndexOf('c', 8);
            int row = int.Parse(name.Substring(8, c - 8));
            int col = int.Parse(name.Substring(c + 1));
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null && tile.sprite != null)
            {
                result[new Vector2Int(col, row)] = tile;
            }
        }
        if (result.Count < 40)
        {
            throw new Exception($"Stage2 타일 로드 실패 ({result.Count}개)");
        }
        return result;
    }

    private static Tile.ColliderType GetColliderType(int col, int row)
    {
        // 다이아몬드 바위의 경사 타일(윗줄/아랫줄)은 스프라이트 Physics Shape(정확한 1:2 경사)로 충돌
        if (col <= 3 && (row == 4 || row == 7))
        {
            return Tile.ColliderType.Sprite;
        }
        // 기둥, 바닥 돌멩이(8~9열)는 장식
        if ((col == 4 && (row == 4 || row == 5)) || col >= 8)
        {
            return Tile.ColliderType.None;
        }
        // 벽 / 바위 몸통 / 나무 발판은 칸 전체 충돌
        return Tile.ColliderType.Grid;
    }

    // 다이아몬드 바위(경사) 타일 문자 -> 타일셋 위치
    //   1 2 / 3 4 : 오르막 / 내리막 경사 (2칸에 1칸 높이)
    //   q w e r / a s d f : 바위 몸통   z x c v : 아래쪽 경사(천장에 매달린 바위, 떠 있는 바위 아랫면)
    // 바닥 돌멩이 장식 (충돌 없음, Deco 타일맵)
    private static readonly Dictionary<char, Vector2Int> PebbleChars = new Dictionary<char, Vector2Int>
    {
        { '5', new Vector2Int(8, 0) }, { '6', new Vector2Int(8, 1) }, { '7', new Vector2Int(9, 1) },
        { '8', new Vector2Int(8, 2) }, { '9', new Vector2Int(9, 2) },
    };

    private static readonly Dictionary<char, Vector2Int> RockChars = new Dictionary<char, Vector2Int>
    {
        { '1', new Vector2Int(0, 4) }, { '2', new Vector2Int(1, 4) }, { '3', new Vector2Int(2, 4) }, { '4', new Vector2Int(3, 4) },
        { 'q', new Vector2Int(0, 5) }, { 'w', new Vector2Int(1, 5) }, { 'e', new Vector2Int(2, 5) }, { 'r', new Vector2Int(3, 5) },
        { 'a', new Vector2Int(0, 6) }, { 's', new Vector2Int(1, 6) }, { 'd', new Vector2Int(2, 6) }, { 'f', new Vector2Int(3, 6) },
        { 'z', new Vector2Int(0, 7) }, { 'x', new Vector2Int(1, 7) }, { 'c', new Vector2Int(2, 7) }, { 'v', new Vector2Int(3, 7) },
    };

    // 벽 Rule Tile: 상하좌우 4방향 This/NotThis 16가지 조합 -> 외곽선 타일
    private static Stage2WallRuleTile EnsureWallRuleTile(Dictionary<Vector2Int, Tile> tiles)
    {
        var rule = AssetDatabase.LoadAssetAtPath<Stage2WallRuleTile>(WallRuleTilePath);
        if (rule == null)
        {
            rule = ScriptableObject.CreateInstance<Stage2WallRuleTile>();
            AssetDatabase.CreateAsset(rule, WallRuleTilePath);
        }
        rule.m_DefaultSprite = tiles[new Vector2Int(1, 1)].sprite;
        rule.m_DefaultColliderType = Tile.ColliderType.Grid;
        rule.m_TilingRules = new List<RuleTile.TilingRule>();
        {
            // 안쪽 꼭짓점: 상하좌우는 모두 벽인데 대각선이 비어 있을 때 (먼저 검사해야 하므로 앞에 둠)
            const int This = RuleTile.TilingRuleOutput.Neighbor.This;
            const int NotThis = RuleTile.TilingRuleOutput.Neighbor.NotThis;
            var eight = new List<Vector3Int>
            {
                Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down,
                new Vector3Int(-1, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(1, -1, 0),
            };
            foreach (KeyValuePair<Vector2Int, bool[]> corner in InnerCorners)
            {
                bool[] open = corner.Value; // { 왼쪽위, 오른쪽위, 왼쪽아래, 오른쪽아래 } 가 비어 있는지
                rule.m_TilingRules.Add(new RuleTile.TilingRule
                {
                    m_Sprites = new[] { tiles[corner.Key].sprite },
                    m_ColliderType = Tile.ColliderType.Grid,
                    m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                    m_NeighborPositions = new List<Vector3Int>(eight),
                    m_Neighbors = new List<int>
                    {
                        This, This, This, This,
                        open[0] ? NotThis : This, open[1] ? NotThis : This, open[2] ? NotThis : This, open[3] ? NotThis : This,
                    },
                });
            }
        }
        var positions = new List<Vector3Int> { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
        for (int mask = 0; mask < 16; mask++)
        {
            bool left = (mask & 1) != 0, right = (mask & 2) != 0, up = (mask & 4) != 0, down = (mask & 8) != 0;
            const int This = RuleTile.TilingRuleOutput.Neighbor.This;
            const int NotThis = RuleTile.TilingRuleOutput.Neighbor.NotThis;
            rule.m_TilingRules.Add(new RuleTile.TilingRule
            {
                m_Sprites = new[] { tiles[AutoTile(left, right, up, down)].sprite },
                m_ColliderType = Tile.ColliderType.Grid,
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_NeighborPositions = new List<Vector3Int>(positions),
                m_Neighbors = new List<int> { left ? This : NotThis, right ? This : NotThis, up ? This : NotThis, down ? This : NotThis },
            });
        }
        // 경사/바위 타일도 벽으로 취급 -> 경사 아래 벽에 불필요한 외곽선이 생기지 않고, 이어지는 곳엔 안쪽 꼭짓점이 붙음
        rule.treatAsWall = RockChars.Values.Where(tiles.ContainsKey).Select(k => (TileBase)tiles[k]).ToArray();
        rule.UpdateNeighborPositions();
        EditorUtility.SetDirty(rule);
        return rule;
    }

    // 안쪽 꼭짓점 타일 (4~7, 0~1): 벽 모서리가 만나는 오목한 곳을 자연스럽게 이어 줌
    //   값 = { 왼쪽위, 오른쪽위, 왼쪽아래, 오른쪽아래 } 대각선이 비어 있는지
    private static readonly Dictionary<Vector2Int, bool[]> InnerCorners = new Dictionary<Vector2Int, bool[]>
    {
        { new Vector2Int(4, 0), new[] { false, false, false, true } },  // 오른쪽아래
        { new Vector2Int(5, 0), new[] { false, false, true, false } },  // 왼쪽아래
        { new Vector2Int(4, 1), new[] { false, true, false, false } },  // 오른쪽위
        { new Vector2Int(5, 1), new[] { true, false, false, false } },  // 왼쪽위
        { new Vector2Int(6, 0), new[] { false, true, false, true } },   // 오른쪽 위+아래
        { new Vector2Int(7, 0), new[] { true, false, true, false } },   // 왼쪽 위+아래
        { new Vector2Int(6, 1), new[] { true, true, false, false } },   // 위쪽 왼+오
        { new Vector2Int(7, 1), new[] { false, false, true, true } },   // 아래쪽 왼+오
    };

    // 4x4 외곽선 오토타일 (상하좌우 이웃 기준)
    //   열: 0 = 왼쪽이 비어 있음, 1 = 양옆 막힘(가운데), 2 = 오른쪽이 비어 있음, 3 = 양옆 다 비어 있음(1칸 기둥)
    //   행: 0 = 위가 비어 있음,   1 = 위아래 막힘(가운데), 2 = 아래가 비어 있음, 3 = 위아래 다 비어 있음(1칸 띠)
    private static Vector2Int AutoTile(bool left, bool right, bool up, bool down)
    {
        int col = left && right ? 1 : (right ? 0 : (left ? 2 : 3));
        int row = up && down ? 1 : (down ? 0 : (up ? 2 : 3));
        return new Vector2Int(col, row);
    }

    private static void CreatePalette(Dictionary<Vector2Int, Tile> tiles)
    {
        if (!File.Exists(PalettePath))
        {
            if (!AssetDatabase.CopyAsset(Stage1PalettePath, PalettePath))
            {
                throw new Exception("1Stage_Tile 팔레트 복사 실패");
            }
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PalettePath);
        try
        {
            root.name = "2Stage_Tile";
            Tilemap tilemap = root.GetComponentInChildren<Tilemap>();
            tilemap.ClearAllTiles();
            foreach (KeyValuePair<Vector2Int, Tile> pair in tiles)
            {
                // 텍스처와 같은 배치 (위쪽 행이 위로)
                tilemap.SetTile(new Vector3Int(pair.Key.x, 7 - pair.Key.y, 0), pair.Value);
            }
            // 벽 Rule Tile (자동 외곽선) - 팔레트 왼쪽 위
            var wallRule = AssetDatabase.LoadAssetAtPath<Stage2WallRuleTile>(WallRuleTilePath);
            if (wallRule != null)
            {
                tilemap.SetTile(new Vector3Int(-2, 7, 0), wallRule);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ------------------------------------------------------------------ prefabs

    private static GameObject EnsureGameManagerPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameManagerPrefabPath);
        if (prefab != null)
        {
            return prefab;
        }

        Scene scene = EditorSceneManager.OpenScene(GameManagerSourceScenePath, OpenSceneMode.Single);
        GameObject source = scene.GetRootGameObjects().FirstOrDefault(g => g.GetComponent<GameManager>() != null);
        if (source == null)
        {
            throw new Exception($"{GameManagerSourceScenePath} 에서 GameManager 를 찾을 수 없음");
        }
        prefab = PrefabUtility.SaveAsPrefabAsset(source, GameManagerPrefabPath);
        // 원본 씬은 저장하지 않음
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        return prefab;
    }

    private static GameObject CreateBackgroundPrefab(Material material)
    {
        // 2bg: 1 = 가장 먼 배경(검은 벽) ... 5 = 가장 가까운 동굴 벽
        string[] sortingLayers = { "Background", "Fourth", "Third", "Second", "First" };
        float[] parallaxX = { 1.0f, 0.9f, 0.8f, 0.6f, 0.4f };
        const float scale = 2.2f; // 192x128px @16ppu = 12x8 유닛 -> 26.4x17.6 (카메라 높이 16 보다 크게)

        var root = new GameObject("Background(Stage2)");
        for (int i = 0; i < 5; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BackgroundDir}/{i + 1}.png");
            if (sprite == null)
            {
                Object.DestroyImmediate(root);
                throw new Exception($"배경 스프라이트를 찾을 수 없음: {BackgroundDir}/{i + 1}.png");
            }

            float width = sprite.bounds.size.x * scale;
            var group = new GameObject($"Layer{i + 1}");
            group.transform.SetParent(root.transform, false);

            ParallaxGroup parallax = group.AddComponent<ParallaxGroup>();
            var so = new SerializedObject(parallax);
            so.FindProperty("parallaxEffectX").floatValue = parallaxX[i];
            so.FindProperty("parallaxEffectY").floatValue = 1f;
            so.FindProperty("backgroundWidth").floatValue = width;
            so.FindProperty("jumpThreshold").floatValue = 1.5f;
            so.FindProperty("offsetY").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();

            int layerId = SortingLayer.NameToID(sortingLayers[i]);
            bool validLayer = SortingLayer.IsValid(layerId);

            for (int k = -1; k <= 1; k++)
            {
                var piece = new GameObject($"{i + 1}_{k + 1}");
                piece.transform.SetParent(group.transform, false);
                piece.transform.localPosition = new Vector3(k * width, 0f, 0f);
                piece.transform.localScale = new Vector3(scale, scale, 1f);
                SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                if (material != null)
                {
                    sr.sharedMaterial = material;
                }
                if (validLayer)
                {
                    sr.sortingLayerID = layerId;
                    sr.sortingOrder = 0;
                }
                else
                {
                    sr.sortingOrder = -100 + i;
                }
            }
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BackgroundPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ------------------------------------------------------------------ room

    private struct DoorAnim
    {
        public float X;
        public float RestY;
        public float ClosedY;
    }

    private static DoorAnim ReadDoorAnimation()
    {
        var anim = new DoorAnim { X = 89f, RestY = 13.91f, ClosedY = 10.97f };
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RockDropClipPath);
        if (clip == null)
        {
            Debug.LogWarning("[Stage2Builder] RockDrop.anim 을 찾지 못해 기본값 사용");
            return anim;
        }
        AnimationCurve x = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"));
        AnimationCurve y = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"));
        if (x != null && x.length > 0)
        {
            anim.X = x.keys[0].value;
        }
        if (y != null && y.length > 0)
        {
            anim.RestY = y.keys[0].value;
            anim.ClosedY = y.keys[y.length - 1].value;
        }
        return anim;
    }

    private static void BuildRoom(string roomName, Layout layout, Dictionary<Vector2Int, Tile> tiles, DoorAnim doorAnim,
        GameObject gameManagerPrefab, GameObject playerPrefab, ref GameObject backgroundPrefab)
    {
        string scenePath = $"{OutputSceneDir}/{roomName}.unity";
        if (File.Exists(scenePath))
        {
            AssetDatabase.DeleteAsset(scenePath);
        }
        if (!AssetDatabase.CopyAsset(TemplateScenePath, scenePath))
        {
            throw new Exception($"템플릿 씬 복사 실패: {TemplateScenePath} -> {scenePath}");
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();

        // 씬을 열면 메모리의 에셋 참조가 언로드될 수 있으므로 타일을 디스크에서 다시 로드
        tiles = LoadTiles();

        // --- 템플릿에서 필요 없는 것 제거 (플레이어/배경은 Stage2 용으로 다시 만듦)
        foreach (GameObject go in roots)
        {
            if (go.GetComponentInChildren<PlayerMove>(true) != null || go.GetComponentInChildren<ParallaxGroup>(true) != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        Transform grid = FindRoot(scene, "Grid").transform;
        foreach (string unused in new[] { "Trap", "Treasure" })
        {
            Transform t = grid.Find(unused);
            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        Tilemap ground = FindTilemap(grid, "Ground");
        Tilemap platform = FindTilemap(grid, "Platform");
        Tilemap foreground = FindTilemap(grid, "ForeGround");
        Tilemap door = FindTilemap(grid, "Door");
        Tilemap monster = FindTilemap(grid, "Monster");
        Tilemap monsterChecker = FindTilemap(grid, "Monster(PlayerChecker)");
        foreach (Tilemap tm in new[] { ground, platform, foreground, door, monster, monsterChecker })
        {
            tm.ClearAllTiles();
        }

        TilemapRenderer groundRenderer = ground.GetComponent<TilemapRenderer>();
        Tilemap deco = CreateDecoTilemap(grid, groundRenderer, "Deco", -1);

        // --- 타일 배치
        Vector2Int d = layout.Find('D');
        // 문이 평소 들어가 있는 천장 홈 (문 위 3칸)은 벽에서 비움
        var doorSlot = new HashSet<Vector2Int>();
        for (int dx = 0; dx < DoorWidth; dx++)
        {
            for (int dy = 0; dy < DoorHeight; dy++)
            {
                doorSlot.Add(new Vector2Int(d.x + dx, d.y + DoorHeight + dy));
            }
        }
        bool Solid(int x, int y)
        {
            if (doorSlot.Contains(new Vector2Int(x, y)))
            {
                return false;
            }
            if (x < 0 || y < 0 || x >= layout.Width || y >= layout.Height)
            {
                return true; // 맵 바깥은 막힌 것으로 취급 -> 외곽 테두리에 윤곽선이 생기지 않음
            }
            char ch = layout.Get(x, y);
            return ch == '#' || RockChars.ContainsKey(ch);
        }
        bool IsPlatform(int x, int y)
        {
            if (x < 0 || y < 0 || x >= layout.Width || y >= layout.Height)
            {
                return false;
            }
            char c = layout.Get(x, y);
            return c == '=' || c == 'T';
        }

        // 벽은 Rule Tile 로 칠함 -> 씬에서 Tile Palette 로 벽을 추가/삭제해도 주변 외곽선이 자동으로 맞춰짐
        var wallRule = AssetDatabase.LoadAssetAtPath<Stage2WallRuleTile>(WallRuleTilePath);
        if (wallRule == null)
        {
            throw new Exception($"벽 Rule Tile 없음: {WallRuleTilePath}");
        }
        // 맵 바깥 2칸도 벽으로 채움 (외곽 테두리에 외곽선이 생기지 않게)
        const int margin = 2;
        for (int y = -margin; y < layout.Height + margin; y++)
        {
            for (int x = -margin; x < layout.Width + margin; x++)
            {
                if (x < 0 || y < 0 || x >= layout.Width || y >= layout.Height)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), wallRule);
                }
            }
        }

        Tile markerTile = tiles[new Vector2Int(1, 1)];
        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                char c = layout.Get(x, y);
                var cell = new Vector3Int(x, y, 0);

                if (RockChars.TryGetValue(c, out Vector2Int rockKey))
                {
                    ground.SetTile(cell, tiles[rockKey]);
                    continue;
                }
                if (Solid(x, y))
                {
                    // 외곽선/안쪽 꼭짓점은 Rule Tile 이 상하좌우·대각선 이웃을 보고 자동 선택
                    ground.SetTile(cell, wallRule);
                    continue;
                }

                switch (c)
                {
                    case '=':
                    {
                        // 끝이 허공이면 마감 타일, 벽에 붙은 쪽/중간은 이어지는 타일
                        bool leftOpen = !IsPlatform(x - 1, y) && !Solid(x - 1, y);
                        bool rightOpen = !IsPlatform(x + 1, y) && !Solid(x + 1, y);
                        Vector2Int key = leftOpen && !rightOpen ? new Vector2Int(4, 2)
                            : rightOpen && !leftOpen ? new Vector2Int(5, 2)
                            : new Vector2Int(5, 3);
                        platform.SetTile(cell, tiles[key]);
                        break;
                    }
                    case 'T': platform.SetTile(cell, tiles[new Vector2Int(4, 3)]); break;
                    case '|': deco.SetTile(cell, tiles[new Vector2Int(4, 4)]); break;
                    case '_': deco.SetTile(cell, tiles[new Vector2Int(4, 5)]); break;
                    case 'M': monster.SetTile(cell, markerTile); break;
                    case '5': case '6': case '7': case '8': case '9':
                        if (tiles.TryGetValue(PebbleChars[c], out Tile pebble))
                        {
                            deco.SetTile(cell, pebble);
                        }
                        break;
                    case 'W': monsterChecker.SetTile(cell, markerTile); break;
                }
            }
        }

        // --- 배경 바위 (다이아몬드 4x4, 충돌 없음, 어둡게) - 벽 뒤에 그려져 일부가 묻힌 것처럼 보임
        Tilemap backDeco = CreateDecoTilemap(grid, groundRenderer, "BackDeco", -2);
        backDeco.color = new Color(0.6f, 0.6f, 0.66f, 1f);
        foreach (Vector2Int rock in layout.Rocks)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    backDeco.SetTile(new Vector3Int(rock.x + i, rock.y + 3 - j, 0), tiles[new Vector2Int(i, 4 + j)]);
                }
            }
        }
        backDeco.CompressBounds();

        foreach (Tilemap tm in new[] { ground, platform, deco, monster, monsterChecker })
        {
            tm.CompressBounds();
            EditorUtility.SetDirty(tm);
        }
        Debug.Log($"[Stage2Builder] {roomName}: {layout.Width}x{layout.Height}, ground {ground.GetUsedTilesCount()} / platform {platform.GetUsedTilesCount()} / tiles loaded {tiles.Count}");

        // --- 스폰 포인트
        Vector2Int s = layout.Find('S');
        FindRoot(scene, "SpawnPoint").transform.position = new Vector3(s.x + 0.5f, s.y + 1.3f, 0f);

        // --- 출구 문 (RockDrop 애니메이션이 절대 좌표를 쓰므로 Door 타일맵 위치는 그대로 두고 셀 오프셋으로 배치)
        //     문은 2x3 독립 블록(외곽선 오토타일)으로 그림. 평소엔 천장 홈에, 닫히면 통로를 막음
        door.transform.localPosition = new Vector3(doorAnim.X, doorAnim.RestY, 0f);
        int offsetX = Mathf.RoundToInt(doorAnim.X);
        int offsetY = Mathf.RoundToInt(doorAnim.ClosedY);
        for (int dx = 0; dx < DoorWidth; dx++)
        {
            for (int dy = 0; dy < DoorHeight; dy++)
            {
                Vector2Int key = AutoTile(dx > 0, dx < DoorWidth - 1, dy < DoorHeight - 1, dy > 0);
                door.SetTile(new Vector3Int(d.x + dx - offsetX, d.y + dy - offsetY, 0), tiles[key]);
            }
        }
        door.CompressBounds();

        GameObject doorCollider = FindRoot(scene, "DoorCollider");
        doorCollider.transform.position = new Vector3(d.x + DoorWidth * 0.5f, d.y + DoorHeight * 0.5f, 0f);
        BoxCollider2D doorBox = doorCollider.GetComponent<BoxCollider2D>();
        doorBox.offset = Vector2.zero;
        doorBox.size = new Vector2(DoorWidth, DoorHeight);
        doorBox.enabled = false;

        FindRoot(scene, "DustParticle").transform.position = new Vector3(d.x + DoorWidth * 0.5f, d.y, 0f);

        // --- 포탈
        Vector2Int p = layout.Find('P');
        GameObject portal = FindRoot(scene, "StagePortal");
        portal.transform.position = new Vector3(p.x + 1f, p.y + DoorHeight * 0.5f, 0f);
        BoxCollider2D portalBox = portal.GetComponent<BoxCollider2D>();
        portalBox.offset = Vector2.zero;
        portalBox.size = new Vector2(2f, DoorHeight);
        var portalSo = new SerializedObject(portal.GetComponent<StagePortal>());
        portalSo.FindProperty("nextSceneName").stringValue = layout.Next;
        portalSo.FindProperty("targetSpawnPointName").stringValue = "SpawnPoint";
        portalSo.ApplyModifiedPropertiesWithoutUndo();
        if (string.IsNullOrEmpty(layout.Next))
        {
            // 다음 스테이지가 아직 없으므로 포탈 비활성 (Stage 3 생기면 nextSceneName 설정 후 활성화)
            portal.SetActive(false);
        }

        // --- 전투방 PlayerChecker (진입 시 문 닫힘 + W 몬스터 스폰, 다 잡으면 문 열림)
        GameObject checker = FindRoot(scene, "PlayerChecker");
        GameObject checkerSpawner = FindRoot(scene, "MonsterManager (PlayerChecker)");
        if (layout.HasChecker)
        {
            RectInt r = layout.Checker;
            checker.transform.position = new Vector3(r.x + r.width * 0.5f, r.y + r.height * 0.5f, 0f);
            BoxCollider2D box = checker.GetComponent<BoxCollider2D>();
            box.offset = Vector2.zero;
            box.size = new Vector2(r.width, r.height);
        }
        else
        {
            Object.DestroyImmediate(checker);
            Object.DestroyImmediate(checkerSpawner);
            Object.DestroyImmediate(monsterChecker.gameObject);
        }

        // --- 카메라 바운드 (맵 외곽에서 1칸 안쪽)
        GameObject cameraBound = FindRoot(scene, "CameraBound");
        cameraBound.transform.position = Vector3.zero;
        PolygonCollider2D poly = cameraBound.GetComponent<PolygonCollider2D>();
        poly.offset = Vector2.zero;
        poly.pathCount = 1;
        poly.SetPath(0, new[]
        {
            new Vector2(1, 1), new Vector2(layout.Width - 1, 1),
            new Vector2(layout.Width - 1, layout.Height - 1), new Vector2(1, layout.Height - 1),
        });

        Vector3 cameraStart = new Vector3(s.x + 0.5f, s.y + 3f, -10f);
        FindRoot(scene, "Main Camera").transform.position = cameraStart;
        FindRoot(scene, "CinemachineCamera").transform.position = cameraStart;

        // --- 배경
        if (backgroundPrefab == null)
        {
            backgroundPrefab = CreateBackgroundPrefab(groundRenderer.sharedMaterial);
        }
        var background = (GameObject)PrefabUtility.InstantiatePrefab(backgroundPrefab, scene);
        background.transform.position = cameraStart + new Vector3(0f, 0f, 10f);

        // --- 테스트용 부트스트랩 (GameManager / 플레이어 자동 생성)
        var bootstrapGo = new GameObject("SceneTestBootstrap");
        SceneManager.MoveGameObjectToScene(bootstrapGo, scene);
        SceneTestBootstrap bootstrap = bootstrapGo.AddComponent<SceneTestBootstrap>();
        var bootstrapSo = new SerializedObject(bootstrap);
        bootstrapSo.FindProperty("gameManagerPrefab").objectReferenceValue = gameManagerPrefab;
        bootstrapSo.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
        bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new Exception($"씬 저장 실패: {scenePath}");
        }
    }

    private static Tilemap CreateDecoTilemap(Transform grid, TilemapRenderer reference, string name, int orderOffset)
    {
        Transform existing = grid.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
        var go = new GameObject(name);
        go.transform.SetParent(grid, false);
        Tilemap tilemap = go.AddComponent<Tilemap>();
        TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
        renderer.sharedMaterial = reference.sharedMaterial;
        renderer.sortingLayerID = reference.sortingLayerID;
        renderer.sortingOrder = reference.sortingOrder + orderOffset;
        go.transform.SetSiblingIndex(0);
        return tilemap;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        GameObject go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
        if (go == null)
        {
            throw new Exception($"템플릿 씬에서 '{name}' 오브젝트를 찾을 수 없음");
        }
        return go;
    }

    private static Tilemap FindTilemap(Transform grid, string name)
    {
        Transform t = grid.Find(name);
        Tilemap tm = t != null ? t.GetComponent<Tilemap>() : null;
        if (tm == null)
        {
            throw new Exception($"Grid 아래에서 '{name}' 타일맵을 찾을 수 없음");
        }
        return tm;
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
            .Where(sc => !sc.path.StartsWith(OutputSceneDir + "/") || Array.IndexOf(RoomNames, Path.GetFileNameWithoutExtension(sc.path)) >= 0)
            .ToList();
        foreach (string room in RoomNames)
        {
            string path = $"{OutputSceneDir}/{room}.unity";
            if (scenes.All(sc => sc.path != path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ------------------------------------------------------------------ layout

    private class Layout
    {
        public int Width;
        public int Height;
        public string Next = "";
        public bool HasChecker;
        public List<Vector2Int> Rocks = new List<Vector2Int>();
        public RectInt Checker;
        private string[] rows; // rows[0] = 맨 아래 줄 (y = 0)

        public char Get(int x, int y) => rows[y][x];

        public Vector2Int Find(char c)
        {
            for (int y = 0; y < Height; y++)
            {
                int x = rows[y].IndexOf(c);
                if (x >= 0)
                {
                    return new Vector2Int(x, y);
                }
            }
            throw new Exception($"레이아웃에 '{c}' 가 없음");
        }

        public static Layout Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception($"레이아웃 파일 없음: {path}");
            }
            var layout = new Layout();
            var mapLines = new List<string>();
            bool inMap = false;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.TrimEnd('\r');
                if (inMap)
                {
                    if (line.Length > 0)
                    {
                        mapLines.Add(line);
                    }
                    continue;
                }
                if (line == "@map")
                {
                    inMap = true;
                    continue;
                }
                if (!line.StartsWith("@"))
                {
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq < 0)
                {
                    continue;
                }
                string key = line.Substring(1, eq - 1).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key == "next")
                {
                    layout.Next = value;
                }
                else if (key == "rocks")
                {
                    foreach (string pair in value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        int[] v = pair.Split(',').Select(int.Parse).ToArray();
                        layout.Rocks.Add(new Vector2Int(v[0], v[1]));
                    }
                }
                else if (key == "checker" && value.Length > 0)
                {
                    int[] v = value.Split(',').Select(int.Parse).ToArray();
                    layout.Checker = new RectInt(v[0], v[1], v[2], v[3]);
                    layout.HasChecker = true;
                }
            }
            if (mapLines.Count == 0)
            {
                throw new Exception($"레이아웃에 @map 이 비어 있음: {path}");
            }
            mapLines.Reverse();
            layout.rows = mapLines.ToArray();
            layout.Height = mapLines.Count;
            layout.Width = mapLines[0].Length;
            if (mapLines.Any(l => l.Length != layout.Width))
            {
                throw new Exception($"레이아웃 줄 길이가 서로 다름: {path}");
            }
            return layout;
        }
    }
}
