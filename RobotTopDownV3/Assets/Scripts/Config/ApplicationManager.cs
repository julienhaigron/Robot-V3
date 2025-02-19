using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent()]
[RequireComponent(typeof(LoadingElement))]
public partial class ApplicationManager : SingletonPersistant<ApplicationManager>
{

	[Header("Objects")]
	[SerializeField] private GameAssets m_gameAssets;
	[SerializeField] private GameConfig m_gameConfig;

	public static GameAssets assets
	{
		get
		{
#if UNITY_EDITOR
			if (Instance == null)
				return Resources.Load<GameAssets>("GameAssets");
#endif
			return Instance.m_gameAssets;
		}
	}
	public static GameConfig config
	{
		get
		{
#if UNITY_EDITOR
			if (Instance == null)
				return Resources.Load<GameConfig>("GameConfig");
#endif
			return Instance.m_gameConfig;
		}
	}
}
