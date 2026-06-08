using UnityEngine;
using UnityEngine.SceneManagement; // 記得引入這個命名空間

public class SceneChanger : MonoBehaviour
{
    // 這裡填入你隊友關卡場景的精確名稱
    public string gameSceneName = "Formal"; 

    // 供動畫結束時呼叫的方法
    public void LoadGameScene()
    {
        SceneManager.LoadScene("Formal");
    }
}