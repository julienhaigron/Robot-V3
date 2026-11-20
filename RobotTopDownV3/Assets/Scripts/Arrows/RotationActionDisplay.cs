using UnityEngine;

public class RotationActionDisplay : GroundOrderDisplay
{
	private const float c_degreesPerSample = 6f;

	[Header("Arc")]
	[SerializeField] private SpriteRenderer m_startDot;
	[SerializeField] private float m_radius = .79f;
	[SerializeField] private float m_pathClearance = .2f;
	[SerializeField] private Color m_rotationColor = new(.72f, .35f, 1f, 1f);
	[SerializeField] private Color m_startDotColor = new(.96f, .97f, .98f, 1f);

	public static void Show ( int _performingEntityID, Tile _pivot, int _fromOrientation, int _toOrientation, bool _isGhost )
	{
		Show(_performingEntityID, _pivot, _fromOrientation, _toOrientation, _isGhost, false);
	}

	public static void ShowMovementTurn ( int _performingEntityID, Tile _pivot, int _fromOrientation, int _toOrientation )
	{
		Show(_performingEntityID, _pivot, _fromOrientation, _toOrientation, true, true);
	}

	private static void Show ( int _performingEntityID, Tile _pivot, int _fromOrientation, int _toOrientation, bool _isGhost, bool _endsOnPath )
	{
		if (_pivot == null || _toOrientation < 0
			|| ObjectsPooling.GetElement(GameAssets.current.game.rotationHandlePoolData, Vector3.zero, Quaternion.identity) is not RotationActionDisplay display)
			return;

		display.Init(_pivot.transform.position, _fromOrientation < 0 ? _toOrientation : _fromOrientation, _toOrientation, _isGhost, _endsOnPath);
		PlayerController.Instance.AddRotationActionDisplay(display, _performingEntityID);
	}

	private void Init ( Vector3 _pivot, int _fromOrientation, int _toOrientation, bool _isGhost, bool _endsOnPath )
	{
		SetStyle(m_rotationColor, _isGhost);

		int steps = ((_toOrientation - _fromOrientation) % 6 + 6) % 6;
		if (steps == 0)
		{
			ClearStrip();
			m_startDot.enabled = false;
			Vector3 facing = RotateEntityAction.GetFacingRotation(_toOrientation) * Vector3.forward;
			ShowHead(_pivot + facing * (m_radius + HeadLength * .5f), facing);
			return;
		}

		Quaternion start = RotateEntityAction.GetFacingRotation(_fromOrientation);
		float sweep = (steps <= 3 ? steps : steps - 6) * 60f;
		float sign = Mathf.Sign(sweep);
		float tip = _endsOnPath ? sweep - sign * m_pathClearance / m_radius * Mathf.Rad2Deg : sweep;
		float arcEnd = tip - sign * HeadLength * c_stripEndInHead / m_radius * Mathf.Rad2Deg;
		int samples = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(arcEnd) / c_degreesPerSample));

		m_points.Clear();
		for (int i = 0; i <= samples; i++)
			m_points.Add(_pivot + start * Quaternion.Euler(0f, arcEnd * i / samples, 0f) * Vector3.forward * m_radius);
		DrawStrip();

		Quaternion headRotation = start * Quaternion.Euler(0f, tip - sign * HeadLength * .5f / m_radius * Mathf.Rad2Deg, 0f);
		ShowHead(_pivot + start * Quaternion.Euler(0f, tip, 0f) * Vector3.forward * m_radius, sign * (headRotation * Vector3.right));
		PlaceFlat(m_startDot, _pivot + start * Vector3.forward * m_radius, start * Vector3.forward, Faded(m_startDotColor));
	}
}
