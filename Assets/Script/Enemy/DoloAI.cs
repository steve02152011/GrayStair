using UnityEngine;
using UnityEngine.AI;
using System.Collections; // 【新增】：為了使用協程 (Coroutine)

[RequireComponent(typeof(NavMeshAgent))]
public class DoloAI : MonoBehaviour
{
    public enum AIState { Wander, Chase, Attack, Flee }

    [Header("目前狀態")]
    public AIState currentState = AIState.Wander;

    [Header("目標設定")]
    public Transform player;
    private CharacterController playerController;
    private Collider playerCollider; // 【新增】：用來控制碰撞穿透
    private Collider myCollider;     // 【新增】：Dolo 自己的碰撞體

    [Header("尋聲定位設定 (盲眼聽覺)")]
    [Tooltip("Dolo 的基礎聽力極限距離")]
    public float maxHearingRadius = 25f;
    [Tooltip("玩家要走多快才會發出聲音？(低於此速度視為完全靜音)")]
    public float silentSpeedThreshold = 0.5f;
    [Tooltip("玩家的奔跑速度基準 (用來換算最大噪音，請填入你FPS腳本的RunSpeed)")]
    public float playerMaxSpeedReference = 10f;
    [Tooltip("Dolo 到達聲音來源後，會在該處停留尋找幾秒？")]
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

    // ==========================================
    // 【新增】：問題一 (受傷穿透防卡死)
    // ==========================================
    [Header("防卡死設定")]
    [Tooltip("咬到玩家後，玩家可以穿透 Dolo 身體逃跑的秒數")]
    public float ignoreCollisionDuration = 2f;

    // ==========================================
    // 【新增】：問題二 (指定避難所)
    // ==========================================
    [Header("懼光逃跑設定 (避難所)")]
    [Tooltip("請在場景中佈置空物件，並給它們設定這個 Tag")]
    public string fleeNodeTag = "FleeNode";
    public float fleeSpeed = 12f;
    public float fleeDistance = 15f;
    public float fleeDuration = 4f;

    // ==========================================
    // 【新增】：異空間傳送設定
    // ==========================================
    [Header("異空間傳送設定")]
    [Tooltip("攻擊時把玩家拖入光學迷宮的機率 (0.0=不會, 0.5=一半機率, 1.0=每次都會)")]
    public float teleportChance = 0.2f; // 預設 20% 機率

    [Header("攻擊力設定")]
    public float damageAmount = 20f;

    private NavMeshAgent agent;
    private float stateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        myCollider = GetComponent<Collider>(); // 取得 Dolo 的碰撞體
        stateTimer = wanderTimer;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
            playerCollider = player.GetComponent<Collider>(); // 取得玩家的碰撞體
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Wander: UpdateWanderState(); break;
            case AIState.Chase: UpdateChaseState(); break;
            case AIState.Attack: UpdateAttackState(); break;
            case AIState.Flee: UpdateFleeState(); break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        Debug.Log($"<color=cyan>[Dolo 大腦]</color> 狀態切換：{currentState} ? <b>{newState}</b>");
        currentState = newState;
        stateTimer = 0f;
    }

    // ================== 漫遊狀態 (瞎眼，只靠聽覺) ==================

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

        if (CheckForSounds())
        {
            ChangeState(AIState.Chase);
        }
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

    // ================== 追擊狀態 (循聲調查) ==================

    private void UpdateChaseState()
    {
        agent.speed = runSpeed;

        if (CheckForSounds())
        {
            stateTimer = 0f;
        }

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
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                ChangeState(AIState.Attack);
            }
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

    // ================== 【修改】：懼光逃跑 (前往避難所) ==================

    public void ReactToLaser(Vector3 laserSourcePos)
    {
        if (currentState == AIState.Flee) return;
        ChangeState(AIState.Flee);
        Debug.Log("<color=blue>[Dolo 恐懼]</color> 嗚啊！！被光照到了！尋找避難所...");

        // 1. 尋找場景中所有的避難所
        GameObject[] fleeNodes = GameObject.FindGameObjectsWithTag(fleeNodeTag);

        if (fleeNodes.Length > 0)
        {
            Transform bestNode = null;
            float maxDistance = -1f;

            // 2. 掃描所有避難所，找出「離玩家最遠的」那一個
            foreach (GameObject node in fleeNodes)
            {
                float distToPlayer = Vector3.Distance(node.transform.position, player.position);
                if (distToPlayer > maxDistance)
                {
                    maxDistance = distToPlayer;
                    bestNode = node.transform;
                }
            }

            // 3. 往最安全的避難所全力衝刺
            if (bestNode != null)
            {
                agent.speed = fleeSpeed;
                agent.SetDestination(bestNode.position);
                return; // 成功找到避難所，直接結束
            }
        }

        // 4. (備用方案)：如果場景裡忘記放避難所，就退回原本的隨機亂跑邏輯
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

    // ================== 【修改】：攻擊狀態 (一擊脫離與穿透) ==================

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
                playerSanity.TakeDamage(damageAmount); // 先扣一次原本的咬傷
            }

            lastAttackTime = Time.time;

            // ==========================================
            // 【新增】：判定是否觸發異空間傳送！
            // ==========================================
            if (Random.value <= teleportChance)
            {
                if (OtherworldManager.Instance != null && !OtherworldManager.Instance.isInOtherworld)
                {
                    // 執行傳送
                    OtherworldManager.Instance.SendToOtherworld(player.gameObject);

                    // 【關鍵修復】：鬆開煞車！必須解除導航鎖定，不然他會永遠定在原地
                    agent.isStopped = false;

                    // 玩家消失了，Dolo 切換回漫遊狀態，並「立刻離開原地」，避免玩家回來時被守屍
                    ChangeState(AIState.Wander);
                    PickNewWanderDestination();
                    return; // 提早結束，不執行後面的穿透協程
                }
            }

            // 【保留】：啟動防卡死穿透機制 (如果沒有被傳送走才執行)
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

    // 【新增】：讓玩家可以從 Dolo 身體穿過去的協程
    private IEnumerator IgnoreCollisionRoutine()
    {
        if (myCollider != null && playerCollider != null)
        {
            Debug.Log("<color=green>[防卡死]</color> 玩家現在可以穿過怪物逃跑！");

            // 關閉 Dolo 和玩家之間的物理碰撞 (但他們還是踩得到地板)
            Physics.IgnoreCollision(myCollider, playerCollider, true);

            // 等待指定的秒數
            yield return new WaitForSeconds(ignoreCollisionDuration);

            // 恢復物理碰撞
            Physics.IgnoreCollision(myCollider, playerCollider, false);

            Debug.Log("<color=green>[防卡死]</color> 碰撞恢復實體。");
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