using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class MonsterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class MonsterSpawnRule
    {
        public int tileCount;
        public GameObject monsterPrefab;
    }

    //타일맵 설정
    [SerializeField] private Tilemap monsterTilemap;
    [SerializeField] private bool hideTilemap = true;

    //소환 규칙 목록
    [SerializeField] private List<MonsterSpawnRule> spawnRules = new List<MonsterSpawnRule>();

    //소환 타이밍
    [SerializeField] private bool spawnOnStart = false;

    //문 연동
    [SerializeField] private DoorController doorController;

    private bool hasSpawned = false;

    private void Start()
    {
        if (hideTilemap && monsterTilemap != null)
        {
            TilemapRenderer tr = monsterTilemap.GetComponent<TilemapRenderer>();
            if (tr != null)
            {
                tr.enabled = false;
            }
        }

        if (spawnOnStart)
        {
            SpawnMonsters();
        }
    }

    public void SpawnMonsters()
    {
        if (hasSpawned)
        {
            return;
        }

        hasSpawned = true;

        if (monsterTilemap == null)
        {
            return;
        }

        List<List<Vector3Int>> clusters = FindTileClusters();
        List<GameObject> spawnedMonsters = new List<GameObject>();

        foreach (var cluster in clusters)
        {
            int count = cluster.Count;
            GameObject prefab = GetPrefabForTileCount(count);

            if (prefab != null)
            {
                Vector3 spawnPos = CalculateClusterCenter(cluster);
                GameObject monster = Instantiate(prefab, spawnPos, Quaternion.identity);
                spawnedMonsters.Add(monster);
            }
        }

        if (doorController != null && spawnedMonsters.Count > 0)
        {
            
            doorController.TrackMonsters(spawnedMonsters);
        }
    }

    private List<List<Vector3Int>> FindTileClusters()
    {
        List<List<Vector3Int>> clusters = new List<List<Vector3Int>>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        BoundsInt bounds = monsterTilemap.cellBounds;
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.right, Vector3Int.left };

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if(!monsterTilemap.HasTile(pos) || visited.Contains(pos))
                {
                    continue;
                }

                List<Vector3Int> currentCluster = new List<Vector3Int>();
                Queue<Vector3Int> queue = new Queue<Vector3Int>();

                queue.Enqueue(pos);
                visited.Add(pos);

                while(queue.Count > 0)
                {
                    Vector3Int current = queue.Dequeue();
                    currentCluster.Add(current);

                    foreach(var dir in directions)
                    {
                        Vector3Int neighbor = current + dir;
                        if(monsterTilemap.HasTile(neighbor) && !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                clusters.Add(currentCluster);
            }
        }
        return clusters;
    }

    private Vector3 CalculateClusterCenter(List<Vector3Int> cluster)
    {
        Vector3 sumPos = Vector3.zero;
        foreach(var cellPos in cluster)
        {
            sumPos += monsterTilemap.GetCellCenterWorld(cellPos);
        }
        return sumPos / cluster.Count;
    }

    private GameObject GetPrefabForTileCount(int count)
    {
        foreach (var rule in spawnRules)
        {
            if(rule.tileCount == count)
            {
                return rule.monsterPrefab;
            }
        }
        return null;
    }
}

