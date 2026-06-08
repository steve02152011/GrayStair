using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class GameEndingTrigger : MonoBehaviour
{
    [Header("結局觸發設定")]
    [Tooltip("黑幕 UI (請放一個掛有 CanvasGroup 的全黑 Image)")]
    public CanvasGroup blackScreen;

    [Tooltip("黑幕淡入需要花費幾秒？")]
    public float fadeDuration = 2.0f;

    // ==========================================
    // 【新增】：UI 隱藏設定
    // ==========================================
    [Header("UI 隱藏設定")]
    [Tooltip("把你在破關時想要隱藏的 UI 物件 (例如：血條、背包、準心、互動提示) 全部拖進來")]
    public GameObject[] uisToHide;

    [Header("影片播放設定")]
    [Tooltip("用來播放通關影片的 Video Player")]
    public VideoPlayer endingVideo;

    [Tooltip("影片播完後要回到哪個場景？(請填寫場景名稱，例如：MainMenu)")]
    public string nextSceneName = "MainMenu";

    private bool isTriggered = false;

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        if (blackScreen != null)
        {
            blackScreen.alpha = 0f;
            blackScreen.blocksRaycasts = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isTriggered && other.CompareTag("Player"))
        {
            isTriggered = true;
            StartCoroutine(EndingRoutine(other.gameObject));
        }
    }

    private IEnumerator EndingRoutine(GameObject player)
    {
        Debug.Log("<color=yellow>[結局]</color> 玩家抵達終點！開始播放結局流程...");

        // ==========================================
        // 【新增】：瞬間隱藏所有遊戲中的 UI
        // ==========================================
        foreach (GameObject ui in uisToHide)
        {
            if (ui != null)
            {
                ui.SetActive(false); // 直接把該 UI 物件關閉
            }
        }

        // 鎖住玩家操作，讓他不能在播結局時亂跑
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 黑幕平滑淡入
        if (blackScreen != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                blackScreen.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            blackScreen.alpha = 1f;
        }

        // 播放結局影片
        if (endingVideo != null)
        {
            endingVideo.Prepare();
            while (!endingVideo.isPrepared)
            {
                yield return null;
            }

            endingVideo.Play();
            yield return new WaitForSeconds(0.5f);

            while (endingVideo.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // 載入主選單
        Debug.Log("<color=green>[結局]</color> 影片播放完畢，準備轉場！");

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneManager.LoadScene(nextSceneName);
        }
    }
}