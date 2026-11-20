using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AnimationLabFeedback
{
	public class WeaponVisual
	{
		public Transform root;
		public Transform origin;
		public readonly List<(ParticleSystem system, Quaternion relative)> performSystems = new();
		public bool isBulletWeapon;
		public PoolData bulletPool;
		public float bulletSpeed = 20f;
		public float bulletInterval = .3f;
		public float aimDuration = 1f;
		public float shootCooldown = .3f;

		public Transform LaunchPoint => origin != null ? origin : root.parent != null ? root.parent : root;
	}

	private const float c_effectLifetime = 3f;
	private const float c_projectileLifetime = 12f;
	private const float c_projectileLinger = 1.2f;
	private const float c_tileAimHeight = 1f;
	private const float c_sprayAngle = 30f;
	private const float c_sprayJitter = .25f;
	private const int c_minSprayCount = 5;

	private readonly AnimationLabRig m_rig;
	private readonly List<GameObject> m_spawned = new();
	private readonly List<Tween> m_tweens = new();
	private Dictionary<string, SfxData> m_sounds;
	private AudioSource m_audio;

	public bool IsEnabled { get; set; } = true;

	public AnimationLabFeedback ( AnimationLabRig _rig )
	{
		m_rig = _rig;
	}

	public static WeaponVisual Describe ( GameObject _prefab, GameObject _instance )
	{
		if (_prefab == null || _instance == null)
			return null;

		WeaponVisual visual = new() { root = _instance.transform };
		Weapon weapon = _prefab.GetComponentInChildren<Weapon>(true);
		if (weapon == null)
			return visual;

		SerializedObject serialized = new(weapon);
		SerializedProperty systems = serialized.FindProperty("m_onPerformPS");
		if (systems != null && systems.isArray)
		{
			for (int i = 0; i < systems.arraySize; i++)
			{
				if (systems.GetArrayElementAtIndex(i).objectReferenceValue is not ParticleSystem source)
					continue;

				Transform match = Match(_prefab.transform, _instance.transform, source.transform);
				ParticleSystem system = match != null ? match.GetComponent<ParticleSystem>() : null;
				if (system == null)
					continue;

				system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				visual.performSystems.Add((system, Quaternion.Inverse(_prefab.transform.rotation) * source.transform.rotation));
			}
		}

		if (weapon is not BulletWeapon)
			return visual;

		visual.isBulletWeapon = true;
		visual.origin = serialized.FindProperty("m_bulletPoint").objectReferenceValue is Transform point ? Match(_prefab.transform, _instance.transform, point) : null;
		visual.bulletPool = serialized.FindProperty("m_bulletPool").objectReferenceValue as PoolData;
		visual.bulletSpeed = Positive(serialized.FindProperty("m_speed").floatValue, 20f);
		visual.bulletInterval = serialized.FindProperty("m_timeBetweenEachBullet").floatValue;
		visual.aimDuration = serialized.FindProperty("m_aimDuration").floatValue;
		visual.shootCooldown = serialized.FindProperty("m_shootCooldownDuration").floatValue;
		return visual;
	}

	public static bool IsSingleTargetShot ( EntityActionData _action )
	{
		return _action.aoeType == EntityActionData.AOEType.Noone
			|| _action.aoeType == EntityActionData.AOEType.Circle && _action.targetType != EntityActionData.TargetType.Self;
	}

	public Vector3 TargetPoint ( EntityActionData _action )
	{
		Transform visual = m_rig.Visual;
		Vector3 forward = visual.forward;
		forward.y = 0f;
		forward = forward.sqrMagnitude > .0001f ? forward.normalized : Vector3.forward;
		float tiles = _action == null ? 1f
			: _action.type == EntityActionData.ActionType.MeleeAttack || _action.maxDistance <= 1 ? Mathf.Max(1, _action.maxDistance)
			: Mathf.Clamp((_action.minDistance + _action.maxDistance) * .5f, 1f, 5f);
		Vector3 point = visual.position + forward * tiles * 2f * Tile.innerRadius;
		bool isTile = _action != null && (_action.targetType == EntityActionData.TargetType.Tile || _action.aoeType != EntityActionData.AOEType.Noone);
		point.y = visual.position.y + (isTile ? c_tileAimHeight : m_rig.CenterHeight);
		return point;
	}

	public void PlayLater ( float _delay, EntityActionData _action, WeaponVisual _weapon )
	{
		if (IsEnabled)
			Delay(Mathf.Max(0f, _delay), () => PlayFor(_action, _weapon));
	}

	public void PlayFor ( EntityActionData _action, WeaponVisual _weapon )
	{
		if (_action == null)
			return;

		if (_action.type == EntityActionData.ActionType.Special || _action.type == EntityActionData.ActionType.Movement)
			PlaySpecial(_action, _weapon);
		else
			PlayStrike(_action, _weapon);
	}

	public void PlayStrike ( EntityActionData _action, WeaponVisual _weapon )
	{
		if (!IsEnabled || _action == null)
			return;

		Vector3 strike = TargetPoint(_action);
		Vector3 origin = OriginOf(_weapon);
		bool isSpray = _action.attackProjectileMode == EntityActionData.AttackProjectileMode.Spray && _action.attackProjectilePool != null;
		if (!(isSpray && _action.doesSprayRepeatPerformVFX))
			SpawnPool(_action.onPerformVFXPool, origin, strike - origin, true);
		if (_action.type == EntityActionData.ActionType.DistanceAttack && !isSpray)
			Recoil(_action, _weapon);
		PlayPerformSystems(_weapon, _action.type == EntityActionData.ActionType.MeleeAttack, strike);
		PlaySound(_action.onPerformSingleAttackSFXID);

		if (_action.attackProjectileMode == EntityActionData.AttackProjectileMode.None || _action.attackProjectilePool == null)
		{
			Hit(_action, strike);
			return;
		}

		float speed = Mathf.Max(1f, _action.attackProjectileSpeed);
		Vector3 side = Vector3.Cross(Vector3.up, Flat(strike - origin)).normalized * 2f * Tile.innerRadius;
		switch (_action.attackProjectileMode)
		{
			case EntityActionData.AttackProjectileMode.Spray:
				int count = Mathf.Max(c_minSprayCount, _action.attackProjectileSprayCount);
				for (int i = 0; i < count; i++)
				{
					float angle = Mathf.Lerp(-c_sprayAngle, c_sprayAngle, count == 1 ? .5f : i / (float)(count - 1));
					Vector3 jitter = new(UnityEngine.Random.Range(-c_sprayJitter, c_sprayJitter), UnityEngine.Random.Range(-c_sprayJitter, c_sprayJitter) * .5f, UnityEngine.Random.Range(-c_sprayJitter, c_sprayJitter));
					Vector3 point = origin + Quaternion.AngleAxis(angle, Vector3.up) * (strike - origin) + jitter;
					Delay(i * Mathf.Max(0f, _action.attackProjectileSprayInterval), () =>
					{
						if (_action.doesSprayRepeatPerformVFX)
							SpawnPool(_action.onPerformVFXPool, OriginOf(_weapon), point - OriginOf(_weapon), true);
						if (_action.type == EntityActionData.ActionType.DistanceAttack)
							Recoil(_action, _weapon);
						Fly(_action.attackProjectilePool, OriginOf(_weapon), Route(_action, OriginOf(_weapon), point), 1, speed, hit => Hit(_action, hit));
					});
				}
				break;

			case EntityActionData.AttackProjectileMode.Chain:
				List<Vector3> hops = new() { strike, strike + side + Flat(strike - origin).normalized * Tile.innerRadius, strike - side };
				int hits = hops.Count;
				if (_action.doesAttackProjectileReturn)
					hops.Add(origin);
				Fly(_action.attackProjectilePool, origin, hops, hits, speed, hit => Hit(_action, hit));
				break;

			default:
				Fly(_action.attackProjectilePool, origin, Route(_action, origin, strike), 1, speed, hit => Hit(_action, hit));
				break;
		}
	}

	public void PlayShot ( EntityActionData _action, WeaponVisual _weapon )
	{
		if (!IsEnabled || _action == null)
			return;

		Vector3 target = TargetPoint(_action);
		Vector3 origin = OriginOf(_weapon);
		PlayPerformSystems(_weapon, false, target);
		PlaySound(_action.onPerformSingleAttackSFXID);
		SpawnPool(_action.onPerformVFXPool, origin, _weapon != null && _weapon.root != null ? LaunchPoseOf(_weapon).forward : target - origin, true);
		Recoil(_action, _weapon);
		PoolData pool = _weapon != null ? _weapon.bulletPool : null;
		Fly(pool, origin, new List<Vector3> { target }, 1, _weapon != null ? _weapon.bulletSpeed : 20f, hit => Hit(_action, hit));
	}

	public void PlaySpecial ( EntityActionData _action, WeaponVisual _weapon )
	{
		if (!IsEnabled || _action == null)
			return;

		Vector3 feet = m_rig.Visual.position;
		Vector3 target = _action.targetType == EntityActionData.TargetType.Self ? feet : TargetPoint(_action);
		Vector3 ground = new(target.x, feet.y, target.z);
		PlaySound(_action.onPerformSingleAttackSFXID);
		if (_action.codeType == EntityActionData.ActionCodeType.InvokeItem)
		{
			SpawnPool(_action.onPerformVFXPool, ground, Vector3.zero, false);
			return;
		}

		switch (_action.visualEffectType)
		{
			case EntityActionData.VisualEffectType.Spell when _action.spellVfxPrefab != null:
				Spawn(_action.spellVfxPrefab, _action.targetType == EntityActionData.TargetType.Self ? feet : ground, _action.spellVfxPrefab.transform.rotation, Mathf.Max(.5f, _action.spellVfxLifetime));
				break;

			case EntityActionData.VisualEffectType.Projectile when _action.projectilePool != null:
				Vector3 origin = OriginOf(_weapon);
				SpawnPool(_action.onPerformVFXPool, origin, target - origin, true);
				Recoil(_action, _weapon);
				Fly(_action.projectilePool, origin, new List<Vector3> { target }, 1, Mathf.Max(1f, _action.projectileSpeed), hit => Hit(_action, hit));
				break;

			default:
				SpawnPool(_action.onPerformVFXPool, _action.targetType == EntityActionData.TargetType.Self ? feet : ground, Vector3.zero, false);
				break;
		}
	}

	public void Clear ()
	{
		foreach (Tween tween in m_tweens)
		{
			if (tween != null && tween.IsActive())
				tween.Kill();
		}

		m_tweens.Clear();
		foreach (GameObject spawned in m_spawned)
		{
			if (spawned != null)
				Object.Destroy(spawned);
		}

		m_spawned.Clear();
		m_audio = null;
	}

	private Vector3 OriginOf ( WeaponVisual _weapon )
	{
		return _weapon != null && _weapon.root != null ? LaunchPoseOf(_weapon).position : m_rig.Visual.position + Vector3.up * m_rig.CenterHeight;
	}

	private Pose LaunchPoseOf ( WeaponVisual _weapon )
	{
		return m_rig.IK.RenderedPoseOf(_weapon.LaunchPoint);
	}

	private static List<Vector3> Route ( EntityActionData _action, Vector3 _origin, Vector3 _point )
	{
		List<Vector3> route = new() { _point };
		if (_action.doesAttackProjectileReturn)
			route.Add(_origin);
		return route;
	}

	private void PlayPerformSystems ( WeaponVisual _weapon, bool _isStrikeOnTarget, Vector3 _strike )
	{
		if (_weapon == null)
			return;

		foreach ((ParticleSystem system, Quaternion relative) in _weapon.performSystems)
		{
			if (system == null)
				continue;

			if (_isStrikeOnTarget)
			{
				Vector3 direction = Flat(_strike - m_rig.Visual.position);
				Quaternion facing = direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : m_rig.Visual.rotation;
				system.transform.SetPositionAndRotation(_strike, facing * relative);
			}

			system.Play();
		}
	}

	private void Recoil ( EntityActionData _action, WeaponVisual _weapon )
	{
		if (_action.shotRecoilStrength > 0f && _weapon != null && _weapon.root != null && m_rig.IK != null)
			m_rig.IK.Recoil(_action.shotRecoilStrength, _weapon.root, false);
	}

	private void Hit ( EntityActionData _action, Vector3 _point )
	{
		SpawnPool(_action.onSingleAttackHitVFXPool, _point, Vector3.zero, false);
		PlaySound(_action.onSingleAttackHitSFXID);
	}

	private void Fly ( PoolData _pool, Vector3 _from, List<Vector3> _waypoints, int _hitCount, float _speed, Action<Vector3> _onHit )
	{
		if (_waypoints.Count == 0)
			return;

		GameObject projectile = Spawn(Prefab(_pool), _from, Look(_waypoints[0] - _from), c_projectileLifetime);
		if (projectile == null)
		{
			for (int i = 0; i < Mathf.Min(_hitCount, _waypoints.Count); i++)
				_onHit?.Invoke(_waypoints[i]);
			return;
		}

		Sequence sequence = DOTween.Sequence();
		Vector3 previous = _from;
		for (int i = 0; i < _waypoints.Count; i++)
		{
			Vector3 start = previous;
			Vector3 point = _waypoints[i];
			bool isHit = i < _hitCount;
			if (isHit)
				sequence.AppendCallback(() =>
				{
					if (projectile != null && (point - start).sqrMagnitude > .000001f)
						projectile.transform.rotation = Quaternion.LookRotation(point - start);
				});
			sequence.Append(projectile.transform.DOMove(point, Mathf.Max(.01f, Vector3.Distance(start, point) / Mathf.Max(1f, _speed))).SetEase(Ease.Linear));
			if (isHit)
				sequence.AppendCallback(() => _onHit?.Invoke(point));
			previous = point;
		}

		sequence.AppendCallback(() => Linger(projectile));
		Track(sequence);
	}

	private static void Linger ( GameObject _projectile )
	{
		if (_projectile == null)
			return;

		foreach (ParticleSystem system in _projectile.GetComponentsInChildren<ParticleSystem>())
			system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		foreach (MeshRenderer renderer in _projectile.GetComponentsInChildren<MeshRenderer>())
			renderer.enabled = false;
		Object.Destroy(_projectile, c_projectileLinger);
	}

	private void Delay ( float _seconds, Action _action )
	{
		if (_seconds <= 0f)
		{
			_action();
			return;
		}

		Track(DOVirtual.DelayedCall(_seconds, () => _action()));
	}

	private void Track ( Tween _tween )
	{
		m_tweens.RemoveAll(tween => tween == null || !tween.IsActive());
		m_tweens.Add(_tween);
	}

	private void SpawnPool ( PoolData _pool, Vector3 _position, Vector3 _direction, bool _isOriented )
	{
		GameObject prefab = Prefab(_pool);
		if (prefab != null)
			Spawn(prefab, _position, _isOriented && _direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(_direction) : Quaternion.identity, c_effectLifetime);
	}

	private GameObject Spawn ( GameObject _prefab, Vector3 _position, Quaternion _rotation, float _lifetime )
	{
		if (!IsEnabled || _prefab == null)
			return null;

		GameObject holder = new("Animation Lab FX Holder");
		holder.SetActive(false);
		try
		{
			GameObject instance = Object.Instantiate(_prefab, holder.transform);
			instance.name = _prefab.name + " (Lab)";
			AnimationLabRig.StripToVisual(instance);
			instance.transform.SetPositionAndRotation(_position, _rotation);
			instance.transform.SetParent(null, true);
			foreach (ParticleSystem system in instance.GetComponentsInChildren<ParticleSystem>())
			{
				if (!system.isPlaying)
					system.Play(false);
			}

			m_spawned.RemoveAll(spawned => spawned == null);
			m_spawned.Add(instance);
			Object.Destroy(instance, _lifetime);
			return instance;
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[Animation Lab] " + _prefab.name + " : " + exception.Message);
			return null;
		}
		finally
		{
			Object.Destroy(holder);
		}
	}

	private void PlaySound ( SfxId _id )
	{
		if (!IsEnabled || _id == SfxId.None)
			return;

		m_sounds ??= LoadSounds();
		if (!m_sounds.TryGetValue(_id.ToString(), out SfxData sound) || sound.Clip == null)
			return;

		if (m_audio == null)
		{
			GameObject speaker = new("Animation Lab Audio");
			m_audio = speaker.AddComponent<AudioSource>();
			m_audio.spatialBlend = 0f;
			m_audio.playOnAwake = false;
			m_spawned.Add(speaker);
		}

		m_audio.pitch = sound.Pitch;
		m_audio.PlayOneShot(sound.Clip, sound.Volume);
	}

	private static Dictionary<string, SfxData> LoadSounds ()
	{
		Dictionary<string, SfxData> sounds = new();
		foreach (string guid in AssetDatabase.FindAssets("t:SfxDatabase"))
		{
			SfxDatabase database = AssetDatabase.LoadAssetAtPath<SfxDatabase>(AssetDatabase.GUIDToAssetPath(guid));
			if (database == null)
				continue;

			foreach (SfxData sound in database.Sounds)
			{
				if (sound != null && !string.IsNullOrEmpty(sound.Id))
					sounds[sound.Id] = sound;
			}
		}

		return sounds;
	}

	private static GameObject Prefab ( PoolData _pool )
	{
		return _pool != null && _pool.baseElement != null ? _pool.baseElement.gameObject : null;
	}

	private static Transform Match ( Transform _prefabRoot, Transform _instanceRoot, Transform _source )
	{
		if (_source == null)
			return null;

		string path = AnimationUtility.CalculateTransformPath(_source, _prefabRoot);
		return string.IsNullOrEmpty(path) ? _instanceRoot : _instanceRoot.Find(path);
	}

	private static Vector3 Flat ( Vector3 _vector )
	{
		_vector.y = 0f;
		return _vector;
	}

	private static Quaternion Look ( Vector3 _direction )
	{
		return _direction.sqrMagnitude > .000001f ? Quaternion.LookRotation(_direction) : Quaternion.identity;
	}

	private static float Positive ( float _value, float _fallback )
	{
		return _value > 0f ? _value : _fallback;
	}
}
