using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Linq;
using Sirenix.OdinInspector;

public class Weapon : MonoBehaviour
{

	[Title("Dependencies")]
	protected WeaponEquipmentData m_data;
	public WeaponEquipmentData Data => m_data;

	[SerializeField] protected List<ParticleSystem> m_onPerformPS;

	protected Entity m_user;
	protected string m_id;
	public string ID => m_id;
	protected Coroutine m_attackCR;
	protected AttackAction m_lastPerformedAction;
	protected Action m_onPerformAttackEnd;
	protected HashSet<Tile> m_targetedTiles = new();

	private float m_pace = 1f;
	private const float c_minPace = .2f;
	protected float Pace => m_pace;
	private readonly Dictionary<ParticleSystem, Quaternion> m_onPerformPSRelativeRotations = new();

	private const float c_tileAimHeight = 1f;
	private const float c_missOffset = .7f;
	private const float c_sprayJitter = .25f;
	private const float c_attackProjectileTimeout = 4f;

	protected virtual Transform LaunchPoint => transform.parent != null ? transform.parent : transform;

	public Pose LaunchPose
	{
		get
		{
			Transform point = LaunchPoint;
			return m_user != null && m_user.Skin != null && m_user.Skin.IK != null ? m_user.Skin.IK.RenderedPoseOf(point) : new Pose(point.position, point.rotation);
		}
	}

	public Vector3 ProjectileOrigin => LaunchPose.position;

	public static void PlayOrientedVFX ( PoolData _pool, Vector3 _position, Vector3 _direction, string _context )
	{
		if (_pool == null)
			return;

		try
		{
			Quaternion rotation = _direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(_direction) : Quaternion.identity;
			_pool.Get(_position, rotation);
		}
		catch (Exception ex)
		{
			Debug.LogError("VFX " + _pool.name + " failed for " + _context + ": " + ex.Message);
		}
	}

	public class WeaponTarget
	{
		public Entity targetEntity;
		public Tile targetTile;

		public Entity ResolvedEntity => targetEntity != null ? targetEntity : targetTile != null ? targetTile.GetCurrentEntity() : null;
	}

	private sealed class AttackFlight
	{
		public VisualProjectile projectile;
		public readonly List<Vector3> waypoints = new();
		public readonly List<WeaponTarget[]> hits = new();
		public float launchTime;
		public bool doesReturn;
		public bool isLaunched;
		public bool isDone;
		public int nextIndex = 1;
		public Vector3 legStart;
		public float legStartTime;
		public float legDuration;

		public void Add ( Vector3 _point, params WeaponTarget[] _hits )
		{
			waypoints.Add(_point);
			hits.Add(_hits);
		}
	}

	public virtual void Init ( Entity _user, WeaponEquipmentData _data, string _id )
	{
		m_user = _user;
		m_data = _data;
		m_id = _id;
	}

	protected virtual void Awake ()
	{
		CaptureAttackFeedbackPoses();
		SilenceAttackFeedback();
	}

	private void CaptureAttackFeedbackPoses ()
	{
		if (m_onPerformPS == null)
			return;

		foreach (ParticleSystem ps in m_onPerformPS)
		{
			if (ps != null && !m_onPerformPSRelativeRotations.ContainsKey(ps))
				m_onPerformPSRelativeRotations.Add(ps, Quaternion.Inverse(transform.rotation) * ps.transform.rotation);
		}
	}

	protected virtual void OnEnable ()
	{
		SilenceAttackFeedback();
	}

	private void SilenceAttackFeedback ()
	{
		if (m_onPerformPS == null)
			return;

		foreach (ParticleSystem ps in m_onPerformPS)
		{
			if (ps == null)
				continue;

			ParticleSystem.MainModule main = ps.main;
			main.playOnAwake = false;
			ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}
	}

	private void OnDestroy ()
	{
		if (m_attackCR != null && GameManager.Instance != null)
			GameManager.Instance.StopCoroutine(m_attackCR);
	}

	public virtual void PerformAttack ( AttackAction _attackAction, Action _onPerformEnd )
	{
		m_lastPerformedAction = _attackAction;
		m_onPerformAttackEnd = _onPerformEnd;

		if (m_attackCR != null)
			GameManager.Instance.StopCoroutine(m_attackCR);

		m_attackCR = GameManager.Instance.StartCoroutine(PerformAttackCR(_attackAction));
	}

	protected IEnumerator PerformAttackCR ( AttackAction _attackAction )
	{
		if (_attackAction.attacksInfos == null)
			_attackAction.ConflictCheckPrewarm();

		if (_attackAction.attacksInfos == null || _attackAction.attacksInfos.Length == 0)
		{
			EndAttack(_attackAction);
			m_attackCR = null;
			yield break;
		}

		m_pace = GetPace(_attackAction);
		int lastSuccessfullAttackIndex = -1;
		for (int i = 0; i < _attackAction.attacksInfos.Length; i++)
		{
			if (_attackAction.attacksInfos[i].isAttackSuccessfull)
				lastSuccessfullAttackIndex = i;
		}
		OnStartAttacking(_attackAction);

		for (int attackIndex = 0; attackIndex < _attackAction.attacksInfos.Length; attackIndex++)
		{
			AttackAction.SingleAttackInfo attackInfo = _attackAction.attacksInfos[attackIndex];

			OnStartSingleAttack(_attackAction, attackIndex, attackInfo);

			yield return AimTargetCR(_attackAction, attackIndex, attackInfo);

			yield return PerformSingleAttackCR(_attackAction, attackIndex, attackInfo, lastSuccessfullAttackIndex);

			OnEndSingleAttack(_attackAction, attackIndex, attackInfo);
		}

		yield return OnEndAttackingCR(_attackAction);

		if (lastSuccessfullAttackIndex == -1)
			EndAttack(_attackAction);

		m_attackCR = null;
	}

	private float GetPace ( AttackAction _attackAction )
	{
		float estimate = EstimateAttackDuration(_attackAction);
		float budget = _attackAction.TickTimeLeft;
		return estimate > budget ? Mathf.Max(c_minPace, budget / estimate) : 1f;
	}

	protected float Paced ( float _seconds )
	{
		return _seconds * m_pace;
	}

	protected virtual float EstimateAttackDuration ( AttackAction _attackAction )
	{
		EntityActionData data = _attackAction.Data;
		float duration = 0f;
		for (int i = 0; i < _attackAction.attacksInfos.Length; i++)
			duration += data.performAnimationHitDelay + EstimateFeedbackFlight(data, GetDistanceToTarget(_attackAction, i)) + m_data.singleAttackAnimationDuration;

		return duration;
	}

	protected float GetDistanceToTarget ( AttackAction _attackAction, int _attackIndex )
	{
		Tile tile = _attackAction.GetTargetTileAt(_attackIndex);
		return tile != null ? Vector3.Distance(ProjectileOrigin, tile.transform.position) : 0f;
	}

	private static float EstimateFeedbackFlight ( EntityActionData _data, float _distance )
	{
		if (_data.attackProjectileMode == EntityActionData.AttackProjectileMode.None || _data.attackProjectilePool == null)
			return 0f;

		float flight = _distance / Mathf.Max(1f, _data.attackProjectileSpeed) * (_data.doesAttackProjectileReturn ? 2f : 1f);
		if (_data.attackProjectileMode == EntityActionData.AttackProjectileMode.Spray)
			flight += Mathf.Max(0, _data.attackProjectileSprayCount - 1) * Mathf.Max(0f, _data.attackProjectileSprayInterval);

		return flight;
	}

	#region ATTACK FLOW

	protected virtual void OnStartAttacking ( AttackAction _attackAction )
	{

	}

	protected virtual void OnStartSingleAttack ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{

	}

	protected virtual IEnumerator AimTargetCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{
		yield return null;
	}

	protected virtual IEnumerator PerformSingleAttackCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo, int _lastSuccessfullAttackIndex )
	{
		yield return ApplyAttackCR(_attackAction, _attackIndex, _attackInfo);

		if (_attackIndex == _lastSuccessfullAttackIndex)
			EndAttack(m_lastPerformedAction);
	}

	protected virtual void OnEndSingleAttack ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{

	}

	protected virtual IEnumerator OnEndAttackingCR ( AttackAction _attackAction )
	{
		yield return null;
	}

	#endregion

	#region DAMAGE LOGIC

	protected virtual IEnumerator ApplyAttackCR ( AttackAction _attackAction, int _attackIndex, AttackAction.SingleAttackInfo _attackInfo )
	{
		bool isFinalStrike = _attackAction.IsLastPerformTick && (_attackAction.attacksInfos == null || _attackIndex >= _attackAction.attacksInfos.Length - 1);
		if (PlayAttackAnimation(_attackAction, _attackInfo, isFinalStrike, out float impactDelay) && impactDelay > 0f)
			yield return new WaitForSeconds(impactDelay);

		if (!_attackInfo.isAttackSuccessfull)
		{
			Entity missedTarget = _attackAction.GetTargetEntityAt(_attackIndex);
			Vector3 missPoint = GetMissPoint(_attackAction, _attackIndex, missedTarget);
			PlayPerformFeedback(_attackAction, missPoint);
			yield return PlayAttackProjectilesCR(_attackAction, new List<WeaponTarget>(), missPoint, null);
			ShowMissOn(missedTarget, _attackInfo.didTargetDodge);
			yield break;
		}

		List<WeaponTarget> targets = GetTargets(_attackAction, _attackIndex);
		Dictionary<WeaponEquipmentData.DamageType, int> damages = BuildDamageDictionary(_attackInfo);
		Vector3 strikePoint = GetStrikePoint(_attackAction, _attackIndex, targets);
		HashSet<WeaponTarget> hitTargets = new();

		PlayPerformFeedback(_attackAction, strikePoint);

		Action<WeaponTarget> hit = ( target ) => HitTarget(_attackAction, _attackInfo, target, damages, hitTargets);
		yield return PlayAttackProjectilesCR(_attackAction, targets, strikePoint, hit);
		NotifyHits(hit, targets.ToArray());

		ShowAoEDodges(_attackAction, targets);

		if (_attackAction.Data.type == EntityActionData.ActionType.MeleeAttack)
			DamageWallsInWeaponRange(_attackAction, damages);

		foreach (AEntityPassiveEffect.PassiveEffectContainer pe in m_lastPerformedAction.effects)
		{
			List<Entity> effectsTargets = GetPassiveEffectTargets(_attackAction, pe, _attackIndex);
			foreach (Entity entity in effectsTargets)
				ApplyEffects(entity, pe);
		}

		yield return new WaitForSeconds(Paced(m_data.singleAttackAnimationDuration));
	}

	private void HitTarget ( AttackAction _attackAction, AttackAction.SingleAttackInfo _attackInfo, WeaponTarget _target, Dictionary<WeaponEquipmentData.DamageType, int> _damages, HashSet<WeaponTarget> _hitTargets )
	{
		if (_target == null || !_hitTargets.Add(_target))
			return;

		PlayHitVFX(_attackAction, _target);

		Entity targetEntity = _target.ResolvedEntity;
		if (targetEntity == null)
			return;

		int hitAmount = _attackAction.Data.GetHitAmount(_attackAction, m_user, targetEntity);
		for (int i = 0; i < hitAmount; i++)
		{
			DealDamage(_attackAction, _attackInfo, targetEntity, _damages);
			ApplyStatuses(targetEntity, _attackInfo);
		}
	}

	protected void DealDamage ( AttackAction _attackAction, AttackAction.SingleAttackInfo _attackInfo, Entity _entity, Dictionary<WeaponEquipmentData.DamageType, int> _damages )
	{
		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
		{
			entityAttacker = m_user,
			entityTargeted = _entity,
			damages = _damages,
			shotOrigin = _attackInfo.ShotOrigin,
			hitTile = _attackAction.GetExchangeTileOf(_entity)
		});
	}

	private void PlayHitVFX ( AttackAction _attackAction, WeaponTarget _target )
	{
		if (_attackAction.Data.onSingleAttackHitVFXPool == null)
			return;

		bool isZone = _attackAction.Data.aoeType != EntityActionData.AOEType.Noone;
		Entity entity = _target.ResolvedEntity;
		Vector3 position = !isZone && entity != null && entity.Skin != null && entity.Skin.Center != null
			? entity.Skin.Center.position
			: _target.targetTile != null ? _target.targetTile.transform.position : m_user.transform.position;

		PlayOrientedVFX(_attackAction.Data.onSingleAttackHitVFXPool, position, Vector3.zero, _attackAction.Data.name);
	}

	protected bool PlayAttackAnimation ( AttackAction _attackAction, AttackAction.SingleAttackInfo _attackInfo )
	{
		return PlayAttackAnimation(_attackAction, _attackInfo, _attackAction.IsLastPerformTick, out _);
	}

	protected bool PlayAttackAnimation ( AttackAction _attackAction, AttackAction.SingleAttackInfo _attackInfo, bool _isFinalStrike, out float _impactDelay )
	{
		_impactDelay = 0f;
		if (m_user == null || m_user.Skin == null)
			return false;

		string weaponKey = _attackInfo.isAttackSuccessfull && !string.IsNullOrEmpty(m_data.attackAnimationSuccessId)
			? m_data.attackAnimationSuccessId : m_data.attackAnimationFailureId;
		bool isLeftHand = m_user.Skin.IK != null && m_user.Skin.IK.IsHeldInLeftHand(transform);
		return m_user.Skin.PlayAttackAnimation(_attackAction, weaponKey, isLeftHand, _isFinalStrike, out _impactDelay);
	}

	public void PlayShotRecoil ( EntityActionData _data )
	{
		if (_data == null || _data.shotRecoilStrength <= 0f || m_user == null || m_user.Skin == null || m_user.Skin.IK == null)
			return;

		m_user.Skin.IK.Recoil(_data.shotRecoilStrength, transform, m_data != null && m_data.isTwoHanded);
	}

	private static bool IsShot ( EntityActionData _data )
	{
		return _data.type == EntityActionData.ActionType.DistanceAttack;
	}

	private void PlayPerformFeedback ( AttackAction _attackAction, Vector3 _strikePoint )
	{
		EntityActionData data = _attackAction.Data;
		bool isSpray = data.attackProjectileMode == EntityActionData.AttackProjectileMode.Spray && data.attackProjectilePool != null && data.doesSprayRepeatPerformVFX;
		if (!isSpray && IsAttackVisible())
		{
			Vector3 origin = ProjectileOrigin;
			PlayOrientedVFX(data.onPerformVFXPool, origin, _strikePoint - origin, data.name);
		}
		if (IsShot(data) && !(data.attackProjectileMode == EntityActionData.AttackProjectileMode.Spray && data.attackProjectilePool != null))
			PlayShotRecoil(data);

		bool isStrikeOnTarget = _attackAction.Data.type == EntityActionData.ActionType.MeleeAttack;
		foreach (ParticleSystem ps in m_onPerformPS)
		{
			if (ps == null)
				continue;

			if (isStrikeOnTarget)
				PlacePerformPSAt(ps, _strikePoint);
			ps.Play();
		}

		SoundManager.Instance.Play(_attackAction.Data.onPerformSingleAttackSFXID);
	}

	private void PlacePerformPSAt ( ParticleSystem _ps, Vector3 _strikePoint )
	{
		Vector3 direction = _strikePoint - m_user.transform.position;
		direction.y = 0f;
		Quaternion facing = direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : m_user.transform.rotation;
		Quaternion relativeRotation = m_onPerformPSRelativeRotations.TryGetValue(_ps, out Quaternion rotation) ? rotation : Quaternion.identity;
		_ps.transform.SetPositionAndRotation(_strikePoint, facing * relativeRotation);
	}

	public Vector3 GetAimPoint ( AttackAction _attackAction, int _attackIndex )
	{
		return GetAimPoint(new WeaponTarget() { targetEntity = _attackAction.GetTargetEntityAt(_attackIndex), targetTile = _attackAction.GetTargetTileAt(_attackIndex) });
	}

	protected Vector3 GetAimPoint ( WeaponTarget _target )
	{
		Entity entity = _target.ResolvedEntity;
		if (entity != null && entity.Skin != null && entity.Skin.Center != null)
			return entity.Skin.Center.position;

		return _target.targetTile != null ? _target.targetTile.transform.position + Vector3.up * c_tileAimHeight : ProjectileOrigin + m_user.transform.forward;
	}

	private Vector3 GetStrikePoint ( AttackAction _attackAction, int _attackIndex, List<WeaponTarget> _targets )
	{
		if (_attackAction.Data.aoeType == EntityActionData.AOEType.Noone && _targets.Count > 0)
			return GetAimPoint(_targets[0]);

		Tile targetTile = _attackAction.GetTargetTileAt(_attackIndex);
		if (targetTile == null)
			return _targets.Count > 0 ? GetAimPoint(_targets[0]) : ProjectileOrigin + m_user.transform.forward;

		return GetAimPoint(new WeaponTarget() { targetTile = targetTile, targetEntity = targetTile.GetCurrentEntity() });
	}

	private Vector3 GetMissPoint ( AttackAction _attackAction, int _attackIndex, Entity _missedTarget )
	{
		Tile tile = _missedTarget != null ? _attackAction.GetExchangeTileOf(_missedTarget) : _attackAction.GetTargetTileAt(_attackIndex);
		Vector3 point = GetAimPoint(new WeaponTarget() { targetEntity = _missedTarget, targetTile = tile });

		Vector3 shot = point - ProjectileOrigin;
		shot.y = 0f;
		Vector3 side = shot.sqrMagnitude > .0001f ? Vector3.Cross(Vector3.up, shot.normalized) : m_user.transform.right;
		return point + side * (UnityEngine.Random.value < .5f ? -c_missOffset : c_missOffset);
	}

	protected bool IsAttackVisible ()
	{
		return m_user == null || GameManager.Instance == null || m_user.IsAlliedTo(GameManager.Instance.PlayerID) || m_user.IsVisible;
	}

	private IEnumerator PlayAttackProjectilesCR ( AttackAction _attackAction, List<WeaponTarget> _targets, Vector3 _mainPoint, Action<WeaponTarget> _onHit )
	{
		EntityActionData data = _attackAction.Data;
		if (data.attackProjectileMode == EntityActionData.AttackProjectileMode.None || data.attackProjectilePool == null)
			yield break;

		List<AttackFlight> flights = BuildAttackFlights(data, _targets, _mainPoint);
		float speed = Mathf.Max(1f, data.attackProjectileSpeed) / m_pace;
		float startTime = Time.time;
		bool isVisible = IsAttackVisible();

		while (Time.time - startTime <= c_attackProjectileTimeout)
		{
			bool isRunning = false;
			float now = Time.time;
			foreach (AttackFlight flight in flights)
			{
				if (flight.isDone)
					continue;

				isRunning = true;
				if (!flight.isLaunched)
				{
					if (now < flight.launchTime)
						continue;
					LaunchFlight(flight, data.attackProjectilePool, speed, now, isVisible);
					if (isVisible && data.attackProjectileMode == EntityActionData.AttackProjectileMode.Spray && data.doesSprayRepeatPerformVFX)
						PlayOrientedVFX(data.onPerformVFXPool, flight.waypoints[0], flight.waypoints[1] - flight.waypoints[0], data.name);
					if (IsShot(data) && data.attackProjectileMode == EntityActionData.AttackProjectileMode.Spray)
						PlayShotRecoil(data);
				}

				AdvanceFlight(flight, now, speed, _onHit);
			}

			if (!isRunning)
				yield break;

			yield return null;
		}

		foreach (AttackFlight flight in flights)
		{
			if (flight.isDone)
				continue;

			for (int i = flight.nextIndex; i < flight.hits.Count; i++)
				NotifyHits(_onHit, flight.hits[i]);
			ReleaseFlight(flight);
		}
	}

	private List<AttackFlight> BuildAttackFlights ( EntityActionData _data, List<WeaponTarget> _targets, Vector3 _mainPoint )
	{
		List<AttackFlight> flights = new();
		Vector3 origin = ProjectileOrigin;
		bool doesReturn = _data.doesAttackProjectileReturn;
		float now = Time.time;

		if (_data.attackProjectileMode == EntityActionData.AttackProjectileMode.Spray && _targets.Count > 0)
		{
			List<WeaponTarget> tiles = SortFromLeftToRight(_targets, origin, _mainPoint);
			int count = Mathf.Max(_data.attackProjectileSprayCount, tiles.Count);
			for (int i = 0; i < count; i++)
			{
				WeaponTarget target = tiles[Mathf.Min(tiles.Count - 1, i * tiles.Count / count)];
				Vector3 jitter = new(UnityEngine.Random.Range(-c_sprayJitter, c_sprayJitter), UnityEngine.Random.Range(-c_sprayJitter, c_sprayJitter) * .5f, UnityEngine.Random.Range(-c_sprayJitter, c_sprayJitter));
				AttackFlight flight = new() { launchTime = now + i * Paced(Mathf.Max(0f, _data.attackProjectileSprayInterval)), doesReturn = doesReturn };
				flight.Add(origin);
				flight.Add(GetAimPoint(target) + jitter, target);
				if (doesReturn)
					flight.Add(origin);
				flights.Add(flight);
			}
			return flights;
		}

		AttackFlight single = new() { launchTime = now, doesReturn = doesReturn };
		single.Add(origin);

		if (_data.attackProjectileMode == EntityActionData.AttackProjectileMode.Chain && _targets.Count > 0)
		{
			foreach (WeaponTarget hop in OrderChainHops(_targets, _mainPoint))
				single.Add(GetAimPoint(hop), hop);
		}
		else
			single.Add(_mainPoint, _targets.ToArray());

		if (doesReturn)
			single.Add(origin);

		flights.Add(single);
		return flights;
	}

	private List<WeaponTarget> SortFromLeftToRight ( List<WeaponTarget> _targets, Vector3 _origin, Vector3 _mainPoint )
	{
		Vector3 forward = _mainPoint - _origin;
		forward.y = 0f;
		if (forward.sqrMagnitude < .0001f)
			forward = m_user.transform.forward;

		List<WeaponTarget> sorted = new(_targets);
		sorted.Sort(( a, b ) => SignedAngleFrom(forward, _origin, a).CompareTo(SignedAngleFrom(forward, _origin, b)));
		return sorted;
	}

	private float SignedAngleFrom ( Vector3 _forward, Vector3 _origin, WeaponTarget _target )
	{
		Vector3 direction = GetAimPoint(_target) - _origin;
		direction.y = 0f;
		return Vector3.SignedAngle(_forward, direction, Vector3.up);
	}

	private List<WeaponTarget> OrderChainHops ( List<WeaponTarget> _targets, Vector3 _start )
	{
		List<WeaponTarget> remaining = new();
		foreach (WeaponTarget target in _targets)
		{
			if (target.ResolvedEntity != null)
				remaining.Add(target);
		}

		if (remaining.Count == 0)
			remaining.AddRange(_targets);

		List<WeaponTarget> ordered = new();
		Vector3 current = _start;
		while (remaining.Count > 0)
		{
			WeaponTarget closest = remaining.OrderBy(target => Vector3.SqrMagnitude(GetAimPoint(target) - current)).First();
			ordered.Add(closest);
			remaining.Remove(closest);
			current = GetAimPoint(closest);
		}

		return ordered;
	}

	private void LaunchFlight ( AttackFlight _flight, PoolData _pool, float _speed, float _now, bool _isVisible )
	{
		_flight.isLaunched = true;
		_flight.legStart = _flight.waypoints[0];
		_flight.legStartTime = _now;
		_flight.legDuration = Vector3.Distance(_flight.waypoints[0], _flight.waypoints[1]) / _speed;

		try
		{
			Vector3 direction = _flight.waypoints[1] - _flight.waypoints[0];
			Quaternion rotation = direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
			PoolElement element = _pool.Get(_flight.waypoints[0], rotation);
			_flight.projectile = element as VisualProjectile;

			if (_flight.projectile != null)
				_flight.projectile.SetVisible(_isVisible);
			else if (element != null)
			{
				Debug.LogError("Attack projectile pool " + _pool.name + " does not hold a VisualProjectile.");
				element.Discard();
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Attack projectile " + _pool.name + " failed: " + ex.Message);
			_flight.projectile = null;
		}
	}

	private void AdvanceFlight ( AttackFlight _flight, float _now, float _speed, Action<WeaponTarget> _onHit )
	{
		while (!_flight.isDone)
		{
			Vector3 destination = _flight.waypoints[_flight.nextIndex];
			float progress = _flight.legDuration <= 0f ? 1f : (_now - _flight.legStartTime) / _flight.legDuration;
			bool isReturning = _flight.doesReturn && _flight.nextIndex == _flight.waypoints.Count - 1;
			Vector3 facing = isReturning ? Vector3.zero : destination - _flight.legStart;

			if (progress < 1f)
			{
				PlaceProjectile(_flight, Vector3.Lerp(_flight.legStart, destination, progress), facing);
				return;
			}

			PlaceProjectile(_flight, destination, facing);
			NotifyHits(_onHit, _flight.hits[_flight.nextIndex]);

			float legEnd = _flight.legStartTime + _flight.legDuration;
			_flight.nextIndex++;
			if (_flight.nextIndex >= _flight.waypoints.Count)
			{
				ReleaseFlight(_flight);
				return;
			}

			_flight.legStart = destination;
			_flight.legStartTime = legEnd;
			_flight.legDuration = Vector3.Distance(destination, _flight.waypoints[_flight.nextIndex]) / _speed;
		}
	}

	private static void PlaceProjectile ( AttackFlight _flight, Vector3 _position, Vector3 _direction )
	{
		if (_flight.projectile == null)
			return;

		_flight.projectile.Place(_position, _direction);
	}

	private static void NotifyHits ( Action<WeaponTarget> _onHit, WeaponTarget[] _targets )
	{
		if (_onHit == null || _targets == null)
			return;

		foreach (WeaponTarget target in _targets)
		{
			try
			{
				_onHit(target);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}
	}

	private static void ReleaseFlight ( AttackFlight _flight )
	{
		_flight.isDone = true;
		if (_flight.projectile == null)
			return;

		try
		{
			_flight.projectile.Release();
		}
		catch (Exception ex)
		{
			Debug.LogError("Attack projectile release failed: " + ex.Message);
		}

		_flight.projectile = null;
	}

	private void DamageWallsInWeaponRange ( AttackAction _attackAction, Dictionary<WeaponEquipmentData.DamageType, int> _damages )
	{
		int hitAmount = _attackAction.Data.GetHitAmount(_attackAction, m_user, null);

		foreach (Tile tile in m_user.Equipment.GetTilesInWeaponRange(_attackAction, true, true))
		{
			if (tile == null || tile.Wall == null)
				continue;

			for (int i = 0; i < hitAmount && tile.Wall.Health > 0; i++)
				tile.Wall.TakeDamage(_damages);
		}
	}

	protected List<WeaponTarget> GetTargets ( AttackAction _attackAction, int _attackIndex )
	{
		List<WeaponTarget> targets = new();

		if (_attackAction.Data.aoeType != EntityActionData.AOEType.Noone)
		{
			Tile aoeCenter = _attackAction.GetTargetTileAt(_attackIndex);
			if (aoeCenter == null)
				return targets;

			foreach (Tile tile in m_user.Equipment.GetTilesInAoERange(_attackAction, aoeCenter, _entityOnTile: _attackAction.GetEntityCaughtOn))
			{
				m_targetedTiles.Add(tile);
				tile.UI.SetOutlineColor(Color.red);
				targets.Add(new() { targetTile = tile, targetEntity = _attackAction.GetEntityCaughtOn(tile) });
			}
		}
		else
		{
			Entity recordedTarget = _attackAction.GetTargetEntityAt(_attackIndex);
			Tile targetTile = recordedTarget != null
				? _attackAction.GetExchangeTileOf(recordedTarget)
				: _attackAction.GetTargetTileAt(_attackIndex);

			if (targetTile == null)
				return targets;

			targetTile.UI.SetOutlineColor(Color.red);
			m_targetedTiles.Add(targetTile);

			targets.Add(new() { targetTile = targetTile, targetEntity = recordedTarget ?? targetTile.GetCurrentEntity() });
		}

		return targets;
	}

	protected virtual List<Entity> GetPassiveEffectTargets ( AttackAction _attackAction, AEntityPassiveEffect.PassiveEffectContainer _passiveEffect, int _attackIndex )
	{
		List<Entity> targets = new();

		if (_passiveEffect.aoeType == EntityActionData.AOEType.Noone)
		{
			Entity singleTarget = _attackAction.GetTargetEntityAt(_attackIndex);
			if (singleTarget != null)
				targets.Add(singleTarget);
		}
		else
		{
			Tile target = _attackAction.GetTargetTileAt(_attackIndex);
			if (target == null)
				return targets;

			Tile from = _passiveEffect.centerType == EntityActionData.AOECenterType.Self
				? m_user.Displacement.Coordinates.GetTile() : target;
			int minDistance = _passiveEffect.effectRange.x != -1 ? _passiveEffect.effectRange.x : _attackAction.Data.aoeMinEffectRange;
			int maxDistance = _passiveEffect.effectRange.y != -1 ? _passiveEffect.effectRange.y : _attackAction.Data.aoeMaxEffectRange;
			foreach (Tile tile in GridManager.Instance.GetTilesInAoERange(_passiveEffect.aoeType, m_user, from, target, minDistance, maxDistance, _attackAction.Data.maxChainedTarget, true))
			{
				if (tile.TryGetCurrentEntity(out Entity entity))
					targets.Add(entity);
			}
		}

		return targets;
	}

	protected virtual Dictionary<WeaponEquipmentData.DamageType, int> BuildDamageDictionary ( AttackAction.SingleAttackInfo _attackInfo )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();

		if (_attackInfo != null && _attackInfo.damageTypes != null)
		{
			for (int i = 0; i < _attackInfo.damageTypes.Length; i++)
				damages.Add((WeaponEquipmentData.DamageType)_attackInfo.damageTypes[i], _attackInfo.damages[i]);
		}

		return damages;
	}

	protected virtual void ApplyStatuses ( Entity _target, AttackAction.SingleAttackInfo _attackInfo )
	{
		if (_attackInfo != null && _attackInfo.areStatusesSuccess != null)
		{
			for (int i = 0; i < _attackInfo.areStatusesSuccess.Length; i++)
			{
				if (_attackInfo.areStatusesSuccess[i])
					_target.AddStatus((EntityStatusEnumID)_attackInfo.statusIds[i], _attackInfo.statusDurations[i], m_user);
			}
		}
	}

	protected virtual void ApplyEffects ( Entity _target, AEntityPassiveEffect.PassiveEffectContainer _passiveEffect )
	{
		GameAssets.current.game.entityEffects[_passiveEffect.enumID].ApplyEffect(m_user, _target, _passiveEffect);
	}

	#endregion

	protected void ShowAoEDodges ( AttackAction _attackAction, List<WeaponTarget> _targets )
	{
		if (_attackAction.Data.aoeType == EntityActionData.AOEType.Noone)
			return;

		List<Tile> zone = new();
		foreach (WeaponTarget target in _targets)
			zone.Add(target.targetTile);

		ShowAoEDodges(_attackAction, zone);
	}

	protected void ShowAoEDodges ( AttackAction _attackAction, List<Tile> _zone )
	{
		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				if (entity == m_user || entity.Equipment.IsDead)
					continue;

				if (_zone.Contains(_attackAction.GetDepartureTileOf(entity)) && !_zone.Contains(_attackAction.GetExchangeTileOf(entity)))
					ShowMissOn(entity, true);
			}
		}
	}

	protected void ShowMissOn ( Entity _target, bool _didTargetDodge = false )
	{
		if (_target == null || _target.Equipment.IsDead)
			return;

		if (_didTargetDodge)
			_target.UI.ShowDodgeText();
		else
			_target.UI.ShowMissText();
	}

	protected virtual void EndAttack ( AttackAction _attackAction )
	{
		m_onPerformAttackEnd?.Invoke();
		foreach (Tile tile in m_targetedTiles)
			tile.UI.ResetOutline();
		m_targetedTiles.Clear();
	}

	public virtual Dictionary<WeaponEquipmentData.DamageType, int> GetDamages ( Entity _user, Entity _target, AEntityAction _action, EntityActionData.PFCResultType _pfcResultType, out LogCard _card, Tile _shotOrigin = null )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
		bool didWinPFC = _pfcResultType == EntityActionData.PFCResultType.FirstWins;
		bool hasTarget = _target != null;
		Tile.TileDirectionType side = hasTarget ? GridManager.Instance.GetHitTileSide(_user, _target, didWinPFC, _shotOrigin) : Tile.TileDirectionType.Front;

		LocalizationManager localization = LocalizationManager.Instance;
		LogCard card = new(LogCard.Kind.Damage, localization.Get(LocalizationKey.log_card_damage))
		{
			actor = _user.Data.name,
			target = hasTarget ? _target.Data.name : localization.Get(LocalizationKey.log_use_weapon_ground)
		};
		string flankLabel = string.Format(localization.Get(LocalizationKey.log_card_step_flank), side.GetLocalizedTitle());

		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in _action.Data.baseDamages)
		{
			int baseDamage = pair.Value;

			float actionFactor = _action.Data.GetDamageFactorAmountForType(_action, _user, _target, pair.Key);
			DamageModifiers modifiers = new(_user, _target, _action, pair.Key, side);

			LogCard.DamageLine line = card.AddDamage(pair.Key, baseDamage);

			float damage = baseDamage * actionFactor;
			line.AddFactor(actionFactor, damage);

			damage *= 1 + modifiers.type;
			line.AddPercent(LocalizationKey.log_card_step_type, modifiers.type, damage);

			damage *= 1 + modifiers.category;
			line.AddPercent(LocalizationKey.log_card_step_category, modifiers.category, damage);

			damage *= 1 + modifiers.general;
			line.AddPercent(LocalizationKey.log_card_step_general, modifiers.general, damage);

			damage *= 1 + modifiers.flank;
			line.AddPercent(flankLabel, modifiers.flank, damage);

			damage *= 1 + modifiers.final;
			line.AddPercent(LocalizationKey.log_card_step_final, modifiers.final, damage);

			int finalDamage = Mathf.RoundToInt(damage);
			line.final = finalDamage;

			damages.Add(pair.Key, finalDamage);
		}

		card.SetDamageVerdict(damages);
		_card = card;

		return damages;
	}

}
