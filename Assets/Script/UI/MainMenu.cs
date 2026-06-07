using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    void Start()
    {
        // ==========================================
        // 【關鍵防護】：清除上一局的存檔殘留！
        // 只要回到主選單，就強制把上一局的 CheckpointManager 銷毀。
        // 這樣當玩家點擊開始遊戲，進入 "Start" 場景時，
        // 系統就會自動生成一個全新的、乾淨的存檔總部！
        // ==========================================
        if (CheckpointManager.Instance != null)
        {
            Destroy(CheckpointManager.Instance.gameObject);
        }

        // 確保在主選單時，遊戲時間是正常流動的，且滑鼠是可見的
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        // 載入你的遊戲主場景
        SceneManager.LoadScene("Start");
    }
}