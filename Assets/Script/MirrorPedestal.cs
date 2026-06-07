using UnityEngine;

public class MirrorPedestal : MonoBehaviour, IInteractable
{
    [Header("存檔系統設定")]
    public string uniqueID;

    [Header("基座狀態")]
    public bool hasMirror = false;

    [Header("物件綁定")]
    public GameObject mirrorModel;
    public GameObject ghostMirrorModel;

    [Header("互動設定")]
    [Tooltip("玩家可以按互動鍵的最遠距離")]
    public float interactRange = 3.0f; // 建議與 PlayerInteractor 保持一致
    public KeyCode interactKey = KeyCode.F;

    [Header("背包與物品設定")]
    public int mirrorWeaponID = 2;

    private InventoryManager playerInventory;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        if (CheckpointManager.Instance != null)
        {
            int state = CheckpointManager.Instance.GetPedestalState(uniqueID);
            if (state == 1) hasMirror = true;
            else if (state == -1) hasMirror = false;
        }

        if (mirrorModel != null) mirrorModel.SetActive(hasMirror);
        if (ghostMirrorModel != null) ghostMirrorModel.SetActive(false);

        playerInventory = FindFirstObjectByType<InventoryManager>();
    }

    void Update()
    {
        if (playerInventory == null || mainCam == null) return;

        bool isLookingAtMe = false;
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            // 確保射線打到的是這個基座 (包含子物件的 Collider)
            MirrorPedestal hitPedestal = hit.collider.GetComponentInParent<MirrorPedestal>();
            if (hitPedestal == this)
            {
                isLookingAtMe = true;
            }
        }

        // 處理半透明預覽模型
        bool canShowPreview = isLookingAtMe && !hasMirror && (playerInventory.GetCurrentItemID() == mirrorWeaponID);
        if (ghostMirrorModel != null) ghostMirrorModel.SetActive(canShowPreview);
    }

    // ==========================================
    // 【新增】：專門準備給 PlayerInteractor 讀取的文字
    // ==========================================
    public string GetPromptText()
    {
        string keyName = interactKey.ToString();

        if (hasMirror)
        {
            return $"按 {keyName} 取下鏡子、Q或E 旋轉基座";
        }
        else
        {
            if (playerInventory != null && playerInventory.GetCurrentItemID() == mirrorWeaponID)
            {
                return $"按 {keyName} 裝上鏡子、Q或E 旋轉基座";
            }
            else
            {
                return "Q或E 旋轉基座";
            }
        }
    }

    public void OnInteract(Transform interactor)
    {
        if (playerInventory == null) playerInventory = interactor.GetComponent<InventoryManager>();
        if (playerInventory == null) return;

        if (hasMirror)
        {
            if (playerInventory.AddItemToInventory(mirrorWeaponID, 1, 1))
            {
                hasMirror = false;
                if (mirrorModel != null) mirrorModel.SetActive(false);
                if (CheckpointManager.Instance != null) CheckpointManager.Instance.RecordPedestalState(uniqueID, false);
            }
        }
        else
        {
            if (playerInventory.GetCurrentItemID() == mirrorWeaponID)
            {
                playerInventory.ConsumeCurrentItem();
                hasMirror = true;
                if (mirrorModel != null) mirrorModel.SetActive(true);
                if (ghostMirrorModel != null) ghostMirrorModel.SetActive(false);
                if (CheckpointManager.Instance != null) CheckpointManager.Instance.RecordPedestalState(uniqueID, true);
            }
        }
    }
}