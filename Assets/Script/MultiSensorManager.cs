using UnityEngine;

public class MultiSensorManager : MonoBehaviour
{
    [Header("通關條件設定")]
    [Tooltip("請把「必須同時被射中」的感應器 (LaserSensor) 全部拖曳到這個陣列裡")]
    public LaserSensor[] requiredSensors;

    [Header("連動機關設定")]
    [Tooltip("條件達成時要開啟的門")]
    public DoorController[] targetDoors;

    [Tooltip("條件達成時要啟動的橋樑")]
    public BridgeController[] targetBridges;

    void Update()
    {
        // 如果沒有設定任何感應器，就直接停止運作防呆
        if (requiredSensors == null || requiredSensors.Length == 0) return;

        bool allActive = true;

        // 檢查陣列裡面的每一個感應器
        foreach (LaserSensor sensor in requiredSensors)
        {
            // 只要有任何一個感應器沒有被打中 (IsActive 為 false)
            if (sensor == null || !sensor.IsActive)
            {
                allActive = false; // 判定為失敗
                break;             // 不用繼續往下檢查了，直接跳出迴圈
            }
        }

        // 根據上面的檢查結果 (allActive)，統一控制所有的門和橋樑
        foreach (DoorController door in targetDoors)
        {
            if (door != null) door.SetDoorState(allActive);
        }

        foreach (BridgeController bridge in targetBridges)
        {
            if (bridge != null) bridge.SetBridgeState(allActive);
        }
    }
}