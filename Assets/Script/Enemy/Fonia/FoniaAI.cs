using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class FoniaAI : MonoBehaviour
{
    public enum AIState { Stalk, WaitClone, Chase, FleeThenTeleport }

    [Header("目前狀態")]
    public AIState currentState = AIState.Stalk;

    [Header("目標設定")]
    public Transform player;
    public Camera playerCamera;

    [Header("視線判定設定 (藍紅區機制)")]
    [Range(0f, 0.4f)] public float edgeToleranceX = 0.15f;
    [Range(0f, 0.4f)] public float edgeToleranceY = 0.1f;

    [Header("移動與跟蹤設定")]
    public float walkSpeed = 2.0f;
    public float chaseSpeed = 4.5f;
    [Tooltip("初始跟蹤距離")]
    public float stalkDistance = 8f;
    [Tooltip("玩家沒發現時，每秒偷偷拉近多少公尺")]
    public float creepSpeed = 0.6f;
    [Tooltip("被摸到多近會直接暴走追殺 (從背後被捅)")]
    public float killDistance = 2.0f;

    [Header("被抓包瞬移設定 (Weeping Angel 機制)")]
    public float caughtFleeSpeed = 8f;
    public float caughtFleeTime = 0.5f;
    public float teleportMinRadius = 3f;
    public float teleportMaxRadius = 15f;
    public float keepFleeingDistance = 12f;
    public float panicTeleportTime = 5f;

    [Header("分身能力設定")]
    public GameObject clonePrefab;
    public float cloneCooldown = 30f;
    [Range(0f, 1f)]
    public float cloneChance = 0.33f;

    [Header("攻擊力設定")]
    public float damageAmount = 20f;

    [Header("追殺與鎖定設定")]
    [Tooltip("請把 Fonia 臉上的 FaceFocusPoint 空物件拖進來")]
    public Transform faceFocusPoint;
    [Tooltip("強制鎖定畫面的吸力強度 (數字越大，玩家越難把滑鼠移開)")]
    public float cameraLockSpeed = 8f;


    private NavMeshAgent agent;
    private float abilityTimer = 0f;
    private GameObject currentClone;
    private bool isFrozenByPlayer = false;

    // 記錄 Fonia 目前正在用多少距離跟蹤你
    private float currentStalkDistance;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (playerCamera == null) playerCamera = Camera.main;

        // 遊戲一開始，從最遠的距離開始跟蹤
        currentStalkDistance = stalkDistance;

        Debug.Log("<color=white>[Fonia 系統]</color> 幽靈殺手 Fonia 已潛入迷宮...");
    }

    void Update()
    {
        HandleCloneAbility();

        switch (currentState)
        {
            case AIState.Stalk:
                UpdateStalkState();
                break;
            case AIState.WaitClone:
                UpdateWaitCloneState();
                break;
            case AIState.Chase:
                UpdateChaseState();
                break;
            case AIState.FleeThenTeleport:
                break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        Debug.Log($"<color=cyan>[Fonia 大腦]</color> 狀態切換：{currentState} ? <b>{newState}</b>");
        currentState = newState;
    }

    private void HandleCloneAbility()
    {
        if (currentState == AIState.WaitClone || currentState == AIState.Chase || currentState == AIState.FleeThenTeleport) return;
        if (isFrozenByPlayer) return;

        abilityTimer += Time.deltaTime;
        if (abilityTimer >= cloneCooldown)
        {
            abilityTimer = 0f;
            if (Random.value <= cloneChance) CastClone();
        }
    }

    private void CastClone()
    {
        if (clonePrefab == null) return;

        currentClone = Instantiate(clonePrefab, transform.position + transform.forward * 1.5f, transform.rotation);
        FoniaClone cloneScript = currentClone.GetComponent<FoniaClone>();
        if (cloneScript != null) cloneScript.Initialize(this, player);

        agent.isStopped = true;
        ChangeState(AIState.WaitClone);
    }

    public void OnCloneFoundPlayer()
    {
        agent.isStopped = false;
        ChangeState(AIState.Chase);
    }

    public void OnCloneExpired()
    {
        agent.isStopped = false;
        ChangeState(AIState.Stalk);
    }

    // ================== 跟蹤與偷偷逼近機制 ==================

    private void UpdateStalkState()
    {
        agent.speed = walkSpeed;

        if (player == null || playerCamera == null) return;

        // 被摸到背後 2.0 公尺內，直接進入追殺！
        if (Vector3.Distance(transform.position, player.position) <= killDistance)
        {
            ChangeState(AIState.Chase);
            return;
        }

        bool isVisibleToPlayer = false;

        Vector3 foniaEyePos = transform.position + Vector3.up * 1.5f;
        Vector3 viewportPos = playerCamera.WorldToViewportPoint(foniaEyePos);

        if (viewportPos.z > 0)
        {
            if (viewportPos.x > edgeToleranceX && viewportPos.x < (1f - edgeToleranceX) &&
                viewportPos.y > edgeToleranceY && viewportPos.y < (1f - edgeToleranceY))
            {
                Vector3 playerBodyPos = playerCamera.transform.position;
                Vector3 rayDir = (foniaEyePos - playerBodyPos).normalized;
                float distance = Vector3.Distance(playerBodyPos, foniaEyePos);

                if (Physics.Raycast(playerBodyPos, rayDir, out RaycastHit hit, distance))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        isVisibleToPlayer = true;
                    }
                }
            }
        }

        if (isVisibleToPlayer)
        {
            isFrozenByPlayer = true;
            agent.isStopped = true;

            // 如果玩家轉頭看到牠了，代表嚇阻成功！重置逼近距離！
            currentStalkDistance = stalkDistance;

            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
        }
        else
        {
            if (isFrozenByPlayer)
            {
                StartCoroutine(HandleCaughtAndTeleport());
            }
            else
            {
                agent.isStopped = false;

                // 如果玩家一直沒回頭，距離會越來越短！
                currentStalkDistance -= creepSpeed * Time.deltaTime;
                currentStalkDistance = Mathf.Max(currentStalkDistance, 0f); // 確保不會變成負數

                // 動態計算新的目標點 (會越來越貼近玩家的背)
                Vector3 behindPlayerPos = player.position - player.forward * currentStalkDistance;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(behindPlayerPos, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
        }
    }

    private IEnumerator HandleCaughtAndTeleport()
    {
        ChangeState(AIState.FleeThenTeleport);
        isFrozenByPlayer = false;

        Debug.Log("<color=orange>[Fonia 驚嚇]</color> 被抓包了！轉身動態撤退...");

        // 【修改】：讓 NavMeshAgent 接管轉向，這樣她逃跑時就會轉頭面向逃跑的方向！
        agent.updateRotation = true;

        float originalAcceleration = agent.acceleration;
        agent.acceleration = 100f;
        agent.isStopped = false;
        agent.speed = caughtFleeSpeed;

        float disappearTimer = 0f;
        float absoluteTimer = 0f;
        float pathUpdateTimer = 0f;

        while (disappearTimer < caughtFleeTime && absoluteTimer < panicTeleportTime)
        {
            if (player != null)
            {
                pathUpdateTimer -= Time.deltaTime;

                if (pathUpdateTimer <= 0f)
                {
                    Vector3 fleeDir = (transform.position - player.position).normalized;
                    Vector3 fleeTarget = transform.position + fleeDir * 15f;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(fleeTarget, out hit, 15f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                    pathUpdateTimer = 0.2f;
                }

                // 【刪除】：原本在這裡強制「鎖定面相玩家」的程式碼已經拔除

                if (Vector3.Distance(transform.position, player.position) <= keepFleeingDistance)
                {
                    disappearTimer = 0f;
                }
                else
                {
                    disappearTimer += Time.deltaTime;
                }
            }

            absoluteTimer += Time.deltaTime;
            yield return null;
        }

        agent.ResetPath();
        agent.isStopped = true;
        agent.updateRotation = true;
        agent.acceleration = originalAcceleration;

        ExecuteTeleport();
        ChangeState(AIState.Stalk);
    }

    private void ExecuteTeleport()
    {
        Vector3 validPoint = transform.position;
        bool pointFound = false;

        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * teleportMaxRadius;
            randomDir.y = 0;
            Vector3 potentialPoint = player.position + randomDir;

            if (Vector3.Distance(player.position, potentialPoint) >= teleportMinRadius)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(potentialPoint, out hit, 2f, NavMesh.AllAreas))
                {
                    validPoint = hit.position;
                    pointFound = true;
                    break;
                }
            }
        }

        if (pointFound) agent.Warp(validPoint);
        else agent.Warp(player.position - player.forward * 5f);

        // 瞬移完畢後，確保下一次跟蹤是從最遠的安全距離開始，重新開始逼近
        currentStalkDistance = stalkDistance;
    }

    private void UpdateWaitCloneState()
    {
        isFrozenByPlayer = false;
    }

    private void UpdateChaseState()
    {
        isFrozenByPlayer = false;
        agent.speed = chaseSpeed;
        agent.isStopped = false;

        if (player != null)
        {
            agent.SetDestination(player.position);

            // --- 恐怖視角強制鎖定機制 (死亡凝視) ---
            if (faceFocusPoint != null && playerCamera != null)
            {
                Vector3 playerEyePos = playerCamera.transform.position;
                Vector3 targetFacePos = faceFocusPoint.position;
                Vector3 dirToFace = targetFacePos - playerEyePos;

                // 射線檢查：玩家跟 Fonia 的臉之間有沒有牆壁擋住？
                if (Physics.Raycast(playerEyePos, dirToFace.normalized, out RaycastHit hit, dirToFace.magnitude))
                {
                    // 如果打到的是 Fonia 本體或其子物件，代表視線暢通沒有牆壁！
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        // 產生一股強大的磁力，把玩家的攝影機「扯」向 Fonia 的臉
                        Quaternion targetRotation = Quaternion.LookRotation(dirToFace);
                        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetRotation, Time.deltaTime * cameraLockSpeed);
                    }
                }
            }

            // --- 【核心修改：數學距離攻擊判定】 ---
            // 如果 Fonia 距離玩家小於 1.5 公尺 (代表已經貼到臉上了)
            if (Vector3.Distance(transform.position, player.position) <= 1.5f)
            {
                // 觸發扣血並瞬移！
                ExecuteHitAndRun();
            }
        }
    }

    // ================== 一擊脫離機制 (取代原本的物理碰撞) ==================
    private void ExecuteHitAndRun()
    {
        // 嘗試抓取玩家的理智度腳本
        PlayerSanity playerSanity = player.GetComponent<PlayerSanity>();
        if (playerSanity != null)
        {
            // 1. 瞬間扣除 20 點理智
            playerSanity.TakeDamage(damageAmount);
            Debug.Log("<color=red>[Fonia 襲擊]</color> 貼臉成功！扣除理智並立刻消失！");

            // 2. 煞車，取消當前的追殺路徑
            agent.ResetPath();

            // 3. 呼叫我們之前寫好的瞬移方法 (它會自動幫我們重置跟蹤距離)
            ExecuteTeleport();

            // 4. 強制把狀態切回「跟蹤」，讓一切恐懼從頭來過
            ChangeState(AIState.Stalk);
        }
    }
}