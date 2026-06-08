using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("UI 基礎設定")]
    public RectTransform[] uiSlots;
    public Image[] slotImages;

    [Tooltip("沒被選中時的正常比例 (1倍)")]
    public Vector3 normalScale = new Vector3(1f, 1f, 1f);
    [Tooltip("被選中時的放大比例 (例如 1.5 倍)")]
    public Vector3 selectedScale = new Vector3(1.5f, 1.5f, 1f);
    public float uiAnimationSpeed = 15f;

    [Header("UI 自動隱藏設定")]
    [Tooltip("請把包住 4 個格子的父物件 (需掛載 CanvasGroup) 拖進來")]
    public CanvasGroup inventoryCanvasGroup;
    [Tooltip("閒置幾秒後開始隱藏")]
    public float hideDelay = 4f;
    [Tooltip("淡出的速度")]
    public float fadeOutSpeed = 2f;

    // 用來記錄已經閒置了幾秒
    private float idleTimer = 0f;

    [Header("動態圖示設定")]
    public Image[] slotIcons;
    public Sprite[] itemSprites;

    [Tooltip("請在 4 個格子底下各建立一個 TextMeshPro，用來顯示數量，並拖進來")]
    public TextMeshProUGUI[] stackTexts;

    [Header("武器資料庫")]
    public GameObject[] allWeapons;

    private int[] _slots = new int[4] { -1, -1, -1, -1 };
    private int[] _itemCounts = new int[4] { 0, 0, 0, 0 };
    private int currentIndex = 0;

    void Start()
    {
        RefreshInventoryUI();
        EquipWeapon();
        ShowUI(); // 遊戲一開始先顯示一下
    }

    void Update()
    {
        HandleScrollInput();
        HandleNumberInput();
        UpdateUISmoothly();
        HandleUIFade(); // 處理淡出邏輯
    }

    private void HandleScrollInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = 3;
            EquipWeapon();
            ShowUI(); // 滾動時瞬間顯示
        }
        else if (scroll < 0f)
        {
            currentIndex++;
            if (currentIndex > 3) currentIndex = 0;
            EquipWeapon();
            ShowUI(); // 滾動時瞬間顯示
        }
    }

    private void HandleNumberInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { SelectSlot(0); ShowUI(); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { SelectSlot(1); ShowUI(); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { SelectSlot(2); ShowUI(); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { SelectSlot(3); ShowUI(); }
    }

    private void SelectSlot(int index)
    {
        currentIndex = index;
        EquipWeapon();
    }

    // ================== 核心新增：顯示與淡出邏輯 ==================
    private void ShowUI()
    {
        idleTimer = 0f; // 重置閒置計時器
        if (inventoryCanvasGroup != null)
        {
            inventoryCanvasGroup.alpha = 1f; // 瞬間完全顯示
        }
    }

    private void HandleUIFade()
    {
        if (inventoryCanvasGroup == null) return;

        idleTimer += Time.deltaTime; // 每一幀增加閒置時間

        // 如果閒置時間超過了設定的 4 秒，就開始平滑淡出
        if (idleTimer > hideDelay)
        {
            inventoryCanvasGroup.alpha = Mathf.Lerp(inventoryCanvasGroup.alpha, 0f, Time.deltaTime * fadeOutSpeed);
        }
    }
    // ==============================================================

    private void UpdateUISmoothly()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            Vector3 targetScale = (i == currentIndex) ? selectedScale : normalScale;
            uiSlots[i].localScale = Vector3.Lerp(uiSlots[i].localScale, targetScale, Time.deltaTime * uiAnimationSpeed);

            if (slotImages[i] != null)
                slotImages[i].color = (slots[i] != -1) ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    private void EquipWeapon()
    {
        int activeWeaponID = slots[currentIndex];
        for (int i = 0; i < allWeapons.Length; i++)
        {
            if (allWeapons[i] != null)
                allWeapons[i].SetActive(i == activeWeaponID);
        }
    }

    private void RefreshInventoryUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int itemID = slots[i];
            if (itemID != -1)
            {
                slotIcons[i].sprite = itemSprites[itemID];
                slotIcons[i].enabled = true;
                Color c = slotIcons[i].color;
                c.a = 1f;
                slotIcons[i].color = c;

                if (stackTexts != null && stackTexts.Length > i && stackTexts[i] != null)
                {
                    stackTexts[i].text = itemCounts[i].ToString();
                    stackTexts[i].enabled = itemCounts[i] > 1;
                }
            }
            else
            {
                slotIcons[i].enabled = false;
                if (stackTexts != null && stackTexts.Length > i && stackTexts[i] != null) stackTexts[i].enabled = false;
            }
        }
    }

    public bool AddItemToInventory(int weaponID, int quantity, int maxStack)
    {
        ShowUI(); // 【新增】：撿到東西時，也要瞬間顯示 UI 讓玩家看！

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == weaponID && itemCounts[i] < maxStack)
            {
                itemCounts[i] += quantity;
                RefreshInventoryUI();
                SelectSlot(i);
                return true;
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == -1)
            {
                slots[i] = weaponID;
                itemCounts[i] = quantity;
                RefreshInventoryUI();
                SelectSlot(i);
                return true;
            }
        }

        Debug.LogWarning("<color=red>[背包]</color> 背包已經滿了，無法撿起！");
        return false;
    }

    public void ConsumeCurrentItem()
    {
        ShowUI(); // 【新增】：吃藥時，也要瞬間顯示 UI 更新數量！

        if (slots[currentIndex] != -1)
        {
            itemCounts[currentIndex]--;

            if (itemCounts[currentIndex] <= 0)
            {
                slots[currentIndex] = -1;
                itemCounts[currentIndex] = 0;
            }

            RefreshInventoryUI();
            EquipWeapon();
        }
    }
    // ==============================================================
    // 【新增】：讓外部機關(如基座)確認玩家現在手上拿著什麼 ID 的物品
    // ==============================================================
    public int GetCurrentItemID()
    {
        // 回傳當前選中格子的物品 ID
        return slots[currentIndex];
    }
    // ==============================================================
    // 【新增】：支援存檔系統的資料傳遞通道
    // ==============================================================

    // 讓存檔系統能公開讀取私有陣列
    public int[] slots { get { return _slots; } } // 將原本的 private int[] slots 改名為 _slots
    public int[] itemCounts { get { return _itemCounts; } } // 將原本的 private int[] itemCounts 改名為 _itemCounts

    // 讓存檔系統知道玩家現在選哪一格
    public int GetCurrentSlotIndex()
    {
        return currentIndex;
    }

    // 接收存檔系統傳回來的資料，並強制更新 UI 和手上裝備
    public void RestoreInventorySnapshot(int[] savedSlots, int[] savedCounts, int savedIndex)
    {
        System.Array.Copy(savedSlots, _slots, 4);
        System.Array.Copy(savedCounts, _itemCounts, 4);
        currentIndex = savedIndex;

        RefreshInventoryUI();
        EquipWeapon();
        ShowUI();
    }
}