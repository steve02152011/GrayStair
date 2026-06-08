using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class DoloAI : MonoBehaviour
{
    public enum AIState { Wander, Chase, Attack, Flee }

    [Header("目前狀態")]
    public AIState currentState = AIState.Wander;

    [Header("目標設定")]
    public Transform player;
    private CharacterController playerController;
    private Collider playerCollider;
    private Collider myCollider;

    // ==========================================
    // 【新增】：音效設定區
    // ==========================================
    [Header("音效來源設定")]
    [Tooltip("用來播放叫聲、攻擊聲的 AudioSource")]
    public AudioSource voiceAudioSource;
    [Tooltip("用來播放腳步聲的專屬 AudioSource")]
    public AudioSource footstepAudioSource;

    [Header("腳步聲設定")]
    [Tooltip("走路/跑步的音效陣列 (隨機播放以增加真實感)")]
    public AudioClip[] footstepSounds;
    [Tooltip("漫遊時每隔幾秒播放一次腳步聲？(跑步時會自動加快)")]
    public float footstepInterval = 0.6f;
    private float footstepTimer;

    [Header("隨機閒置音效 (三不五時發出的聲音)")]
    [Tooltip("閒置時會隨機播放的低吼聲")]
    public AudioClip[] idleSounds;
    [Tooltip("最少隔幾秒發出一次聲音？")]
    public float idleSoundMinInterval = 5f;
    [Tooltip("最多隔幾秒發出一次聲音？")]
    public float idleSoundMaxInterval = 12f;
    private float idleSoundTimer;

    [Header("狀態觸發音效")]
    [Tooltip("發現玩家，切換到 Chase 狀態時的凶狠吼叫聲")]
    public AudioClip chaseRoarSound;
    [Tooltip("咬到玩家造成傷害時的撕咬聲")]
    public AudioClip attackHitSound;
    [Tooltip("被雷射照到時的痛苦慘叫聲")]
    public AudioClip painSound;
    // ==========================================

    [Header("尋聲定位設定 (盲眼聽覺)")]
    public float maxHearingRadius = 25f;
    public float silentSpeedThreshold = 0.5f;
    public float playerMaxSpeedReference = 10f;
    public float investigateTime = 3f;

    private Vector3 lastHeardPosition;

    [Header("避光雷達設定 (僅漫遊有效)")]
    public LayerMask laserLayer;
    public float avoidLaserDistance = 7f;
    public float whiskersAngle = 30f;

    [Header("遊走設定")]
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    public float walkSpeed = 3.5f;

    [Header("追擊與攻擊設定")]
    public float runSpeed = 8f;
    public float attackRange = 2.5f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("防卡死設定")]
    public float ignoreCollisionDuration = 2f;

    [Header("懼光逃跑設定 (避難所)")]
    public string fleeNodeTag = "FleeNode";
    public float fleeSpeed = 12f;
    public float fleeDistance = 15f;
    public float fleeDuration = 4f;

    [Header("異空間傳送設定")]
    public float teleportChance = 0.2f;

    [Header("攻擊力設定")]
    public float damageAmount = 20f;

    private NavMeshAgent agent;
    private float stateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        myCollider = GetComponent<Collider>();
        stateTimer = wanderTimer;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
            playerCollider = player.GetComponent<Collider>();
        }

        // 初始化隨機音效計時器
        idleSoundTimer = Random.Range(idleSoundMinInterval, idleSoundMaxInterval);
    }

    void Update()
    {
        // 執行音效邏輯
        HandleFootsteps();
        HandleIdleSounds();

        switch (currentState)
        {
            case AIState.Wander: UpdateWanderState(); break;
            case AIState.Chase: UpdateChaseState(); break;
            case AIState.Attack: UpdateAttackState(); break;
            case AIState.Flee: UpdateFleeState(); break;
        }
    }

    // ================== 音效處理邏輯 ==================
    private void HandleFootsteps()
    {
        // 只要 Dolo 在移動 (速度大於 0.1) 就播放腳步聲
        if (agent.velocity.magnitude > 0.1f && !agent.isStopped)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                if (footstepAudioSource != null && footstepSounds.Length > 0)
                {
                    AudioClip step = footstepSounds[Random.Range(0, footstepSounds.Length)];
                    footstepAudioSource.PlayOneShot(step);
                }

                // 如果是追擊或逃跑狀態，腳步聲頻率會變快 (乘以 0.6 倍時間)
                float currentInterval = (currentState == AIState.Chase || currentState == AIState.Flee) ? footstepInterval * 0.6f : footstepInterval;
                footstepTimer = currentInterval;
            }
        }
        else
        {
            // 停下時立刻重置，這樣下次起步時會馬上踩出第一步
            footstepTimer = 0f;
        }
    }

    private void HandleIdleSounds()
    {
        // 只有在遊蕩(Wander)且沒有在追人時，才會三不五時發出聲音
        if (currentState == AIState.Wander)
        {
            idleSoundTimer -= Time.deltaTime;
            if (idleSoundTimer <= 0)
            {
                if (voiceAudioSource != null && idleSounds.Length > 0)
                {
                    AudioClip idleClip = idleSounds[Random.Range(0, idleSounds.Length)];
                    voiceAudioSource.PlayOneShot(idleClip);
                }
                // 重新決定下一次發出聲音的時間
                idleSoundTimer = Random.Range(idleSoundMinInterval, idleSoundMaxInterval);
            }
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        Debug.Log($"<color=cyan>[Dolo 大腦]</color> 狀態切換：{currentState} ? <b>{newState}</b>");

        // 【新增】：如果切換到 Chase 狀態，播放凶狠吼叫聲
        if (newState == AIState.Chase && voiceAudioSource != null && chaseRoarSound != null)
        {
            voiceAudioSource.PlayOneShot(chaseRoarSound);
        }

        currentState = newState;
        stateTimer = 0f;
    }

    // ================== AI 狀態邏輯 (維持不變) ==================

    private void UpdateWanderState()
    {
        agent.speed = walkSpeed;

        if (CheckWhiskersForLaser())
        {
            PickNewWanderDestination();
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                stateTimer += Time.deltaTime;
                if (stateTimer >= wanderTimer)
                {
                    PickNewWanderDestination();
                }
            }
            else
            {
                stateTimer = 0f;
            }
        }

        if (CheckForSounds()) ChangeState(AIState.Chase);
    }

    private void PickNewWanderDestination()
    {
        Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
        agent.SetDestination(newPos);
        stateTimer = 0;
    }

    private bool CheckWhiskersForLaser()
    {
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f);
        if (Physics.Raycast(rayStart, transform.forward, avoidLaserDistance, laserLayer)) return true;
        Vector3 leftDir = Quaternion.Euler(0, -whiskersAngle, 0) * transform.forward;
        if (Physics.Raycast(rayStart, leftDir, avoidLaserDistance, laserLayer)) return true;
        Vector3 rightDir = Quaternion.Euler(0, whiskersAngle, 0) * transform.forward;
        if (Physics.Raycast(rayStart, rightDir, avoidLaserDistance, laserLayer)) return true;

        return false;
    }

    private void UpdateChaseState()
    {
        agent.speed = runSpeed;

        if (CheckForSounds()) stateTimer = 0f;

        agent.SetDestination(lastHeardPosition);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            stateTimer += Time.deltaTime;
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                ChangeState(AIState.Attack);
            }
            else if (stateTimer >= investigateTime)
            {
                ChangeState(AIState.Wander);
            }
        }
        else
        {
            if (Vector3.Distance(transform.position, player.position) <= attackRange) ChangeState(AIState.Attack);
        }
    }

    private bool CheckForSounds()
    {
        if (player == null || playerController == null) return false;
        Vector3 horizontalVelocity = new Vector3(playerController.velocity.x, 0, playerController.velocity.z);
        float playerSpeed = horizontalVelocity.magnitude;

        if (playerSpeed <= silentSpeedThreshold) return false;
        float currentNoiseRadius = Mathf.Lerp(0, maxHearingRadius, playerSpeed / playerMaxSpeedReference);

        if (Vector3.Distance(transform.position, player.position) <= currentNoiseRadius)
        {
            lastHeardPosition = player.position;
            return true;
        }
        return false;
    }

    public void ReactToLaser(Vector3 laserSourcePos)
    {
        if (currentState == AIState.Flee) return;
        ChangeState(AIState.Flee);
        Debug.Log("<color=blue>[Dolo 恐懼]</color> 嗚啊！！被光照到了！尋找避難所...");

        // 【新增】：被雷射照到時播放痛苦聲
        if (voiceAudioSource != null && painSound != null)
        {
            voiceAudioSource.PlayOneShot(painSound);
        }

        GameObject[] fleeNodes = GameObject.FindGameObjectsWithTag(fleeNodeTag);
        if (fleeNodes.Length > 0)
        {
            Transform bestNode = null;
            float maxDistance = -1f;

            foreach (GameObject node in fleeNodes)
            {
                float distToPlayer = Vector3.Distance(node.transform.position, player.position);
                if (distToPlayer > maxDistance)
                {
                    maxDistance = distToPlayer;
                    bestNode = node.transform;
                }
            }

            if (bestNode != null)
            {
                agent.speed = fleeSpeed;
                agent.SetDestination(bestNode.position);
                return;
            }
        }

        Debug.LogWarning("<color=orange>[Dolo 警告]</color> 找不到 FleeNode！使用隨機逃跑模式。");
        Vector3 fleeDirection = (transform.position - laserSourcePos).normalized;
        Vector3 targetFleePoint = transform.position + fleeDirection * fleeDistance;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetFleePoint, out hit, 10f, NavMesh.AllAreas))
        {
            agent.speed = fleeSpeed;
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.speed = fleeSpeed;
            agent.SetDestination(RandomNavSphere(transform.position, fleeDistance, -1));
        }
    }

    private void UpdateFleeState()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= fleeDuration) ChangeState(AIState.Wander);
    }

    private void UpdateAttackState()
    {
        agent.isStopped = true;
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Debug.Log("<color=magenta>[Dolo 戰鬥]</color> 飛撲撕咬！！");

            PlayerSanity playerSanity = player.GetComponent<PlayerSanity>();
            if (playerSanity != null)
            {
                playerSanity.TakeDamage(damageAmount);
            }

            // 【新增】：播放撕咬玩家的音效
            if (voiceAudioSource != null && attackHitSound != null)
            {
                voiceAudioSource.PlayOneShot(attackHitSound);
            }

            lastAttackTime = Time.time;

            if (Random.value <= teleportChance)
            {
                if (OtherworldManager.Instance != null && !OtherworldManager.Instance.isInOtherworld)
                {
                    OtherworldManager.Instance.SendToOtherworld(player.gameObject);
                    agent.isStopped = false;
                    ChangeState(AIState.Wander);
                    PickNewWanderDestination();
                    return;
                }
            }

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(IgnoreCollisionRoutine());
            }
        }

        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            agent.isStopped = false;
            ChangeState(AIState.Chase);
        }
    }

    private IEnumerator IgnoreCollisionRoutine()
    {
        if (myCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(myCollider, playerCollider, true);
            yield return new WaitForSeconds(ignoreCollisionDuration);
            Physics.IgnoreCollision(myCollider, playerCollider, false);
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}