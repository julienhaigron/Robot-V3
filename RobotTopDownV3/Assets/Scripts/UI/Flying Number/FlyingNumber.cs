using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;


public class FlyingNumber : MonoBehaviour
{
	public Action onFinished;

	[SerializeField] protected RectTransform m_rectTfm;
	[ShowIf("@m_rectTfm == null")]
	[SerializeField] protected WorldCounterHorizontalLayout m_worldLayoutGroup;
	[SerializeField] protected Transform m_movedTfm;
	[SerializeField] protected SpriteRenderer m_iconSpriteRdr;
	[SerializeField] protected CounterDisplay m_counterDisplay;

	[SerializeField] protected TMP_Text m_counterTMP;

	[ReadOnly, ShowInInspector]
	public bool isPlaying;
	[ReadOnly, ShowInInspector]
	public bool isMergable;

	[HideInInspector]
	public FlyingNumberManager.FlyingNumberConfig config;
	public enum NumberState
	{
		Hidden,
		Showing,
		Idle,
		Hiding,
	}

	protected NumberState m_currentState = NumberState.Hidden;
	public NumberState CurrentState => m_currentState;
	protected Transform MovedTfm => m_rectTfm == null ? m_movedTfm : m_rectTfm.transform;

	protected int m_mergeIndex = -1; // 0 = default no icon //-1 = not used  only merge when match
	public int MergeIndex => m_mergeIndex;
	protected float m_currentTime = 0f;
	protected float m_currentValue = 0f;
	protected Vector3 m_idlePosition;
	protected Sequence m_fadeTween;
	protected Tweener m_moveTween;
	protected Coroutine m_playingCR;
	protected float m_t;

	protected string Prefix
	{
		get
		{
			if (config.addPlusPrefixIfPositive)
			{
				if (m_currentValue > 0)
					return config.prefix + "+";
			}
			return config.prefix;
		}
	}

	/*public void SetIconAndMergeIndex ( CurrencyType _currencyType, float _iconScale = 1f )
	{
		Currency c = GameAssets.current.currencies[_currencyType];
		SetIconAndMergeIndex(c.icon, _iconScale);

		m_mergeIndex = c.icon.GetHashCode();
	}*/

	public virtual void SetIconAndMergeIndex ( Sprite _iconSprite, float _iconScale = 1f )
	{
		if (_iconSprite != null && m_iconSpriteRdr != null)
		{
			m_iconSpriteRdr.sprite = _iconSprite;
			m_iconSpriteRdr.gameObject.SetActive(true);
		}

		m_mergeIndex = _iconSprite.GetHashCode();
	}

	public virtual void DisableIconAndSetMergeIndex ( int _mergeIndex )
	{
		if (m_iconSpriteRdr != null)
			m_iconSpriteRdr.gameObject.SetActive(false);

		m_mergeIndex = _mergeIndex;
	}

	public void ResetAtHiddenState ( float _yOverlapOffset, bool _playPS )
	{
		m_currentState = NumberState.Hidden;
		m_currentTime = 0f;
		isMergable = false;
		isPlaying = false;

		//set Pos
		m_moveTween?.Kill();

		//rnd offset
		m_idlePosition = Vector3.zero;
		if (config.rndXMinMaxOffset != Vector2.zero || config.rndYMinMaxOffset != Vector2.zero)
			m_idlePosition = new Vector2(Random.Range(config.rndXMinMaxOffset.x, config.rndXMinMaxOffset.y), Random.Range(config.rndYMinMaxOffset.x, config.rndYMinMaxOffset.y));

		m_idlePosition += Vector3.up * _yOverlapOffset;

		if (_playPS)
			MovePSToIdlePos();

		MoveToNewPos(m_idlePosition + (Vector3)config.hiddenOffset);

		//set angle
		SetAngle();

		//set font asset
		if (config.fontAsset != null)
			m_counterTMP.fontMaterial = config.fontAsset;

		//SetColor
		m_counterDisplay.TextColor = config.textColor;

		//SetBlink
		SetBlink(false);

		//set alpha
		FadeAllAlphas(0f, 0f, config.ignoreTimeScale);

		//set value
		m_counterDisplay.SetValueInstant(0f, true, config.prefix, config.suffix, config.useEngineeringNotation, config.stringFormat);
	}

	protected virtual void MoveToNewPos ( Vector3 _newPos )
	{
		m_movedTfm.localPosition = _newPos;
	}

	protected virtual void MovePSToIdlePos ()
	{
		if (m_counterDisplay.burstUpdatePS != null)
			m_counterDisplay.burstUpdatePS.transform.localPosition = m_idlePosition;
		if (m_counterDisplay.loopUpdatePS != null)
			m_counterDisplay.loopUpdatePS.transform.localPosition = m_idlePosition;

		//PI are not used in world
	}

	protected virtual void FadeAllAlphas ( float _alpha, float _duration, bool _ignoreTimeScale )
	{
		m_fadeTween?.Kill();
		m_fadeTween = DOTween.Sequence();
		m_fadeTween.SetUpdate(_ignoreTimeScale);

		if (m_iconSpriteRdr != null)
			m_fadeTween.Join(m_iconSpriteRdr.DOFade(_alpha, _duration).SetEase(Ease.Linear));
		if (m_counterTMP != null)
			m_fadeTween.Join(m_counterTMP.DOFade(_alpha, _duration).SetEase(Ease.Linear));
	}

	protected void SetAngle ()
	{
		Vector3 angle = Vector3.zero;
		if (config.rndMinMaxAngle != Vector2.zero)
			angle = new Vector3(0f, 0f, Random.Range(config.rndMinMaxAngle.x, config.rndMinMaxAngle.y));
		MovedTfm.localRotation = Quaternion.Euler(angle);
	}

	protected void SetColors ()
	{
		m_counterDisplay.TextColor = config.textColor;
	}

	protected void SetBlink ( bool _active )
	{
		/*foreach (GraphicColorAnimation blink in m_counterDisplay.graphicColorAnimationList)
		{
			if (!_active)
				blink.ResetColor();
			else
				blink.PlayAnimation(config.defaultColorAnimation, null, config.useAnimationCustomColor ? config.customColorAnimation : null);
		}*/
	}

	public void DoAnimation ( float _newValue, float _yOffset, bool _blink, bool _playPS )
	{
		SetObjectEnable(true);
		//a already playing instance be called by the manager if config.merge == false
		if (m_playingCR != null)
		{
			StopCoroutine(m_playingCR);

			DoMergeAnimation(_newValue, _blink, _playPS);
			m_playingCR = StartCoroutine(IdleCR(0.2f));
		}
		else
		{
			m_playingCR = StartCoroutine(PlayingCR(_newValue, _yOffset, _blink, _playPS));
		}
	}

	protected virtual void SetObjectEnable ( bool _active )
	{
		gameObject.SetActive(_active);
	}

	protected void DoMergeAnimation ( float _newValue, bool _blink, bool _playPS )
	{
		if (m_currentState == NumberState.Hiding)
		{
			m_fadeTween?.Kill();
			FadeAllAlphas(1f, 0f, config.ignoreTimeScale);

			m_moveTween?.Kill();
			MoveToNewPos(m_idlePosition);
		}

		SetAngle();
		SetBlink(_blink);

		bool diffIsEnoughToLaunchLoopUpdate = (_newValue > 0 && _newValue > 3) || (_newValue < 0 && _newValue < -3);
		if (m_currentValue > 0)
			diffIsEnoughToLaunchLoopUpdate = (_newValue / m_currentValue) > m_counterDisplay.minDiffRatioToLoopUpdate;

		m_currentValue += _newValue;

		bool forceBurstUpdate = !diffIsEnoughToLaunchLoopUpdate || config.forceTextBurstUpdateOnMerge;

		m_counterDisplay.UpdateValue(m_currentValue, null, _playPS, Prefix, config.suffix, config.useEngineeringNotation, config.stringFormat, null, forceBurstUpdate);
		//will do the punch scale of the counter display (that contains the icon too)
	}

	public void SkipAndHide ()
	{
		if (m_playingCR != null)
			StopCoroutine(m_playingCR);

		m_playingCR = StartCoroutine(HideCR());
	}

	protected IEnumerator PlayingCR ( float _newValue, float _yOffset, bool _blink, bool _playPS )
	{
		m_t = 0f;

		//reset all
		ResetAtHiddenState(_yOffset, _playPS);

		//set blink
		SetBlink(_blink);

		//SHOW
		m_currentState = NumberState.Showing;
		isMergable = true;
		isPlaying = true;
		FadeAllAlphas(1f, config.showDuration * (config.hideLastWhenSpawningNew ? 0.5f : 1f), config.ignoreTimeScale);
		DOMoveToIdlePos(config.showDuration, config.showEase, config.ignoreTimeScale);

		//animate value
		m_currentValue = _newValue;
		if (!config.forceTextBurstUpdateOnShow)
			m_counterDisplay.UpdateValue(m_currentValue, null, _playPS, Prefix, config.suffix, config.useEngineeringNotation, config.stringFormat);
		else
		{
			m_counterDisplay.SetValueInstant(m_currentValue, true, Prefix, config.suffix, config.useEngineeringNotation, config.stringFormat);
			m_counterDisplay.DoScale();//the scale animation is used instead to have a different animation on update (punchscale)

			if (_playPS)
				PlayBurstParticle();
		}

		UpdateLayout();

		while (m_t < config.showDuration)
		{
			m_t += config.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
			m_currentTime += config.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
			yield return null;
		}

		//SET IDLE
		m_playingCR = StartCoroutine(IdleCR());
	}

	void UpdateLayout ()
	{
		if (m_rectTfm != null || m_worldLayoutGroup == null) return;

		m_worldLayoutGroup.UpdateLayout();
		StartCoroutine(UpdateWorldLayoutCR());
	}

	IEnumerator UpdateWorldLayoutCR ()
	{
		yield return null;
		m_worldLayoutGroup.UpdateLayout();

		while (m_counterDisplay.TextTweenIsPlaying)
		{
			yield return null;
			m_worldLayoutGroup.UpdateLayout();
		}
	}

	protected virtual void DOMoveToIdlePos ( float _duration, Ease _ease, bool _ignoreTimeScale )
	{
		m_moveTween?.Kill();
		m_moveTween = m_movedTfm.DOLocalMove(m_idlePosition, _duration).SetEase(_ease).SetUpdate(_ignoreTimeScale);
	}

	protected virtual void PlayBurstParticle ()
	{
		if (m_counterDisplay.burstUpdatePS != null)
		{
			m_counterDisplay.burstUpdatePS.Stop();
			m_counterDisplay.burstUpdatePS.Play();
		}
	}

	protected IEnumerator IdleCR ( float _additionalTime = 0f )
	{
		//IDLE
		m_currentState = NumberState.Idle;

		//in case of merge let show finish
		while (m_moveTween.IsActive() && m_moveTween.IsPlaying())
			yield return null;

		while (m_fadeTween.IsActive() && m_fadeTween.IsPlaying())
			yield return null;

		//wait for counter animation
		while (m_counterDisplay.TextTweenIsPlaying)
			yield return null;

		m_t = 0f;
		while (m_t < config.idleDuration + _additionalTime)
		{
			m_t += config.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
			m_currentTime += config.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
			yield return null;
		}

		//HIDE
		m_playingCR = StartCoroutine(HideCR());
	}

	protected IEnumerator HideCR ()
	{
		m_currentState = NumberState.Hiding;

		FadeAllAlphas(0f, config.hideDuration, config.ignoreTimeScale);

		//if canceled do no interupt current move tween
		if (!m_moveTween.IsActive() || !m_moveTween.IsPlaying())
		{
			//Vector3 hidePos = m_idlePosition + (Vector3)config.hideOffset;
			DOMoveToIdlePos(config.showDuration, config.showEase, config.ignoreTimeScale);
		}

		m_t = 0f;
		while (m_t < config.hideDuration)
		{
			m_t += config.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

			if (m_counterTMP.alpha < 0.2f)
				isMergable = false;

			m_currentTime += config.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
			yield return null;
		}

		//FINISH
		OnHideFinished();
		onFinished?.Invoke();
	}

	public void OnHideFinished ()
	{
		m_currentValue = 0f;
		m_currentState = NumberState.Hidden;
		SetObjectEnable(false);
		isPlaying = false;
		m_playingCR = null;
		m_mergeIndex = -1;
	}
}