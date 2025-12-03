using UnityEngine;
using YFan.Attributes;       // 你的 UI 属性系统
using YFan.Runtime.Base.Abstract;
using YFan.Utils;

// ================== Controller (宿主) ==================

public class GuardController : UIAbstractController
{
    [YTitle("引用设置")]
    public Transform PlayerObj;

    [YTitle("AI 参数配置 (调整这里以适配比例尺)")]
    [YBoxGroup("感知参数")]
    [YRange(1, 50)]
    public float PatrolDetectRange = 5.0f; // 巡逻时发现玩家的距离

    [YBoxGroup("感知参数")]
    [YRange(0.5f, 10)]
    public float AttackRange = 1.5f;       // 发动攻击的距离

    [YBoxGroup("感知参数")]
    [YRange(5, 100)]
    public float LostTargetRange = 10.0f;  // 玩家逃跑多远后放弃追逐

    [YSpace(10)]
    [YTitle("运行时 Debug")]
    [YBoxGroup("状态监控")]
    [YReadOnly]
    [SerializeField]
    private string _currentStateName;

    [YBoxGroup("状态监控")]
    [YReadOnly]
    [SerializeField]
    public float DistanceToPlayer; // 公开给状态类访问

    private FSM<GuardController> _fsm;
    private MeshRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();

        // 初始化状态机
        _fsm = new FSM<GuardController>(this);

        // 添加状态
        _fsm.AddState(new PatrolState());
        _fsm.AddState(new ChaseState());
        _fsm.AddState(new AttackState());

        // 启动
        _fsm.StartState<PatrolState>();
    }

    private void Update()
    {
        if (PlayerObj == null) return;

        // 计算距离
        DistanceToPlayer = Vector3.Distance(transform.position, PlayerObj.position);

        // 驱动状态机
        _fsm.OnUpdate();

        // 刷新 Inspector 显示
        _currentStateName = _fsm.CurrentState?.GetType().Name;
    }

    // 改变颜色 (视觉反馈)
    public void SetColor(Color c)
    {
        if (_renderer != null) _renderer.material.color = c;
    }

    // 【核心】可视化调试：画出检测范围
    private void OnDrawGizmos()
    {
        // 1. 巡逻检测范围 (绿色)
        Gizmos.color = new Color(0, 1, 0, 0.3f); // 半透明绿
        Gizmos.DrawWireSphere(transform.position, PatrolDetectRange);

        // 2. 攻击范围 (红色)
        Gizmos.color = new Color(1, 0, 0, 0.5f); // 半透明红
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // 3. 丢失范围 (黄色)
        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, LostTargetRange);

        // 4. 连线
        if (PlayerObj != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, PlayerObj.position);
        }
    }
}

// ================== States (状态类) ==================

// 1. 巡逻状态
public class PatrolState : AbstractState<GuardController>
{
    private float _moveTimer;
    private int _direction = 1;

    public override void OnEnter()
    {
        YLog.Info("进入巡逻模式", "AI");
        mOwner.SetColor(Color.green);
    }

    public override void OnUpdate()
    {
        // 左右移动逻辑
        mOwner.transform.Translate(Vector3.right * _direction * 2f * Time.deltaTime);

        _moveTimer += Time.deltaTime;
        if (_moveTimer > 2f)
        {
            _direction *= -1;
            _moveTimer = 0;
        }

        // 检测：使用配置参数 PatrolDetectRange
        if (mOwner.DistanceToPlayer < mOwner.PatrolDetectRange)
        {
            ChangeState<ChaseState>();
        }
    }
}

// 2. 追逐状态
public class ChaseState : AbstractState<GuardController>
{
    public override void OnEnter()
    {
        YLog.Info("发现玩家！开始追逐！", "AI");
        mOwner.SetColor(Color.yellow);
    }

    public override void OnUpdate()
    {
        // 朝向并移动
        var dir = (mOwner.PlayerObj.position - mOwner.transform.position).normalized;
        mOwner.transform.Translate(dir * 4f * Time.deltaTime); // 跑得比巡逻快

        float dist = mOwner.DistanceToPlayer;

        // 判定攻击：使用 AttackRange
        if (dist < mOwner.AttackRange)
        {
            ChangeState<AttackState>();
        }
        // 判定丢失：使用 LostTargetRange
        else if (dist > mOwner.LostTargetRange)
        {
            ChangeState<PatrolState>();
        }
    }
}

// 3. 攻击状态
public class AttackState : AbstractState<GuardController>
{
    private float _timer;

    public override void OnEnter()
    {
        YLog.Info("攻击中...", "AI");
        mOwner.SetColor(Color.red);
        _timer = 0;
    }

    public override void OnUpdate()
    {
        // 模拟攻击硬直 (1秒)
        _timer += Time.deltaTime;

        if (_timer > 1.0f)
        {
            // 攻击结束，重新判断
            if (mOwner.DistanceToPlayer < mOwner.AttackRange)
                ChangeState<AttackState>(); // 还在范围内，继续打
            else
                ChangeState<ChaseState>();  // 跑了，继续追
        }
    }
}
