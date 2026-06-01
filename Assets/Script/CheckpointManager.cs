using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [Header("目前的存檔紀錄 (記憶體暫存)")]
    public bool hasCheckpoint = false;

    // ==========================================
    // 1. 玩家狀態 
    // ==========================================
    public Vector3 savedPosition;
    public Quaternion savedRotation;
    public float savedSanity;
    public int[] savedSlots = new int[4];
    public int[] savedItemCounts = new int[4];
    public int savedCurrentIndex;

    // ==========================================
    // 2. 環境狀態 (記帳本)
    // ==========================================
    // 草稿區
    private List<string> tempDestroyedItems = new List<string>();
    private List<string> tempActivatedLasers = new List<string>();
    private List<string> tempPedestalsWithMirror = new List<string>();
    private List<string> tempPedestalsEmpty = new List<string>();
    // 【新增】：用字典來記錄物件的專屬角度 (ID 對應 角度)
    private Dictionary<string, Quaternion> tempRotations = new Dictionary<string, Quaternion>();

    // 存檔區
    public List<string> savedDestroyedItems = new List<string>();
    public List<string> savedActivatedLasers = new List<string>();
    public List<string> savedPedestalsWithMirror = new List<string>();
    public List<string> savedPedestalsEmpty = new List<string>();
    // 【新增】：正式存檔的旋轉角度
    public Dictionary<string, Quaternion> savedRotations = new Dictionary<string, Quaternion>();
    // ==========================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ==========================================
        // 【關鍵修復】：無條件清空上一條命的草稿！
        // 不管有沒有踩過存檔點，只要場景重載 (玩家死亡復活)，
        // 都必須強制把「草稿 (Temp)」退回「正式存檔 (Saved)」的狀態。
        // (如果還沒存過檔，Saved 是空的，就會完美清空 Temp！)
        // ==========================================
        tempDestroyedItems = new List<string>(savedDestroyedItems);
        tempActivatedLasers = new List<string>(savedActivatedLasers);
        tempPedestalsWithMirror = new List<string>(savedPedestalsWithMirror);
        tempPedestalsEmpty = new List<string>(savedPedestalsEmpty);
        tempRotations = new Dictionary<string, Quaternion>(savedRotations);

        // 只有在「確實有存過檔」的情況下，才去強行把玩家拉到存檔點並還原背包
        if (hasCheckpoint)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) RestoreState(player);
        }
    }

    public void SaveState(GameObject player)
    {
        hasCheckpoint = true;

        savedPosition = player.transform.position;
        savedRotation = player.transform.rotation;

        PlayerSanity sanity = player.GetComponent<PlayerSanity>();
        if (sanity != null) savedSanity = sanity.currentSanity;

        InventoryManager inv = player.GetComponentInChildren<InventoryManager>();
        if (inv != null)
        {
            System.Array.Copy(inv.slots, savedSlots, 4);
            System.Array.Copy(inv.itemCounts, savedItemCounts, 4);
            savedCurrentIndex = inv.GetCurrentSlotIndex();
        }

        // 將當前的草稿進度，正式寫入存檔區
        savedDestroyedItems = new List<string>(tempDestroyedItems);
        savedActivatedLasers = new List<string>(tempActivatedLasers);
        savedPedestalsWithMirror = new List<string>(tempPedestalsWithMirror);
        savedPedestalsEmpty = new List<string>(tempPedestalsEmpty);

        // 【新增】：寫入旋轉角度到存檔
        savedRotations = new Dictionary<string, Quaternion>(tempRotations);

        Debug.Log("<color=green>[存檔系統]</color> 已記錄玩家與場景環境狀態！");
    }

    public void RestoreState(GameObject player)
    {
        if (!hasCheckpoint) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = savedPosition;
        player.transform.rotation = savedRotation;
        if (cc != null) cc.enabled = true;

        PlayerSanity sanity = player.GetComponent<PlayerSanity>();
        if (sanity != null) sanity.currentSanity = savedSanity;

        InventoryManager inv = player.GetComponentInChildren<InventoryManager>();
        if (inv != null) inv.RestoreInventorySnapshot(savedSlots, savedItemCounts, savedCurrentIndex);

        Debug.Log("<color=cyan>[存檔系統]</color> 玩家已在存檔點復活！");
    }

    // ==========================================
    // 專屬各機關的 API 
    // ==========================================
    public void RecordItemDestroyed(string id) { if (!tempDestroyedItems.Contains(id)) tempDestroyedItems.Add(id); }
    public bool IsItemDestroyed(string id) { return tempDestroyedItems.Contains(id); }

    public void RecordLaserActivated(string id) { if (!tempActivatedLasers.Contains(id)) tempActivatedLasers.Add(id); }
    public bool IsLaserActivated(string id) { return tempActivatedLasers.Contains(id); }

    public void RecordPedestalState(string id, bool hasMirror)
    {
        if (hasMirror)
        {
            if (!tempPedestalsWithMirror.Contains(id)) tempPedestalsWithMirror.Add(id);
            if (tempPedestalsEmpty.Contains(id)) tempPedestalsEmpty.Remove(id);
        }
        else
        {
            if (!tempPedestalsEmpty.Contains(id)) tempPedestalsEmpty.Add(id);
            if (tempPedestalsWithMirror.Contains(id)) tempPedestalsWithMirror.Remove(id);
        }
    }

    public int GetPedestalState(string id)
    {
        if (tempPedestalsWithMirror.Contains(id)) return 1;
        if (tempPedestalsEmpty.Contains(id)) return -1;
        return 0;
    }

    // ==========================================
    // 【新增】：專門記錄與讀取物件的旋轉角度 API
    // ==========================================
    public void RecordObjectRotation(string id, Quaternion rotation)
    {
        if (tempRotations.ContainsKey(id)) tempRotations[id] = rotation; // 如果已經有記錄了，就覆寫新角度
        else tempRotations.Add(id, rotation); // 如果是第一次轉，就新增一筆
    }

    public bool TryGetObjectRotation(string id, out Quaternion savedRotation)
    {
        return tempRotations.TryGetValue(id, out savedRotation);
    }
}