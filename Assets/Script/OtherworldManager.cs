using UnityEngine;

public class OtherworldManager : MonoBehaviour
{
    public static OtherworldManager Instance;

    [Header("異空間設定")]
    [Tooltip("把你在光學迷宮設定的多個出生點 (空物件) 拖進來")]
    public Transform[] mazeSpawnPoints;

    [Tooltip("在迷宮中，每秒鐘要扣除多少理智值？")]
    public float sanityDrainPerSecond = 1f;

    // ==========================================
    // 【新增】：Fonia 怪物連動
    // ==========================================
    [Header("怪物連動")]
    [Tooltip("請把場景中的 Fonia 怪物本體拖曳到這裡")]
    public FoniaAI foniaMonster;

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
        if (isInOtherworld && playerSanity != null)
        {
            drainTimer += Time.deltaTime;
            if (drainTimer >= 1f)
            {
                playerSanity.TakeDamage(sanityDrainPerSecond);
                drainTimer = 0f;
            }
        }
    }

    public void SendToOtherworld(GameObject player)
    {
        if (mazeSpawnPoints == null || mazeSpawnPoints.Length == 0) return;

        isInOtherworld = true;
        playerSanity = player.GetComponent<PlayerSanity>();

        savedMainWorldPosition = player.transform.position;
        savedMainWorldRotation = player.transform.rotation;

        int randomIndex = Random.Range(0, mazeSpawnPoints.Length);
        Transform spawnPoint = mazeSpawnPoints[randomIndex];

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        if (cc != null) cc.enabled = true;

        // ==========================================
        // 【新增】：玩家進入異空間，立刻喚醒 Fonia！
        // ==========================================
        if (foniaMonster != null) foniaMonster.ActivateFonia();

        Debug.Log($"<color=purple>[異空間]</color> 玩家被傳送到迷宮的第 {randomIndex + 1} 號出生點了！");
    }

    public void ReturnToMainWorld(GameObject player)
    {
        if (!isInOtherworld) return;

        isInOtherworld = false;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = savedMainWorldPosition;
        player.transform.rotation = savedMainWorldRotation;

        if (cc != null) cc.enabled = true;

        // ==========================================
        // 【新增】：玩家逃回主世界，強制 Fonia 下線休眠！
        // ==========================================
        if (foniaMonster != null) foniaMonster.DeactivateFonia();

        Debug.Log("<color=green>[異空間]</color> 玩家成功逃脫，返回主世界！");
    }
}