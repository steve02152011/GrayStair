using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PillPickup : MonoBehaviour
{
    // ==========================================
    // 【新增】：存檔系統設定
    // ==========================================
    [Header("存檔系統設定")]
    [Tooltip("請輸入獨一無二的ID，例如 'Pill_Room1_01'")]
    public string uniqueID;

    [Header("UI 綁定")]
    [Tooltip("玩家靠近時顯示的『按 F 撿起藥瓶』提示群組或文字")]
    public GameObject interactPrompt;

    private bool isPlayerNear = false;
    private InventoryManager playerInventory;

    void Awake()
    {
        // 遊戲一開始先確保提示字是關閉的，避免出現幽靈 UI
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        // 【新增】：遊戲一開始，去問總部自己是不是已經被吃過了？
        if (CheckpointManager.Instance != null && CheckpointManager.Instance.IsItemDestroyed(uniqueID))
        {
            // 如果已經在死掉的名單裡，就立刻銷毀自己，不讓玩家看到！
            Destroy(gameObject);
            return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerInventory = other.transform.root.GetComponentInChildren<InventoryManager>();

            // 玩家靠近，打開提示字
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            playerInventory = null;

            // 玩家離開，關閉提示字
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            if (playerInventory != null)
            {
                // 【關鍵參數】：代表這是一號武器(理智藥)，給 1 瓶，最大可疊加 3 瓶！
                if (playerInventory.AddItemToInventory(1, 1, 3))
                {
                    Debug.Log("<color=green>[撿拾系統]</color> 獲得理智藥！");

                    // 東西被撿走並銷毀前，一定要先把提示字關掉
                    if (interactPrompt != null)
                    {
                        interactPrompt.SetActive(false);
                    }

                    // 【新增】：成功撿起時，打電話給總部，把自己的名字寫進死亡名單
                    if (CheckpointManager.Instance != null)
                    {
                        CheckpointManager.Instance.RecordItemDestroyed(uniqueID);
                    }

                    Destroy(gameObject);
                }
            }
        }
    }
}