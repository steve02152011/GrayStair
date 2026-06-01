using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MirrorPickup : MonoBehaviour
{
    // ==========================================
    // 【新增】：存檔系統設定
    // ==========================================
    [Header("存檔系統設定")]
    [Tooltip("請輸入獨一無二的ID，例如 'Mirror_Room1'")]
    public string uniqueID;

    [Header("物品設定")]
    [Tooltip("攜帶式鏡子在背包裡的 ID 是多少？(請跟基座設定的 ID 一致，例如 2)")]
    public int mirrorWeaponID = 2;

    [Header("UI 綁定")]
    [Tooltip("玩家靠近時顯示的『按 F 撿起』提示群組或文字")]
    public GameObject interactPrompt;

    private bool isPlayerNear = false;
    private InventoryManager playerInventory;

    void Awake()
    {
        // 遊戲一開始先確保提示字是關閉的，避免幽靈 UI
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    void Start()
    {
        // 確保碰撞體是觸發器模式
        GetComponent<BoxCollider>().isTrigger = true;

        // 【新增】：遊戲一開始，去問總部自己是不是已經被撿過了？
        if (CheckpointManager.Instance != null && CheckpointManager.Instance.IsItemDestroyed(uniqueID))
        {
            // 如果已經在死掉的名單裡，就立刻銷毀自己，不讓玩家看到！
            Destroy(gameObject);
            return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 檢查撞到我們的是不是玩家
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            // 記住玩家身上的背包系統
            playerInventory = other.transform.root.GetComponentInChildren<InventoryManager>();

            // 打開提示字
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
            }

            Debug.Log("<color=yellow>[撿拾系統]</color> 發現地上的鏡子！可以按 [F] 撿起了！");

            if (playerInventory == null)
            {
                Debug.LogError("<color=red>[嚴重錯誤]</color> 我找不到玩家身上的 InventoryManager 腳本！");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            playerInventory = null; // 玩家離開時清空記憶

            // 關閉提示字
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }

            Debug.Log("<color=grey>[撿拾系統]</color> 玩家離開了鏡子範圍。");
        }
    }

    void Update()
    {
        // 如果玩家在範圍內，且按下了 F 鍵
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            if (playerInventory != null)
            {
                // 嘗試把鏡子塞進背包
                if (playerInventory.AddItemToInventory(mirrorWeaponID, 1, 1))
                {
                    Debug.Log("<color=green>[撿拾系統]</color> 成功撿起攜帶式鏡子並收進背包！");

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

                    // 塞成功了，直接把這個地上的道具徹底刪除！
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning("<color=orange>[撿拾系統]</color> 背包滿了或無法撿起！");
                }
            }
        }
    }
}