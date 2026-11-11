using UnityEngine;

// 어떤 방(씬)에서든 바로 Play 해서 테스트할 수 있게 해주는 부트스트랩.
// - GameManager가 없으면 프리팹으로 생성 (StagePortal 이 GameManager.Instance 를 사용하기 때문)
// - "Player" 태그 오브젝트가 없으면 SpawnPoint 위치에 플레이어 생성
//   (이전 씬에서 DontDestroyOnLoad 로 넘어온 플레이어가 있으면 생성하지 않으므로 중복되지 않음)
// - 에디터/개발 빌드 전용 디버그 키
//   F1: SpawnPoint 로 이동 / F2: 보스방 입구 문 앞으로 이동 / F3: 현재 씬 몬스터 전부 제거
[DefaultExecutionOrder(-1000)]
public class SceneTestBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string spawnPointName = "SpawnPoint";
    [SerializeField] private bool enableDebugKeys = true;

    private void Awake()
    {
        if (GameManager.Instance == null && gameManagerPrefab != null)
        {
            GameObject gm = Instantiate(gameManagerPrefab);
            gm.name = gameManagerPrefab.name;
        }

        if (GameObject.FindWithTag("Player") == null && playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, GetSpawnPosition(), Quaternion.identity);
            player.name = playerPrefab.name;
        }
    }

    private Vector3 GetSpawnPosition()
    {
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        return spawnPoint != null ? spawnPoint.transform.position : transform.position;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (!enableDebugKeys)
        {
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Teleport(player, GetSpawnPosition());
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            // 보스방 입구 문 바로 앞(보스방 반대쪽)으로 이동
            GameObject door = GameObject.Find("DoorCollider");
            if (door != null)
            {
                StagePortal portal = FindFirstObjectByType<StagePortal>(FindObjectsInactive.Include);
                bool roomOnRight = portal == null || portal.transform.position.x > door.transform.position.x;
                Teleport(player, door.transform.position + new Vector3(roomOnRight ? -3f : 3f, 0f, 0f));
            }
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            foreach (Monster monster in FindObjectsByType<Monster>(FindObjectsSortMode.None))
            {
                Destroy(monster.gameObject);
            }
        }
    }

    private static void Teleport(GameObject player, Vector3 position)
    {
        player.transform.position = position;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
#endif
}
