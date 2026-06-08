using System.Collections.Generic;
using UnityEngine;

public class LaserSensor : MonoBehaviour, ILaserReceiver
{
    [Header("連動設定 (單一感應器控制)")]
    [Tooltip("如果是單一感應器就能開的機關，拖曳到這裡；如果是複數解謎，請保持這裡為空！")]
    public DoorController[] targetDoors;

    [Tooltip("把所有你要控制的『橋樑』拖曳到這個列表裡！")]
    public BridgeController[] targetBridges;

    // ==========================================
    // 【新增】：讓外部的「多重感應器管理器」可以讀取它的狀態
    // ==========================================
    public bool IsActive { get; private set; }

    private float lastHitTime = -1f;
    private float activeDuration = 0.1f;

    void Update()
    {
        // 判斷自己是否正在被雷射照射
        IsActive = (Time.time - lastHitTime) <= activeDuration;

        foreach (DoorController door in targetDoors)
        {
            if (door != null) door.SetDoorState(IsActive);
        }

        foreach (BridgeController bridge in targetBridges)
        {
            if (bridge != null) bridge.SetBridgeState(IsActive);
        }
    }

    public bool ProcessLaser(Vector3 hitPoint, Vector3 hitNormal, Vector3 incomingDir, Collider hitCollider, ref float remainingDistance, List<Vector3> laserPoints, out Vector3 nextStartPoint, out Vector3 nextDirection)
    {
        lastHitTime = Time.time;
        nextStartPoint = Vector3.zero;
        nextDirection = Vector3.zero;
        return false;
    }
}