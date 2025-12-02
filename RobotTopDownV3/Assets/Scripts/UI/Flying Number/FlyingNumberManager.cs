using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingNumberManager : MonoBehaviour
{
	[System.Serializable]
	public class FlyingNumberConfig
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
		public GraphicColorAnimation.AnimationType defaultColorAnimation = GraphicColorAnimation.AnimationType.RedLimitedCountBlink;
		public bool useAnimationCustomColor = false;
		public Color customColorAnimation = Color.red;

		public FlyingNumberConfig () { }
		public FlyingNumberConfig ( FlyingNumberConfig _ref )
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
			defaultColorAnimation = _ref.defaultColorAnimation;
			useAnimationCustomColor = _ref.useAnimationCustomColor;
			customColorAnimation = _ref.customColorAnimation;
		}
	}

	public FlyingNumberConfig config;

	[Space()]
	[InfoBox("both UI and world prefab ref set, plz choose one et remove the other", InfoMessageType.Warning, VisibleIf = "@m_flyingNumberPrefab != null && m_UIflyingNumberPrefab != null")]
	[SerializeField] private UIFlyingNumber m_UIflyingNumberPrefab;
	[SerializeField] private FlyingNumber m_flyingNumberPrefab;
	[SerializeField] private int m_poolBufferedSize = 1;
	[Space()]
	[SerializeField] private Transform m_flyingNumberParent;
	[SerializeField, ReadOnly] private List<FlyingNumber> m_flyingNumberList;

	[Button(), ShowIf("@m_flyingNumberList.Count > 0")]
	void ClearFlyingList () { m_flyingNumberList.Clear(); }

	private void Awake ()
	{
#if UNITY_EDITOR
		if (m_flyingNumberPrefab != null && m_UIflyingNumberPrefab != null)
		{
			Debug.LogWarning(gameObject.name + " flyingNumber has both UI and world prefab, plz choose one et remove the other");
		}
#endif

		if (m_poolBufferedSize > 0)
		{
			if (m_flyingNumberPrefab != null)
			{
				for (int i = 0; i < m_poolBufferedSize; i++)
				{
					FlyingNumber instance = Instantiate(m_flyingNumberPrefab, m_flyingNumberParent);
					instance.onFinished += OnInstanceFinished;
					instance.OnHideFinished();
					m_flyingNumberList.Add(instance);
				}
			}
			else if (m_UIflyingNumberPrefab != null)
			{
				for (int i = 0; i < m_poolBufferedSize; i++)
				{
					FlyingNumber instance = Instantiate(m_UIflyingNumberPrefab, m_flyingNumberParent);
					instance.onFinished += OnInstanceFinished;
					instance.OnHideFinished();
					m_flyingNumberList.Add(instance);
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
			for (int i = 0; i < m_flyingNumberList.Count; i++)
			{
				if (m_flyingNumberList[i].isPlaying)
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
			for (int i = 0; i < m_flyingNumberList.Count; i++)
			{
				if (m_flyingNumberList[i].CurrentState == UIFlyingNumber.NumberState.Showing || m_flyingNumberList[i].CurrentState == UIFlyingNumber.NumberState.Idle)
					count++;
			}
			return count;
		}
	}
	private float m_currentOverlapOffset = 0f;

	/*public void ShowNumber ( float _value, CurrencyType _currencyType, bool _blink = false, bool _playPS = false, float _iconScale = 1f )
	{
		if (!gameObject.activeInHierarchy)
			return;

		TryHidingVisible();
		FlyingNumber instance = GetNumberInstance(GameAssets.current.currencies[_currencyType].icon.GetHashCode());
		instance.config = new FlyingNumberConfig(config);
		instance.SetIconAndMergeIndex(_currencyType, _iconScale);

		InstanceDoAnimation(instance, _value, _blink, _playPS);
	}*/

	public void ShowNumber ( float _value, Sprite _iconSprite, bool _blink = false, bool _playPS = false, float _iconScale = 1f )
	{
		if (!gameObject.activeInHierarchy)
			return;

		TryHidingVisible();
		FlyingNumber instance = GetNumberInstance(_iconSprite.GetHashCode());
		instance.config = new FlyingNumberConfig(config);
		instance.SetIconAndMergeIndex(_iconSprite, _iconScale);

		InstanceDoAnimation(instance, _value, _blink, _playPS);
	}

	public void ShowNumber ( float _value, int _mergeIndex = 0, bool _blink = false, bool _playPS = false )
	{
		if (!gameObject.activeInHierarchy)
			return;

		TryHidingVisible();
		FlyingNumber instance = GetNumberInstance(_mergeIndex);
		instance.config = new FlyingNumberConfig(config);
		instance.DisableIconAndSetMergeIndex(_mergeIndex);

		InstanceDoAnimation(instance, _value, _blink, _playPS);
	}

	public void ShowNumber ( float _value, bool _blink = false, bool _playPS = false )
	{
		ShowNumber(_value, 0, _blink, _playPS);
	}

	void InstanceDoAnimation ( FlyingNumber _instance, float _value, bool _blink, bool _playPS )
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
			for (int i = 0; i < m_flyingNumberList.Count; i++)
			{
				if (m_flyingNumberList[i].CurrentState == UIFlyingNumber.NumberState.Showing || m_flyingNumberList[i].CurrentState == UIFlyingNumber.NumberState.Idle)
					m_flyingNumberList[i].SkipAndHide();
			}
		}
	}

	FlyingNumber GetNumberInstance ( int _mergeIndex )
	{
		if (config.mergeVisibleNumbers)
		{
			for (int i = m_flyingNumberList.Count - 1; i >= 0; i--)
			{
				if (m_flyingNumberList[i].isMergable && m_flyingNumberList[i].MergeIndex == _mergeIndex) //mergable ?
					return m_flyingNumberList[i];
			}
		}

		for (int i = 0; i < m_flyingNumberList.Count; i++)
		{
			if (!m_flyingNumberList[i].isPlaying) //first is usable ?
				return m_flyingNumberList[i];
		}

		//when no instance usable or mergable add one
		FlyingNumber instance = Instantiate((m_UIflyingNumberPrefab ?? m_flyingNumberPrefab), m_flyingNumberParent);
		instance.onFinished += OnInstanceFinished;
		m_flyingNumberList.Add(instance);
		return instance;
	}

	void OnInstanceFinished ()
	{
		if (ShowOrIdleCount == 0)
			m_currentOverlapOffset = 0f;
	}

#if UNITY_EDITOR
	[FoldoutGroup("Test")]
	[SerializeField] private float m_testValue = 1f;
	[FoldoutGroup("Test")]
	[SerializeField] private bool m_testBlink;
	[FoldoutGroup("Test")]
	[SerializeField] private bool m_playPS;

	/*[FoldoutGroup("Test")]
	[Button("test A")]
	void TestCurrencyA ( CurrencyType _testCurrency )
	{
		if (_testCurrency != CurrencyType.Unknown)
			ShowNumber(m_testValue, _testCurrency, m_testBlink, m_playPS);
		else
			ShowNumber(m_testValue, m_testBlink, m_playPS);
	}

	[FoldoutGroup("Test")]
	[Button("test B")]
	void TestCurrencyB ( CurrencyType _testCurrency )
	{
		if (_testCurrency != CurrencyType.Unknown)
			ShowNumber(m_testValue, _testCurrency, m_testBlink, m_playPS);
		else
			ShowNumber(m_testValue, m_testBlink, m_playPS);
	}*/
#endif
}