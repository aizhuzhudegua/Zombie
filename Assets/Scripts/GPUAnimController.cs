using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class GPUAnimController : MonoBehaviour
{
    [Header("动画配置")]
    public AnimationConfig animConfig;

    [Header("过渡设置")]
    public float transitionDuration = 0.2f;

    [Header("烘焙帧率")]
    public float frameRate = 30f;

    [Header("运行状态")]
    [ReadOnly] public bool isPaused = false;

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;
    private AnimationClipInfo _currentClip;
    private string _currentAnimName;
    private string _targetAnimName;

    private float _animProgress;    // 当前动画播放进度
    private float _frameTimer;
    private float _frameInterval => 1f / frameRate;

    // 过渡状态
    private bool _isTransitioning;
    private float _transitionTimer;
    private float _frozenProgress;          // 过渡期间冻结的当前动画进度
    private AnimationClipInfo _targetClip;  // 过渡目标动画

    public event Action<string> OnAnimationEnd;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _animProgress = 0;
        _renderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        
     
    }

    void Update()
    {
        if (!isPaused && _currentClip != null)
        {
            _frameTimer += Time.deltaTime;
            if (_frameTimer >= _frameInterval)
            {
                _frameTimer = 0;

                if (_isTransitioning)
                {
                    // 过渡期间：进度冻结，只推进 _Transition
                    _transitionTimer += _frameInterval;
                    float t = Mathf.Clamp01(_transitionTimer / transitionDuration);
                    _mpb.SetFloat("_Transition", t);
                    UpdateAnimData(_frozenProgress);

                    if (t >= 1f)
                    {
                        _currentAnimName = _targetAnimName;
                        // 过渡完成，继承冻结进度（保持与原协程一致）
                        _currentClip = _targetClip;
                        _animProgress = 0;
                        _isTransitioning = false;

                        _mpb.Clear();
                        _mpb.SetFloat("_CurrAnimStartFrame", _targetClip.start);
                        _mpb.SetFloat("_CurrAnimEndFrame",   _targetClip.end);
                        _mpb.SetInt("_isTransition", 0);
                        UpdateAnimData();

                        _targetClip = null;
                    }
                }
                else
                {
                    // 正常播放
                    float totalFrames = _currentClip.end - _currentClip.start;
                    float frameDuration = 1f / frameRate;
                    _animProgress += frameDuration / (totalFrames / frameRate);

                    if (_currentClip.isLoop)
                        _animProgress %= 1f;
                    else
                    {
                        if(Mathf.Abs(1 - _animProgress) < 0.01f)
                            OnAnimationEnd?.Invoke(_currentAnimName); // 动画结束事件
                        _animProgress = Mathf.Min(_animProgress, 1f);
                    }
                        

                    UpdateAnimData();
                }
            }
        }
    }

    #region 外部接口
    [Button]
    public void PlayAnimation(string animationName)
    {
        if (!CheckAnimationValid(animationName)) return;
        _currentAnimName = animationName;
        _isTransitioning = false;
        _targetClip = null;

        _currentClip = animConfig.clips[animationName];
        _animProgress = 0; // 重置进度

        _mpb.Clear();
        _mpb.SetFloat("_CurrAnimStartFrame", _currentClip.start);
        _mpb.SetFloat("_CurrAnimEndFrame",   _currentClip.end);
        _mpb.SetInt("_isTransition", 0);
        _mpb.SetFloat("_Transition", 0);
        UpdateAnimData();
    }

    [Button]
    public void PlayAnimationWithTransition(string animationName)
    {
        if (!CheckAnimationValid(animationName)) return;
        if (_currentClip == null) { PlayAnimation(animationName); return; }
        _targetAnimName = animationName;
        _targetClip = animConfig.clips[animationName];
        _frozenProgress = _animProgress; // 冻结当前进度
        _transitionTimer = 0;
        _isTransitioning = true;

        _mpb.Clear();
        _mpb.SetFloat("_CurrAnimStartFrame",   _currentClip.start);
        _mpb.SetFloat("_CurrAnimEndFrame",     _currentClip.end);
        _mpb.SetFloat("_TargetAnimStartFrame", _targetClip.start);
        _mpb.SetFloat("_TargetAnimEndFrame",   _targetClip.end);
        _mpb.SetInt("_isTransition", 1);
        _mpb.SetFloat("_Transition", 0);
        UpdateAnimData(_frozenProgress);
    }

    [Button] public void PauseAnimation()  => isPaused = true;
    [Button] public void ResumeAnimation() => isPaused = false;
    [Button] public void ResetAnimation()  => _animProgress = 0;
    #endregion

    #region 核心逻辑
    // 更新数据到Shader
    private void UpdateAnimData(float overrideProgress = -1)
    {
        float progress = overrideProgress < 0 ? _animProgress : overrideProgress;
        _mpb.SetFloat("_AnimProgress", progress);
        _renderer.SetPropertyBlock(_mpb);
    }

    private bool CheckAnimationValid(string animationName)
    {
        if (animConfig == null) { Debug.LogError("未绑定AnimationConfig", gameObject); return false; }
        if (!animConfig.clips.ContainsKey(animationName)) { Debug.LogError($"无动画：{animationName}", gameObject); return false; }
        return true;
    }
    #endregion
}