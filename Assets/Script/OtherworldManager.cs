using UnityEngine;

public class OtherworldManager : MonoBehaviour
{
    public static OtherworldManager Instance;

    [Header("異空間設定")]
    [Tooltip("把你在光學迷宮設定的多個出生點 (空物件) 拖進來")]
    public Transform[] mazeSpawnPoints;

    [Tooltip("在迷宮中，每秒鐘要扣除多少理智值？")]
    public float sanityDrainPerSecond = 1f;

    [Header("系統監控 (請勿手動修改)")]
    public bool isInOtherworld = false;
    public Vector3 savedMainWorldPosition;
    public Quaternion savedMainWorldRotation;

    private PlayerSanity playerSanity;
    private float drainTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // 如果玩家在異空間，就會像中毒一樣，每一秒鐘扣除一次微量的理智值
        if (isInOtherworld && playerSanity != null)
        {
            drainTimer += Time.deltaTime;
            if (drainTimer >= 1f)
            {
                playerSanity.TakeDamage(sanityDrainPerSecond); // 呼叫你原本的扣血方法
                drainTimer = 0f;
            }
        }
    }

    // ==========================================
    // 傳送玩家到迷宮
    // ==========================================
    public void SendToOtherworld(GameObject player)
    {
        if (mazeSpawnPoints == null || mazeSpawnPoints.Length == 0)
        {
            Debug.LogError("<color=red>[異空間]</color> 傳送失敗！你沒有設定迷宮的出生點！");
            return;
        }

        isInOtherworld = true;
        playerSanity = player.GetComponent<PlayerSanity>();

        // 1. 記下主世界的精準位置與視角
        savedMainWorldPosition = player.transform.position;
        savedMainWorldRotation = player.transform.rotation;

        // 2. 隨機抽選一個光學迷宮的出生點
        int randomIndex = Random.Range(0, mazeSpawnPoints.Length);
        Transform spawnPoint = mazeSpawnPoints[randomIndex];

        // 3. 執行傳送 (?? 必須先暫時關閉 CharacterController 才能用程式改座標)
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        if (cc != null) cc.enabled = true;

        Debug.Log($"<color=purple>[異空間]</color> 玩家被傳送到迷宮的第 {randomIndex + 1} 號出生點了！");
    }

    // ==========================================
    // 讓玩家逃回主世界
    // ==========================================
    public void ReturnToMainWorld(GameObject player)
    {
        if (!isInOtherworld) return;

        isInOtherworld = false;

        // 執行傳送，把玩家放回主世界剛剛消失的地方
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = savedMainWorldPosition;
        player.transform.rotation = savedMainWorldRotation;

        if (cc != null) cc.enabled = true;

        Debug.Log("<color=green>[異空間]</color> 玩家成功逃脫，返回主世界！");
    }
}