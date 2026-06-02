using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class OtherworldExit : MonoBehaviour
{
    void Start()
    {
        // 確保出口是觸發器模式
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // 如果是玩家碰到了出口
        if (other.CompareTag("Player"))
        {
            if (OtherworldManager.Instance != null && OtherworldManager.Instance.isInOtherworld)
            {
                // 呼叫總機，把玩家送回主世界
                OtherworldManager.Instance.ReturnToMainWorld(other.gameObject);
            }
        }
    }
}