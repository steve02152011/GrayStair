using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("存檔點設定")]
    [Tooltip("打勾代表這個存檔點踩過一次後就會失效，避免重複一直存檔")]
    public bool triggerOnlyOnce = true;

    private bool isActivated = false;

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // 如果還沒被踩過，且是玩家踩進來
        if (!isActivated && other.CompareTag("Player"))
        {
            // 呼叫全球存檔中心，把玩家現在的狀態存下來
            CheckpointManager.Instance.SaveState(other.gameObject);

            if (triggerOnlyOnce)
            {
                isActivated = true;
            }
        }
    }
}