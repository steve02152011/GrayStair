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

    [Header("音效設定")]
    public AudioSource ambientAudioSource;
    public AudioClip ambientSound;
    public AudioSource voiceAudioSource;
    public AudioClip chaseRoarSound;

    [Header("視線判定設定 (藍紅區機制)")]
    [Range(0f, 0.4f)] public float edgeToleranceX = 0.15f;
    [Range(0f, 0.4f)] public float edgeToleranceY = 0.1f;

    [Header("移動與跟蹤設定")]
    public float walkSpeed = 2.0f;
    public float chaseSpeed = 4.5f;
    public float stalkDistance = 8f;
    public float creepSpeed = 0.6f;
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
    [Range(0f, 1f)] public float cloneChance = 0.33f;

    [Header("攻擊力設定")]
    public float damageAmount = 20f;

    [Header("追殺與鎖定設定")]
    public Transform faceFocusPoint;
    public float cameraLockSpeed = 8f;

    private NavMeshAgent agent;
    private float abilityTimer = 0f;
    private GameObject currentClone;
    private bool isFrozenByPlayer = false;
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

        // ==========================================
        // 【修改】：遊戲一開始，強制讓 Fonia 下線休眠
        // (等待玩家被咬進異空間時才會啟動)
        // ==========================================
        DeactivateFonia();
    }

    void Update()
    {
        HandleCloneAbility();

        switch (currentState)
        {
            case AIState.Stalk: UpdateStalkState(); break;
            case AIState.WaitClone: UpdateWaitCloneState(); break;
            case AIState.Chase: UpdateChaseState(); break;
            case AIState.FleeThenTeleport: break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        Debug.Log($"<color=cyan>[Fonia 大腦]</color> 狀態切換：{currentState} ? <b>{newState}</b>");
        currentState = newState;
    }

    // ==========================================
    // 【新增】：異空間啟動與關機機制
    // ==========================================
    public void ActivateFonia()
    {
        // 1. 顯示本體並重啟導航
        gameObject.SetActive(true);

        if (agent != null && player != null)
        {
            agent.enabled = true;

            // 【貼心設計】：把 Fonia 傳送到玩家背後的黑暗處，而不是出現在奇怪的舊位置
            Vector3 spawnPos = player.position - player.forward * stalkDistance;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 10f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            agent.isStopped = false;
        }

        isFrozenByPlayer = false;
        currentStalkDistance = stalkDistance;
        ChangeState(AIState.Stalk);

        // 2. 開啟恐怖環境音
        if (ambientAudioSource != null && ambientSound != null)
        {
            ambientAudioSource.clip = ambientSound;
            ambientAudioSource.loop = true;
            ambientAudioSource.Play();
        }

        Debug.Log("<color=white>[Fonia 系統]</color> 玩家進入異空間，Fonia 已上線！");
    }

    public void DeactivateFonia()
    {
        // 1. 停止導航與追擊
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 2. 關閉所有聲音
        if (ambientAudioSource != null) ambientAudioSource.Stop();

        // 3. 銷毀場上的分身 (避免玩家回主世界還看到分身)
        if (currentClone != null) Destroy(currentClone);

        // 4. 徹底隱藏本體，節省效能
        gameObject.SetActive(false);

        Debug.Log("<color=grey>[Fonia 系統]</color> 玩家離開異空間，Fonia 已下線休眠！");
    }

    // ==========================================
    // (以下維持不變)
    // ==========================================
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
        if (voiceAudioSource != null && chaseRoarSound != null) voiceAudioSource.PlayOneShot(chaseRoarSound);
        ChangeState(AIState.Chase);
    }

    public void OnCloneExpired()
    {
        agent.isStopped = false;
        ChangeState(AIState.Stalk);
    }

    private void UpdateStalkState()
    {
        agent.speed = walkSpeed;

        if (player == null || playerCamera == null) return;

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
                    if (hit.transform == transform || hit.transform.IsChildOf(transform)) isVisibleToPlayer = true;
                }
            }
        }

        if (isVisibleToPlayer)
        {
            isFrozenByPlayer = true;
            agent.isStopped = true;
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
                currentStalkDistance -= creepSpeed * Time.deltaTime;
                currentStalkDistance = Mathf.Max(currentStalkDistance, 0f);

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
                    if (NavMesh.SamplePosition(fleeTarget, out hit, 15f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
                    pathUpdateTimer = 0.2f;
                }

                if (Vector3.Distance(transform.position, player.position) <= keepFleeingDistance) disappearTimer = 0f;
                else disappearTimer += Time.deltaTime;
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

            if (faceFocusPoint != null && playerCamera != null)
            {
                Vector3 playerEyePos = playerCamera.transform.position;
                Vector3 targetFacePos = faceFocusPoint.position;
                Vector3 dirToFace = targetFacePos - playerEyePos;

                if (Physics.Raycast(playerEyePos, dirToFace.normalized, out RaycastHit hit, dirToFace.magnitude))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(dirToFace);
                        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetRotation, Time.deltaTime * cameraLockSpeed);
                    }
                }
            }

            if (Vector3.Distance(transform.position, player.position) <= 1.5f) ExecuteHitAndRun();
        }
    }

    private void ExecuteHitAndRun()
    {
        PlayerSanity playerSanity = player.GetComponent<PlayerSanity>();
        if (playerSanity != null)
        {
            playerSanity.TakeDamage(damageAmount);
            Debug.Log("<color=red>[Fonia 襲擊]</color> 貼臉成功！扣除理智並立刻消失！");

            agent.ResetPath();
            ExecuteTeleport();
            ChangeState(AIState.Stalk);
        }
    }
}