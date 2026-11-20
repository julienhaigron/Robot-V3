using UnityEngine;

public class RotationActionDisplay : GroundDisplay
{
	[Header("Arc")]
	[SerializeField] private SpriteRenderer m_startDot;
	[SerializeField] private float m_radius = .79f;
	[SerializeField] private float m_shieldTurnRadius = 1.14f;
	[SerializeField] private float m_pathClearance = .2f;
	[SerializeField] private Color m_rotationColor = new(.72f, .35f, 1f, 1f);
	[SerializeField] private Color m_startDotColor = new(.96f, .97f, .98f, 1f);

	public static void Show ( int _performingEntityID, Tile _pivot, int _fromOrientation, int _toOrientation, bool _isGhost )
	{
		if (TryGet(_pivot, _toOrientation, out RotationActionDisplay display))
			display.Draw(_performingEntityID, _pivot, _fromOrientation, _toOrientation, display.m_rotationColor, display.m_radius, _isGhost, false);
	}

	public static void ShowMovementTurn ( int _performingEntityID, Tile _pivot, int _fromOrientation, int _toOrientation )
	{
		if (TryGet(_pivot, _toOrientation, out RotationActionDisplay display))
			display.Draw(_performingEntityID, _pivot, _fromOrientation, _toOrientation, display.m_rotationColor, display.m_radius, true, true);
	}

	public static void ShowShieldTurn ( int _performingEntityID, Tile _pivot, int _fromOrientation, int _toOrientation, Color _color, bool _isGhost )
	{
		if (TryGet(_pivot, _toOrientation, out RotationActionDisplay display))
			display.Draw(_performingEntityID, _pivot, _fromOrientation, _toOrientation, _color, display.m_shieldTurnRadius, _isGhost, false);
	}

	private static bool TryGet ( Tile _pivot, int _toOrientation, out RotationActionDisplay _display )
	{
		_display = _pivot == null || _toOrientation < 0 ? null
			: ObjectsPooling.GetElement(GameAssets.current.game.rotationHandlePoolData, Vector3.zero, Quaternion.identity) as RotationActionDisplay;
		return _display != null;
	}

	private void Draw ( int _performingEntityID, Tile _pivot, int _fromOrientation, int _toOrientation, Color _color, float _radius, bool _isGhost, bool _endsOnPath )
	{
		Init(_pivot.transform.position, _fromOrientation < 0 ? _toOrientation : _fromOrientation, _toOrientation, _color, _radius, _isGhost, _endsOnPath);
		PlayerController.Instance.AddOrderDisplay(this, _performingEntityID);
	}

	private void Init ( Vector3 _pivot, int _fromOrientation, int _toOrientation, Color _color, float _radius, bool _isGhost, bool _endsOnPath )
	{
		SetStyle(_color, _isGhost);

		int steps = ((_toOrientation - _fromOrientation) % 6 + 6) % 6;
		if (steps == 0)
		{
			ClearStrip();
			m_startDot.enabled = false;
			Vector3 facing = RotateEntityAction.GetFacingRotation(_toOrientation) * Vector3.forward;
			ShowHead(_pivot + facing * (_radius + HeadLength * .5f), facing);
			return;
		}

		Quaternion start = RotateEntityAction.GetFacingRotation(_fromOrientation);
		float startYaw = RotateEntityAction.GetFacingYaw(_fromOrientation);
		float sweep = (steps <= 3 ? steps : steps - 6) * 60f;
		float sign = Mathf.Sign(sweep);
		float tip = _endsOnPath ? sweep - sign * m_pathClearance / _radius * Mathf.Rad2Deg : sweep;
		float arcEnd = tip - sign * HeadLength * c_stripEndInHead / _radius * Mathf.Rad2Deg;

		Strip.Clear();
		Strip.AddArc(_pivot, _radius, startYaw, startYaw + arcEnd, StripHalfWidth, _color);
		Strip.Apply();

		Quaternion headRotation = start * Quaternion.Euler(0f, tip - sign * HeadLength * .5f / _radius * Mathf.Rad2Deg, 0f);
		ShowHead(_pivot + start * Quaternion.Euler(0f, tip, 0f) * Vector3.forward * _radius, sign * (headRotation * Vector3.right));
		PlaceFlat(m_startDot, _pivot + start * Vector3.forward * _radius, start * Vector3.forward, Faded(m_startDotColor));
	}
}
