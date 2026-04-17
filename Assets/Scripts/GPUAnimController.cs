using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

public class GPUAnimController : MonoBehaviour
{
    [Header("绑定数据")]
    public Texture2D boneTex;
    public AnimationConfig animConfig;
    public float frameRate = 30;
    public float defaultFadeTime = 0.2f;

    private Material mat;
    private string currentAnimName;
    private Coroutine transitionCoroutine;

    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
        mat.SetTexture("_BoneTex", boneTex);
        mat.SetFloat("_FrameRate", frameRate);

        // 默认播放第一个动画
        if (animConfig.clips.Count > 0)
        {
            foreach (var key in animConfig.clips.Keys)
            {
                PlayAnimation(key);
                break;
            }
        }
    }

    [Button]
    public void PlayAnimation(string targetAnimName, float fadeTime = -1)
    {
        if (!animConfig.clips.TryGetValue(targetAnimName, out AnimationClipInfo targetClip))
        {
            Debug.LogWarning($"动画 {targetAnimName} 不存在！");
            return;
        }

        // 同动画：无过渡
        if (currentAnimName == targetAnimName)
        {
            SetAnimationFrame(targetClip);
            mat.SetFloat("_Transition", 0);
            return;
        }

        // 不同动画：过渡
        float fade = fadeTime < 0 ? defaultFadeTime : fadeTime;
        StartCrossFade(targetClip, targetAnimName, fade);
    }

    // 设置当前动画的 起始+结束帧
    private void SetAnimationFrame(AnimationClipInfo clip)
    {
        float start = clip.start;
        float end = clip.end; // 结束帧束帧

        mat.SetFloat("_CurrAnimStartFrame", start);
        mat.SetFloat("_CurrAnimEndFrame", end);
        mat.SetFloat("_TargetAnimStartFrame", start);
        mat.SetFloat("_TargetAnimEndFrame", end);
    }

    private void StartCrossFade(AnimationClipInfo targetClip, string targetAnimName, float fadeTime)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        // 设置目标动画起止帧
        float targetStart = targetClip.start;
        float targetEnd = targetClip.end;

        mat.SetFloat("_TargetAnimStartFrame", targetStart);
        mat.SetFloat("_TargetAnimEndFrame", targetEnd);

        transitionCoroutine = StartCoroutine(TransitionCoroutine(fadeTime, targetAnimName));
    }

    private IEnumerator TransitionCoroutine(float fadeTime, string newAnimName)
    {
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            mat.SetFloat("_Transition", t / fadeTime);
            yield return null;
        }

        // 过渡完成：无缝切换
        currentAnimName = newAnimName;
        mat.SetFloat("_CurrAnimStartFrame", mat.GetFloat("_TargetAnimStartFrame"));
        mat.SetFloat("_CurrAnimEndFrame", mat.GetFloat("_TargetAnimEndFrame"));
        mat.SetFloat("_Transition", 0);
    }
}