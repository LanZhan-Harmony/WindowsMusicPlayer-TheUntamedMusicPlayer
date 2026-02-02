namespace UntamedMusicPlayer.LyricRenderer;

/// <summary>
/// 弹簧动画器 - 实现平滑的弹簧效果滚动
/// </summary>
/// <remarks>
/// 创建弹簧动画器
/// </remarks>
/// <param name="stiffness">刚度，越大弹性越强（推荐100-500）</param>
/// <param name="damping">阻尼，越大衰减越快（推荐10-30）</param>
/// <param name="mass">质量，越大惯性越大（推荐1-5）</param>
public sealed class SpringAnimator(float stiffness = 200f, float damping = 20f, float mass = 1f)
{
    // 弹簧参数
    private readonly float _stiffness = stiffness; // 刚度
    private readonly float _damping = damping; // 阻尼
    private readonly float _mass = mass; // 质量

    // 状态
    private float _currentValue = 0;
    private float _targetValue = 0;
    private float _velocity = 0;

    /// <summary>
    /// 当前值
    /// </summary>
    public float CurrentValue => _currentValue;

    /// <summary>
    /// 目标值
    /// </summary>
    public float TargetValue => _targetValue;

    /// <summary>
    /// 是否正在动画中
    /// </summary>
    public bool IsAnimating =>
        Math.Abs(_currentValue - _targetValue) > 0.1f || Math.Abs(_velocity) > 0.1f;

    /// <summary>
    /// 设置目标值
    /// </summary>
    public void SetTarget(float target)
    {
        _targetValue = target;
    }

    /// <summary>
    /// 立即设置当前值（跳过动画）
    /// </summary>
    public void SetImmediate(float value)
    {
        _currentValue = value;
        _targetValue = value;
        _velocity = 0;
    }

    /// <summary>
    /// 更新动画状态
    /// </summary>
    /// <param name="deltaTime">时间增量（秒）</param>
    /// <returns>当前值</returns>
    public float Update(float deltaTime)
    {
        if (!IsAnimating)
        {
            _currentValue = _targetValue;
            _velocity = 0;
            return _currentValue;
        }

        // 弹簧力公式: F = -k * x - c * v
        // 其中 k 是刚度, x 是位移, c 是阻尼, v 是速度
        var displacement = _currentValue - _targetValue;
        var springForce = -_stiffness * displacement;
        var dampingForce = -_damping * _velocity;
        var totalForce = springForce + dampingForce;

        // 加速度 = 力 / 质量
        var acceleration = totalForce / _mass;

        // 更新速度和位置（使用半隐式欧拉法）
        _velocity += acceleration * deltaTime;
        _currentValue += _velocity * deltaTime;

        return _currentValue;
    }

    /// <summary>
    /// 添加冲量（用于用户拖动）
    /// </summary>
    public void AddImpulse(float impulse)
    {
        _velocity += impulse;
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    public void Reset()
    {
        _currentValue = 0;
        _targetValue = 0;
        _velocity = 0;
    }
}

/// <summary>
/// 歌词渲染状态
/// </summary>
public sealed class LyricRenderState
{
    /// <summary>
    /// 歌词行列表
    /// </summary>
    public List<LyricLine> Lines { get; set; } = [];

    /// <summary>
    /// 滚动动画器
    /// </summary>
    public SpringAnimator ScrollAnimator { get; } = new(180f, 22f, 1.2f);

    /// <summary>
    /// 用户是否正在交互（鼠标悬停或滚动）
    /// </summary>
    public bool IsUserInteracting { get; set; } = false;

    /// <summary>
    /// 用户交互超时计时器
    /// </summary>
    public DateTime LastInteractionTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 用户滚动偏移量
    /// </summary>
    public float UserScrollOffset { get; set; } = 0;

    /// <summary>
    /// 鼠标悬停的行索引（-1表示没有悬停）
    /// </summary>
    public int HoveredLineIndex { get; set; } = -1;

    /// <summary>
    /// 用户交互超时时间（秒）
    /// </summary>
    private const double InteractionTimeout = 3.0;

    /// <summary>
    /// 检查用户交互是否已超时
    /// </summary>
    public bool IsInteractionTimedOut =>
        IsUserInteracting && (DateTime.Now - LastInteractionTime).TotalSeconds > InteractionTimeout;

    /// <summary>
    /// 重置用户交互状态
    /// </summary>
    public void ResetInteraction()
    {
        IsUserInteracting = false;
        UserScrollOffset = 0;
        HoveredLineIndex = -1;
    }

    /// <summary>
    /// 记录用户交互
    /// </summary>
    public void RecordInteraction()
    {
        IsUserInteracting = true;
        LastInteractionTime = DateTime.Now;
    }

    /// <summary>
    /// 从LyricSlice列表创建渲染状态
    /// </summary>
    public void LoadFromSlices(List<LyricSlice> slices)
    {
        Lines.Clear();
        for (var i = 0; i < slices.Count; i++)
        {
            Lines.Add(LyricLine.FromSlice(slices[i]));
        }
        ScrollAnimator.Reset();
        ResetInteraction();
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    public void Reset()
    {
        Lines.Clear();
        ScrollAnimator.Reset();
        ResetInteraction();
    }
}
