using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Linq;
using Sirenix.OdinInspector;

public class Shield : ToolEquipment
{
	public ShieldEquipmentData ShieldData => m_data as ShieldEquipmentData;

	public int orientation = 0;

	[SerializeField] private Transform m_orientationIndicatorPrefab;
	[SerializeField] private float m_orientationIndicatorHeight = .03f;
	[SerializeField] private Color m_allyIndicatorColor = new(.35f, .85f, 1f, 1f);
	[SerializeField] private Color m_enemyIndicatorColor = new(1f, .35f, .3f, 1f);

	private int m_currentHp;
	public int CurrentHp => m_currentHp;

	private Transform m_orientationIndicator;
	private bool m_isIndicatorColorSet;

	private static readonly int s_colorID = Shader.PropertyToID("_Color");

	public override void Init ( Entity _user, ToolEquipmentData _data, string _id, bool _isFirstSide )
	{
		base.Init(_user, _data, _id, _isFirstSide);

		orientation = _isFirstSide ? 5 : 2;
		m_currentHp = ShieldData.hp;

		if (m_orientationIndicatorPrefab != null && m_orientationIndicator == null)
		{
			m_orientationIndicator = Instantiate(m_orientationIndicatorPrefab);
			m_orientationIndicator.gameObject.SetActive(false);
		}
		m_isIndicatorColorSet = false;
	}

	private void LateUpdate ()
	{
		if (m_orientationIndicator == null)
			return;

		Tile tile = GetIndicatorTile();
		bool isShown = tile != null && m_currentHp > 0 && !m_user.Equipment.IsDead
			&& (m_user.IsAlliedTo(GameManager.Instance.PlayerID) || m_user.IsVisible);

		if (m_orientationIndicator.gameObject.activeSelf != isShown)
			m_orientationIndicator.gameObject.SetActive(isShown);
		if (!isShown)
			return;

		if (!m_isIndicatorColorSet)
			ApplyIndicatorColor(m_user.IsAlliedTo(GameManager.Instance.PlayerID) ? m_allyIndicatorColor : m_enemyIndicatorColor);

		Vector3 position = m_user.Skin != null && m_user.Skin.Center != null ? m_user.Skin.Center.position : m_user.transform.position;
		position.y = tile.transform.position.y + m_orientationIndicatorHeight;
		m_orientationIndicator.SetPositionAndRotation(position, Quaternion.Euler(0f, 30f + orientation * 60f, 0f));
	}

	private Tile GetIndicatorTile ()
	{
		if (m_user == null || GameManager.Instance == null || GridManager.Instance == null || GridManager.Instance.Tiles == null || m_user.Displacement == null)
			return null;

		int tileID = m_user.Displacement.Coordinates.ID;
		return tileID >= 0 && tileID < GridManager.Instance.Tiles.Length ? GridManager.Instance.Tiles[tileID] : null;
	}

	private void ApplyIndicatorColor ( Color _color )
	{
		m_isIndicatorColorSet = true;
		MaterialPropertyBlock block = new();
		foreach (Renderer renderer in m_orientationIndicator.GetComponentsInChildren<Renderer>(true))
		{
			renderer.GetPropertyBlock(block);
			block.SetColor(s_colorID, _color);
			renderer.SetPropertyBlock(block);
		}
	}

	private void OnDestroy ()
	{
		if (m_orientationIndicator != null)
			Destroy(m_orientationIndicator.gameObject);
	}

	public int RemoveDamage(int _damageAmount )
	{
		if (m_currentHp <= 0)
			return 0;

		int amountRemoved = Mathf.Min(_damageAmount, m_currentHp);
		m_currentHp -= amountRemoved;
		return amountRemoved;
	}

	public override void PerformAction ( SpecialAction _specialAction, Action _onPerformEnd )
	{

		if(_specialAction is TurnShieldAction turnAction)
		{
			orientation = turnAction.targetedOrientation;
		}

		DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => _onPerformEnd?.Invoke());
	}
}
