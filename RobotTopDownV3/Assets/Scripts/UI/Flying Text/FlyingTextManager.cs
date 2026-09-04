using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class FlyingTextManager : MonoBehaviour
{
	[System.Serializable]
	public class FlyingTextConfig
	{
		public bool addPlusPrefixIfPositive = true;
		public string prefix = "";
		public string suffix = "";
		public bool useEngineeringNotation = false;
		public string stringFormat = "";
		public Material fontAsset = null;
		[Space()]
		public Vector2 rndXMinMaxOffset = Vector2.zero;
		public Vector2 rndYMinMaxOffset = Vector2.zero;
		public Vector2 rndMinMaxAngle = Vector2.zero;
		[Space()]
		public bool ignoreTimeScale = true;
		public float showDuration = 0.3f;
		public Vector2 hiddenOffset = Vector2.down * 40f;
		public Ease showEase = Ease.OutQuad;
		public bool forceTextBurstUpdateOnShow = false;
		[Space()]
		public float idleDuration = 0.5f;
		[Space()]
		public float hideDuration = 0.3f;
		public Vector2 hideOffset = Vector2.zero;
		public Ease hideEase = Ease.OutQuad;
		[Space()]
		public bool mergeVisibleNumbers = false;
		[ShowIf("@mergeVisibleNumbers == true")]
		public bool forceTextBurstUpdateOnMerge = false;
		[ShowIf("@mergeVisibleNumbers == false")]
		public bool hideLastWhenSpawningNew = false;
		public float yAddedOffsetOnOverLap = 0.2f;
		public float yOffsetOnOverLapMax = 10f;
		[Space()]
		public Color textColor = Color.white;
		//public GraphicColorAnimation.AnimationType defaultColorAnimation = GraphicColorAnimation.AnimationType.RedLimitedCountBlink;
		public bool useAnimationCustomColor = false;
		public Color customColorAnimation = Color.red;

		public FlyingTextConfig () { }
		public FlyingTextConfig ( FlyingTextConfig _ref )
		{
			addPlusPrefixIfPositive = _ref.addPlusPrefixIfPositive;
			prefix = _ref.prefix;
			suffix = _ref.suffix;
			fontAsset = _ref.fontAsset;
			useEngineeringNotation = _ref.useEngineeringNotation;
			stringFormat = _ref.stringFormat;
			rndXMinMaxOffset = _ref.rndXMinMaxOffset;
			rndYMinMaxOffset = _ref.rndYMinMaxOffset;
			rndMinMaxAngle = _ref.rndMinMaxAngle;
			ignoreTimeScale = _ref.ignoreTimeScale;
			showDuration = _ref.showDuration;
			hiddenOffset = _ref.hiddenOffset;
			showEase = _ref.showEase;
			forceTextBurstUpdateOnShow = _ref.forceTextBurstUpdateOnShow;
			idleDuration = _ref.idleDuration;
			hideDuration = _ref.hideDuration;
			hideOffset = _ref.hideOffset;
			hideEase = _ref.hideEase;
			mergeVisibleNumbers = _ref.mergeVisibleNumbers;
			forceTextBurstUpdateOnMerge = _ref.forceTextBurstUpdateOnMerge;
			hideLastWhenSpawningNew = _ref.hideLastWhenSpawningNew;
			yAddedOffsetOnOverLap = _ref.yAddedOffsetOnOverLap;
			yOffsetOnOverLapMax = _ref.yOffsetOnOverLapMax;
			textColor = _ref.textColor;
			//defaultColorAnimation = _ref.defaultColorAnimation;
			useAnimationCustomColor = _ref.useAnimationCustomColor;
			customColorAnimation = _ref.customColorAnimation;
		}
	}

	public FlyingTextConfig config;

	//Kept out of the config on purpose: that class is already serialized in the prefabs, and a field added to
	//it now comes back at its default rather than at the value written here. On the component it holds.
	[Title("Outline")]
	[SerializeField] private bool m_useOutline = true;
	[SerializeField, ShowIf("@m_useOutline")] private Color m_outlineColor = Color.black;
	[SerializeField, ShowIf("@m_useOutline"), Range(0f, 1f)] private float m_outlineWidth = .2f;

	[Space()]
	[InfoBox("both UI and world prefab ref set, plz choose one et remove the other", InfoMessageType.Warning, VisibleIf = "@m_flyingTextPrefab != null && m_UIflyingTextPrefab != null")]
	[SerializeField, FormerlySerializedAs("m_UIflyingNumberPrefab")] private UIFlyingText m_UIflyingTextPrefab;
	[SerializeField, FormerlySerializedAs("m_flyingNumberPrefab")] private FlyingText m_flyingTextPrefab;
	[SerializeField] private int m_poolBufferedSize = 1;
	[Space()]
	[SerializeField, FormerlySerializedAs("m_flyingNumberParent")] private Transform m_flyingTextParent;
	[SerializeField, ReadOnly, FormerlySerializedAs("m_flyingNumberList")] private List<FlyingText> m_flyingTextList;

	[Button(), ShowIf("@m_flyingTextList.Count > 0")]
	void ClearFlyingList () { m_flyingTextList.Clear(); }

	private void Awake ()
	{
#if UNITY_EDITOR
		if (m_flyingTextPrefab != null && m_UIflyingTextPrefab != null)
		{
			Debug.LogWarning(gameObject.name + " flyingNumber has both UI and world prefab, plz choose one et remove the other");
		}
#endif

		if (m_poolBufferedSize > 0)
		{
			if (m_flyingTextPrefab != null)
			{
				for (int i = 0; i < m_poolBufferedSize; i++)
				{
					FlyingText instance = Instantiate(m_flyingTextPrefab, m_flyingTextParent);
					instance.onFinished += OnInstanceFinished;
					instance.OnHideFinished();
					m_flyingTextList.Add(instance);
				}
			}
			else if (m_UIflyingTextPrefab != null)
			{
				for (int i = 0; i < m_poolBufferedSize; i++)
				{
					FlyingText instance = Instantiate(m_UIflyingTextPrefab, m_flyingTextParent);
					instance.onFinished += OnInstanceFinished;
					instance.OnHideFinished();
					m_flyingTextList.Add(instance);
				}
			}
			else
			{
				Debug.LogError("missing UI of world flyingNumberPrefab ref");
			}
		}
	}


	private int PlayingCount
	{
		get
		{
			int count = 0;
			for (int i = 0; i < m_flyingTextList.Count; i++)
			{
				if (m_flyingTextList[i].isPlaying)
					count++;
			}
			return count;
		}
	}

	private int ShowOrIdleCount
	{
		get
		{
			int count = 0;
			for (int i = 0; i < m_flyingTextList.Count; i++)
			{
				if (m_flyingTextList[i].CurrentState == UIFlyingText.TextState.Showing || m_flyingTextList[i].CurrentState == UIFlyingText.TextState.Idle)
					count++;
			}
			return count;
		}
	}
	private float m_currentOverlapOffset = 0f;

	public void ShowText ( string _text, Color? _colorOverride = null, bool _blink = false, bool _playPS = false )
	{
		if (!gameObject.activeInHierarchy || string.IsNullOrEmpty(_text))
			return;

		TryHidingVisible();
		FlyingText instance = GetTextInstance(_text.GetHashCode());
		instance.config = new FlyingTextConfig(config);
		ApplyOutlineTo(instance);
		instance.DisableIconAndSetMergeIndex(_text.GetHashCode());
		instance.SetRawText(_text);
		instance.SetColorOverride(_colorOverride);

		InstanceDoAnimation(instance, 0f, _blink, _playPS);
	}

	public void ShowNumber ( float _value, Sprite _iconSprite, bool _blink = false, bool _playPS = false, float _iconScale = 1f, Color? _colorOverride = null )
	{
		if (!gameObject.activeInHierarchy)
			return;

		TryHidingVisible();
		FlyingText instance = GetTextInstance(_iconSprite.GetHashCode());
		instance.config = new FlyingTextConfig(config);
		ApplyOutlineTo(instance);
		instance.SetRawText(null);
		instance.SetColorOverride(_colorOverride);
		instance.SetIconAndMergeIndex(_iconSprite, _iconScale);

		InstanceDoAnimation(instance, _value, _blink, _playPS);
	}

	public void ShowNumber ( float _value, int _mergeIndex = 0, bool _blink = false, bool _playPS = false )
	{
		if (!gameObject.activeInHierarchy)
			return;

		TryHidingVisible();
		FlyingText instance = GetTextInstance(_mergeIndex);
		instance.config = new FlyingTextConfig(config);
		ApplyOutlineTo(instance);
		instance.SetRawText(null);
		instance.SetColorOverride(null);
		instance.DisableIconAndSetMergeIndex(_mergeIndex);

		InstanceDoAnimation(instance, _value, _blink, _playPS);
	}

	public void ShowNumber ( float _value, bool _blink = false, bool _playPS = false )
	{
		ShowNumber(_value, 0, _blink, _playPS);
	}

	void ApplyOutlineTo ( FlyingText _instance )
	{
		_instance.SetOutline(m_useOutline ? m_outlineColor : (Color?)null, m_outlineWidth);
	}

	void InstanceDoAnimation ( FlyingText _instance, float _value, bool _blink, bool _playPS )
	{
		if (config.yAddedOffsetOnOverLap > 0f && ShowOrIdleCount > 0)
		{
			m_currentOverlapOffset = config.yAddedOffsetOnOverLap * ShowOrIdleCount;
			if (m_currentOverlapOffset > config.yOffsetOnOverLapMax)
				m_currentOverlapOffset = config.yOffsetOnOverLapMax;
		}

		_instance.DoAnimation(_value, m_currentOverlapOffset, _blink, _playPS);
	}

	void TryHidingVisible ()
	{
		if (config.hideLastWhenSpawningNew)
		{
			for (int i = 0; i < m_flyingTextList.Count; i++)
			{
				if (m_flyingTextList[i].CurrentState == UIFlyingText.TextState.Showing || m_flyingTextList[i].CurrentState == UIFlyingText.TextState.Idle)
					m_flyingTextList[i].SkipAndHide();
			}
		}
	}

	FlyingText GetTextInstance ( int _mergeIndex )
	{
		if (config.mergeVisibleNumbers)
		{
			for (int i = m_flyingTextList.Count - 1; i >= 0; i--)
			{
				if (m_flyingTextList[i].isMergable && m_flyingTextList[i].MergeIndex == _mergeIndex) //mergable ?
					return m_flyingTextList[i];
			}
		}

		for (int i = 0; i < m_flyingTextList.Count; i++)
		{
			if (!m_flyingTextList[i].isPlaying) //first is usable ?
				return m_flyingTextList[i];
		}

		FlyingText instance = Instantiate((m_UIflyingTextPrefab ?? m_flyingTextPrefab), m_flyingTextParent);
		instance.onFinished += OnInstanceFinished;
		m_flyingTextList.Add(instance);
		return instance;
	}

	void OnInstanceFinished ()
	{
		if (ShowOrIdleCount == 0)
			m_currentOverlapOffset = 0f;
	}
}