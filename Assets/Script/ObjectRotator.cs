using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    // ==========================================
    // 【新增】：存檔系統設定
    // ==========================================
    [Header("存檔系統設定")]
    [Tooltip("請輸入獨一無二的ID，例如 'Rotator_Mirror_01'")]
    public string uniqueID;

    [Header("旋轉設定")]
    [Tooltip("每次按下按鍵要旋轉的角度")]
    public float rotationStep = 15f;

    [Header("互動限制")]
    [Tooltip("玩家需要靠多近才能操作？(單位: 公尺)")]
    public float interactDistance = 2.5f;

    private Camera mainCamera;

    void Start()
    {
        // 遊戲開始時，自動尋找場景中掛有 "MainCamera" 標籤的玩家攝影機
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("<color=red>[錯誤]</color> 找不到玩家攝影機！請確定你的攝影機有設定 'MainCamera' 標籤。");
        }

        // 【新增】：遊戲一開始，去問總部自己上次被轉到了什麼角度？
        if (CheckpointManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            // 如果總部有這顆物件的旋轉紀錄，就把自己轉過去！
            if (CheckpointManager.Instance.TryGetObjectRotation(uniqueID, out Quaternion savedRotation))
            {
                transform.localRotation = savedRotation;
            }
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // 1. 產生一條從「攝影機正中心」往「正前方」發射的隱形射線
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        // 2. 發射射線，並限制它的最遠距離為 interactDistance
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // 3. 如果射線打中了東西，檢查那個東西是不是「我自己 (這個掛著腳本的物件)」
            ObjectRotator target = hit.collider.GetComponent<ObjectRotator>();

            if (target == this)
            {
                // 只有在「距離夠近」且「準心剛好指著我」的時候，才允許按 Q/E
                HandleRotationInput();
            }
        }
    }

    private void HandleRotationInput()
    {
        bool hasRotated = false;

        // 按下 Q 鍵向左轉
        if (Input.GetKeyDown(KeyCode.Q))
        {
            transform.Rotate(0, 0, -rotationStep, Space.Self);
            Debug.Log($"<color=cyan>[物件旋轉]</color> 玩家對準並沿 Z 軸向左轉了 {rotationStep} 度！");
            hasRotated = true;
        }

        // 按下 E 鍵向右轉
        if (Input.GetKeyDown(KeyCode.E))
        {
            transform.Rotate(0, 0, rotationStep, Space.Self);
            Debug.Log($"<color=cyan>[物件旋轉]</color> 玩家對準並沿 Z 軸向右轉了 {rotationStep} 度！");
            hasRotated = true;
        }

        // 【新增】：如果真的有旋轉，立刻跟總部回報最新的角度
        if (hasRotated && CheckpointManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            CheckpointManager.Instance.RecordObjectRotation(uniqueID, transform.localRotation);
        }
    }
}