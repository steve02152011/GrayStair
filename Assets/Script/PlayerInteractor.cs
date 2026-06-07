using UnityEngine;
using TMPro;

public class PlayerInteractor : MonoBehaviour
{
    [Header("互動設定")]
    [Tooltip("玩家可以按 F 互動的最遠距離")]
    public float interactRange = 3.0f;
    public KeyCode interactKey = KeyCode.F;

    [Header("UI 設定")]
    [Tooltip("請把畫面中央用來顯示提示的 TextMeshPro 拖曳到這裡")]
    public TextMeshProUGUI promptText;

    private Camera playerCamera;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();

        // 遊戲開始時先隱藏提示文字
        if (promptText != null) promptText.enabled = false;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // 1. 發射射線掃描前方
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        bool hitInteractable = Physics.Raycast(ray, out RaycastHit hit, interactRange);
        IInteractable interactableObj = null;

        if (hitInteractable)
        {
            interactableObj = hit.collider.GetComponentInParent<IInteractable>();
        }

        // ================== 動態 UI 顯示邏輯 ==================
        if (interactableObj != null)
        {
            if (promptText != null)
            {
                // 只要看到可互動物件，就一定打開文字顯示
                promptText.enabled = true;

                // 檢查看到了什麼特定的東西？
                MirrorPedestal pedestal = hit.collider.GetComponentInParent<MirrorPedestal>();
                ManualDoor door = hit.collider.GetComponentInParent<ManualDoor>();

                if (pedestal != null)
                {
                    // 【關鍵】：如果是鏡子基座，直接向基座索取最準確的文字狀態！
                    promptText.text = pedestal.GetPromptText();
                }
                else if (door != null && door.targetDoor != null)
                {
                    // 如果是門
                    promptText.text = door.targetDoor.isOpen ? "按 F 關" : "按 F 開";
                }
                else
                {
                    // 如果是其他普通物件 (藥丸、攜帶鏡等)
                    promptText.text = "按 F 互動";
                }
            }

            // ================== 按鍵互動邏輯 ==================
            if (Input.GetKeyDown(interactKey))
            {
                interactableObj.OnInteract(this.transform);
            }
        }
        else
        {
            // 【完美修復】：只要視線一離開任何互動物件，或退後超出距離
            // 立刻毫無懸念地把 UI 關閉，絕對不會卡在畫面上！
            if (promptText != null)
            {
                promptText.enabled = false;
            }
        }
    }
}