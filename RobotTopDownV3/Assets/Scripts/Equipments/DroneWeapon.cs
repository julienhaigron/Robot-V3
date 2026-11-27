using UnityEngine;

public class DroneWeapon : BulletWeapon
{
	[Header("Drone")]
	[SerializeField] private Transform m_drone;
	[SerializeField] private Transform[] m_rotors;
	[SerializeField] private Vector3 m_hoverOffset = new(.45f, 2.3f, -.15f);
	[SerializeField] private float m_deployedHeight = 1.6f;
	[SerializeField] private float m_bobAmplitude = .07f;
	[SerializeField] private float m_bobSpeed = 2.4f;
	[SerializeField] private float m_flightDuration = .6f;
	[SerializeField] private float m_flightArc = .6f;
	[SerializeField] private float m_turnSpeed = 720f;
	[SerializeField] private float m_aimHold = 1.2f;
	[SerializeField] private float m_rotorSpeed = 1440f;
	[SerializeField] private float m_recoilDistance = .15f;
	[SerializeField] private float m_recoilRecovery = 8f;

	private Tile m_deployedTile;
	private int m_remainingDeployTicks;
	private int m_sentryRadius;
	private Vector3 m_aimPoint;
	private float m_aimTime = float.NegativeInfinity;
	private Vector3 m_flightFrom;
	private float m_flightProgress = 1f;
	private Vector3 m_kick;

	public bool IsDeployed => m_deployedTile != null;
	public override bool DrivesUserPose => false;

	private bool IsShown => m_user != null && !m_user.Equipment.IsDead
		&& (m_user.IsAlliedTo(GameManager.Instance.PlayerID) || (IsDeployed ? m_deployedTile.IsVisible : m_user.IsShownToLocalPlayer));

	private Quaternion Facing => m_user.SkinParent.transform.rotation;
	private Vector3 HoverAnchor => m_user.Displacement.FeetPosition + Facing * m_hoverOffset;
	private Vector3 Anchor => IsDeployed ? m_deployedTile.transform.position + Vector3.up * m_deployedHeight : HoverAnchor;

	public override void Init ( Entity _user, WeaponEquipmentData _data, string _id )
	{
		base.Init(_user, _data, _id);
		if (m_drone != null)
		{
			m_drone.SetParent(_user.transform, true);
			m_drone.position = HoverAnchor;
		}

		TurnManager.onNewRoundStart -= OnTick;
		TurnManager.onNewRoundStart += OnTick;
	}

	protected override void OnDestroy ()
	{
		base.OnDestroy();
		TurnManager.onNewRoundStart -= OnTick;
		if (m_drone != null)
			Destroy(m_drone.gameObject);
	}

	public bool IsDockedAt ( AEntityAction _action )
	{
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
			return !IsDeployed;

		int tick = _action.TimeAtStartPerform;
		if (IsDeployed && tick < m_remainingDeployTicks)
			return false;

		if (!TurnManager.Instance.RecordedActions.TryGetValue(m_user.ID, out var queued))
			return true;

		foreach (TurnManager.RecordedAction recorded in queued)
		{
			if (recorded.action is not DeployDroneAction deploy || deploy == _action || deploy.linkedEquipmentId != m_id)
				continue;

			int deployTick = deploy.TimeAtStartPerform;
			if (tick > deployTick && tick <= deployTick + deploy.Data.deployDuration)
				return false;
		}

		return true;
	}

	public void Deploy ( Tile _tile, int _radius, int _duration )
	{
		if (_tile == null || m_user == null || m_user.Equipment.IsDead)
			return;

		StartFlight();
		m_deployedTile = _tile;
		m_sentryRadius = _radius;
		m_remainingDeployTicks = _duration;
	}

	private void Recall ()
	{
		StartFlight();
		m_deployedTile = null;
		if (m_user != null && !m_user.Equipment.IsDead)
			LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_drone_recalled), m_user.Data.name), LogConsole.LogEventType.ActionResolution);
	}

	private void StartFlight ()
	{
		if (m_drone == null)
			return;

		m_flightFrom = m_drone.position;
		m_flightProgress = 0f;
	}

	private void OnTick ()
	{
		if (!IsDeployed)
			return;

		if (m_user == null || m_user.Equipment.IsDead || m_remainingDeployTicks <= 0)
		{
			Recall();
			return;
		}

		m_remainingDeployTicks--;
		FireSentryShot();
	}

	private void FireSentryShot ()
	{
		if (!TryGetShotAction(out EntityActionData shotData) || !TryGetSentryTarget(out Entity target))
			return;

		Tile targetTile = target.Displacement.Coordinates.GetTile();
		DroneSentryShotAction shot = new();
		shot.Init(shotData, m_id, m_user.ID, m_user.Displacement.Coordinates.ID, TurnManager.currentTick);
		shot.originTileID = m_deployedTile.coordinates.ID;
		shot.hasClaimedStatusCharges = true;
		shot.targetTileIDs = new[] { targetTile.coordinates.ID };
		shot.targetedEntityIDs = new[] { target.ID };
		shot.orientationAtPerform = GridManager.Instance.GetClosestOrientation(m_deployedTile, targetTile);
		shot.ConflictCheckPrewarm();
		shot.Prepare(m_user.State);

		TurnManager.RecordedEvent sentryEvent = new("Drone sentry " + m_user.Data.name);
		TurnManager.Instance.AddGameEvent(sentryEvent);
		QueueAttack(shot, sentryEvent.EndEvent);
	}

	private bool TryGetShotAction ( out EntityActionData _data )
	{
		_data = null;
		foreach (EntityActionEnumID actionID in m_user.KnownedActions)
		{
			EntityActionData data = GameAssets.current.game.entityActionsData[actionID];
			if (data.codeType == EntityActionData.ActionCodeType.Attack
				&& m_user.ComponentLinkedToAction.TryGetValue(actionID, out var componentIDs) && componentIDs.Contains(m_id))
			{
				_data = data;
				return true;
			}
		}

		return false;
	}

	private bool TryGetSentryTarget ( out Entity _target )
	{
		_target = null;
		int bestDistance = int.MaxValue;
		foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(m_deployedTile, 1, m_sentryRadius, false, true, true))
		{
			if (!tile.TryGetCurrentEntity(out Entity entity) || entity.Equipment.IsDead || entity.IsImmortal || entity.IsAlliedTo(m_user.OwnerID))
				continue;

			int distance = m_deployedTile.coordinates.DistanceTo(tile.coordinates);
			if (distance > bestDistance || (distance == bestDistance && entity.Equipment.CurrentHealth >= _target.Equipment.CurrentHealth))
				continue;

			_target = entity;
			bestDistance = distance;
		}

		return _target != null;
	}

	public override void PointAt ( Vector3 _target )
	{
		m_aimPoint = _target;
		m_aimTime = Time.time;
	}

	public override void PlayShotRecoil ( EntityActionData _data )
	{
		if (m_drone != null && _data != null && _data.shotRecoilStrength > 0f)
			m_kick = -m_drone.forward * m_recoilDistance * _data.shotRecoilStrength;
	}

	private void LateUpdate ()
	{
		if (m_drone == null || m_user == null)
			return;

		bool isShown = IsShown;
		if (m_drone.gameObject.activeSelf != isShown)
			m_drone.gameObject.SetActive(isShown);
		if (!isShown)
			return;

		Vector3 target = Anchor + Vector3.up * (Mathf.Sin(Time.time * m_bobSpeed) * m_bobAmplitude);
		if (m_flightProgress < 1f)
		{
			m_flightProgress = Mathf.Min(1f, m_flightProgress + Time.deltaTime / Mathf.Max(.01f, m_flightDuration));
			float eased = Mathf.SmoothStep(0f, 1f, m_flightProgress);
			target = Vector3.Lerp(m_flightFrom, target, eased) + Vector3.up * (Mathf.Sin(eased * Mathf.PI) * m_flightArc);
		}

		m_kick = Vector3.Lerp(m_kick, Vector3.zero, Time.deltaTime * m_recoilRecovery);
		m_drone.position = target + m_kick;

		Vector3 look = Time.time - m_aimTime < m_aimHold ? m_aimPoint - m_drone.position : Facing * Vector3.forward;
		look.y = 0f;
		if (look.sqrMagnitude > .0001f)
			m_drone.rotation = Quaternion.RotateTowards(m_drone.rotation, Quaternion.LookRotation(look), m_turnSpeed * Time.deltaTime);

		foreach (Transform rotor in m_rotors)
		{
			if (rotor != null)
				rotor.Rotate(0f, m_rotorSpeed * Time.deltaTime, 0f, Space.Self);
		}
	}
}
