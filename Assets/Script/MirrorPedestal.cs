using UnityEngine;

public class MirrorPedestal : MonoBehaviour, IInteractable
{
    // ==========================================
    // 【新增】：存檔系統設定
    // ==========================================
    [Header("存檔系統設定")]
    [Tooltip("請輸入獨一無二的ID，例如 'Pedestal_Room1'")]
    public string uniqueID;

    [Header("基座狀態")]
    [Tooltip("基座上現在有沒有放著鏡子？")]
    public bool hasMirror = false;

    [Header("物件綁定")]
    [Tooltip("放在基座上的『實體鏡子模型』 (請拖曳子物件進來)")]
    public GameObject mirrorModel;

    [Header("預覽視覺設定 (新增)")]
    [Tooltip("半透明的『預覽鏡子模型』 (請複製一個鏡子，換上半透明材質，拖曳進來)")]
    public GameObject ghostMirrorModel;

    [Tooltip("玩家要靠多近看著基座，才會顯示預覽？(請設定與你按F互動一樣的距離)")]
    public float previewDistance = 4f;

    [Header("背包與物品設定")]
    [Tooltip("攜帶式鏡子在 InventoryManager 的 allWeapons 陣列裡是第幾個？")]
    public int mirrorWeaponID = 2;

    private InventoryManager playerInventory;

    void Start()
    {
        // 【新增】：一開局就問總部，我這座台子上次被記錄時是有鏡子還是空的？
        if (CheckpointManager.Instance != null)
        {
            int state = CheckpointManager.Instance.GetPedestalState(uniqueID);
            if (state == 1) hasMirror = true;       // 總部說有鏡子
            else if (state == -1) hasMirror = false; // 總部說沒鏡子
            // 如果是 0 代表總部沒記錄，就照你在 Inspector 裡設定的預設值
        }

        if (mirrorModel != null)
        {
            mirrorModel.SetActive(hasMirror);
        }

        if (ghostMirrorModel != null)
        {
            ghostMirrorModel.SetActive(false);
        }

        playerInventory = FindFirstObjectByType<InventoryManager>();
    }

    void Update()
    {
        HandleGhostPreview();
    }

    private void HandleGhostPreview()
    {
        if (ghostMirrorModel == null || playerInventory == null) return;

        bool canShowPreview = false;

        if (!hasMirror && playerInventory.GetCurrentItemID() == mirrorWeaponID)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, previewDistance))
                {
                    if (hit.collider.gameObject == this.gameObject)
                    {
                        canShowPreview = true;
                    }
                }
            }
        }

        ghostMirrorModel.SetActive(canShowPreview);
    }

    public void OnInteract(Transform interactor)
    {
        if (playerInventory == null)
        {
            playerInventory = interactor.GetComponent<InventoryManager>();
        }

        if (playerInventory == null)
        {
            Debug.LogWarning("<color=red>[基座]</color> 找不到玩家的 InventoryManager！");
            return;
        }

        if (hasMirror)
        {
            bool added = playerInventory.AddItemToInventory(mirrorWeaponID, 1, 1);
            if (added)
            {
                hasMirror = false;
                if (mirrorModel != null) mirrorModel.SetActive(false);

                // 【新增】：玩家拿走鏡子，跟總部報備「我現在變空的了」
                if (CheckpointManager.Instance != null)
                    CheckpointManager.Instance.RecordPedestalState(uniqueID, false);

                Debug.Log("<color=green>[基座]</color> 拿起了攜帶式鏡子！");
            }
        }
        else
        {
            int currentItemID = playerInventory.GetCurrentItemID();
            if (currentItemID == mirrorWeaponID)
            {
                playerInventory.ConsumeCurrentItem();
                hasMirror = true;
                if (mirrorModel != null) mirrorModel.SetActive(true);

                if (ghostMirrorModel != null) ghostMirrorModel.SetActive(false);

                // 【新增】：玩家放上鏡子，跟總部報備「我現在有鏡子了」
                if (CheckpointManager.Instance != null)
                    CheckpointManager.Instance.RecordPedestalState(uniqueID, true);

                Debug.Log("<color=green>[基座]</color> 成功放下了攜帶式鏡子！");
            }
            else
            {
                Debug.Log("<color=orange>[基座]</color> 你必須拿著攜帶式鏡子才能放上去！");
            }
        }
    }
}