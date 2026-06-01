using UnityEngine;
using UnityEngine.SceneManagement; // 用來重新載入場景

[RequireComponent(typeof(BoxCollider))]
public class VoidDeathTrigger : MonoBehaviour
{
    public enum DeathAction { TeleportToRespawn, DealMassiveDamage, ReloadScene }

    [Header("虛空懲罰設定")]
    [Tooltip("玩家掉進去後要發生什麼事？")]
    public DeathAction deathAction = DeathAction.TeleportToRespawn;

    [Header("重生點設定 (如果選 TeleportToRespawn)")]
    [Tooltip("請把場景中的一個空物件 (RespawnPoint) 拖曳到這裡")]
    public Transform respawnPoint;

    void Start()
    {
        // 確保一定是觸發器模式
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // 檢查掉下來的是不是玩家
        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=red>[虛空]</color> 玩家掉出地圖了！執行懲罰！");

            switch (deathAction)
            {
                // 【選項 1】：讀取最後一個 Checkpoint (替換原本的 TeleportToRespawn)
                case DeathAction.TeleportToRespawn:
                    if (CheckpointManager.Instance != null && CheckpointManager.Instance.hasCheckpoint)
                    {
                        // 呼叫中心還原狀態！
                        CheckpointManager.Instance.RestoreState(other.gameObject);
                    }
                    else if (respawnPoint != null)
                    {
                        // 如果玩家還沒踩過任何存檔點就掉下去了，使用備用的初始復活點
                        CharacterController cc = other.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        other.transform.position = respawnPoint.position;
                        other.transform.rotation = respawnPoint.rotation;
                        if (cc != null) cc.enabled = true;
                    }
                    break;

                // 【選項 2】：直接透過你的 PlayerSanity 系統扣除極大傷害 (觸發你的正常死亡邏輯)
                case DeathAction.DealMassiveDamage:
                    PlayerSanity sanity = other.GetComponent<PlayerSanity>();
                    if (sanity != null)
                    {
                        sanity.TakeDamage(9999f); // 受到 9999 點傷害，保證死透
                    }
                    else
                    {
                        Debug.LogError("<color=red>[虛空]</color> 找不到玩家身上的 PlayerSanity 腳本！");
                    }
                    break;

                // 【選項 3】：簡單粗暴，直接重新載入這個關卡 (適合沒有做死亡畫面的初期測試)
                case DeathAction.ReloadScene:
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    break;
            }
        }
        // ==========================================
        // 【防呆加碼】：如果是怪物 (Dolo / Fonia) 掉下去怎麼辦？
        // ==========================================
        else if (other.CompareTag("Enemy") || other.GetComponent<DoloAI>() != null)
        {
            Debug.Log("<color=magenta>[虛空]</color> 怪物掉出地圖了，幫牠解脫！");
            Destroy(other.gameObject);
            // 如果你的怪物需要重生，也可以在這裡改寫成傳送邏輯
        }
    }
}