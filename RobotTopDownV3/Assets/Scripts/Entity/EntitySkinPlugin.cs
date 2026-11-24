using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EntitySkinPlugin : EntityPlugin
{
	public const int OverFogSortingOrder = 10;

	[SerializeField] private GameObject m_visualRoot;
	[SerializeField] private GameObject m_radarRoot;
	[SerializeField] private GameObject m_thermicRoot;
	[SerializeField] private Material m_thermicMaterial;
	[SerializeField] private Transform m_center;
	public Transform Center => m_center;
	public Transform StatusVisualParent => m_visualRoot != null ? m_visualRoot.transform : transform;

	[SerializeField] private Animator m_animator;
	[SerializeField] private HumanoidEntityIK m_humanoidEntityIK;
	public HumanoidEntityIK IK => m_humanoidEntityIK;

	[SerializeField] private SerializableDictionary<EntityActionData.ActionType, string> m_animationKeyPerActionDictionary;
	[SerializeField] private EntityAnimationProfile m_animationProfile;
	public EntityAnimationProfile AnimationProfile => m_animationProfile;

	private AttackAnimationPlayer m_attackPlayer;
	public AttackAnimationPlayer AttackPlayer => m_attackPlayer ??= new AttackAnimationPlayer(m_animator, TuningFor);
	private GuardAnimationPlayer m_guardPlayer;
	private GuardAnimationPlayer GuardPlayer => m_guardPlayer ??= new GuardAnimationPlayer(m_animator);
	private LocomotionAnimationPlayer m_locomotionPlayer;
	private LocomotionAnimationPlayer LocomotionPlayer => m_locomotionPlayer ??= new LocomotionAnimationPlayer(m_animator);
	private StandbyAnimationPlayer m_standbyPlayer;
	private StandbyAnimationPlayer StandbyPlayer => m_standbyPlayer ??= new StandbyAnimationPlayer(m_animator);

	private static readonly HumanBodyBones[] s_poseBones =
	{
		HumanBodyBones.Hips, HumanBodyBones.Neck, HumanBodyBones.Head,
		HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
		HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
		HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes
	};

	private bool m_isGuardHeld;
	private bool m_isAiming;
	private bool m_isMoving;
	private string m_moveStyle;
	private AEntityAction m_performedAction;
	private Weapon m_guardWeapon;

	private bool m_isThermicLookWanted;
	private Material m_forcedLook;
	private Material m_appliedLook;
	private readonly List<Renderer> m_lookRenderers = new();
	public IReadOnlyList<Renderer> LookRenderers => m_lookRenderers;
	private readonly List<Material[]> m_lookOriginalMaterials = new();
	private readonly List<int> m_lookOriginalSortingOrders = new();
	private readonly List<ShadowCastingMode> m_lookOriginalShadowModes = new();

	private bool IsFlying => m_linkedEntity != null && m_linkedEntity.Status.Contains(EntityStatusEnumID.Flying);

	public override void Init ( EntitySavedData _entityData )
	{
		base.Init(_entityData);

		m_linkedEntity.onStartPerformAction += OnStartActionPerform;
		m_linkedEntity.onEndPerformAction += OnEndActionPerform;
		m_linkedEntity.Equipment.onDeath += OnEntityDeath;
		TurnManager.onStartInputPhase += OnStartInputPhase;
		if (m_humanoidEntityIK != null)
			m_humanoidEntityIK.GripAmount = m_animationProfile != null ? m_animationProfile.gripAmount : 1f;

		MoveSkinToEntityLayer();
	}

	private void MoveSkinToEntityLayer ()
	{
		int entityLayer = LayerMask.NameToLayer("Entity");
		List<Renderer> renderers = new();
		CollectSkinRenderers(renderers);
		foreach (Renderer renderer in renderers)
		{
			if (!renderer.TryGetComponent(out Collider _))
				renderer.gameObject.layer = entityLayer;
		}
	}

	private void Update ()
	{
		if (m_animator == null || m_linkedEntity == null || m_linkedEntity.Equipment == null)
			return;

		Weapon weapon = m_isMoving || m_isGuardHeld ? null : GuardWeapon();
		bool isLeftHand = weapon != null && m_humanoidEntityIK != null && m_humanoidEntityIK.IsHeldInLeftHand(weapon.transform);
		GuardPlayer.Set(weapon != null ? weapon.Data.guardAnimation : null, isLeftHand, weapon != null ? weapon.Data.guardTorsoYaw : 0f);
		GuardPlayer.Update(Time.deltaTime);
		LocomotionPlayer.Set(m_isMoving, m_moveStyle, IsFlying);
		LocomotionPlayer.Update();
		StandbyPlayer.Update();
	}

	public void SetPresentation ( bool _isGuardHeld, float _standby )
	{
		m_isGuardHeld = _isGuardHeld;
		StandbyPlayer.Weight = Mathf.Clamp01(_standby);
		StandbyPlayer.Update();
	}

	public Bounds GetPoseBounds ( float _margin )
	{
		Vector3 origin = m_linkedEntity != null ? m_linkedEntity.transform.position : transform.position;
		Bounds bounds = new(origin, Vector3.zero);
		bool isHuman = m_animator != null && m_animator.isHuman && m_animator.isActiveAndEnabled;
		if (isHuman)
		{
			m_animator.Update(0f);
			foreach (HumanBodyBones bone in s_poseBones)
			{
				Transform boneTransform = m_animator.GetBoneTransform(bone);
				if (boneTransform != null)
					bounds.Encapsulate(boneTransform.position);
			}

			Transform head = m_animator.GetBoneTransform(HumanBodyBones.Head);
			if (head != null)
			{
				foreach (Transform child in head.GetComponentsInChildren<Transform>())
					bounds.Encapsulate(child.position);
			}
		}

		bounds.Expand(new Vector3(_margin, _margin, _margin) * 2f);
		bounds.SetMinMax(new Vector3(bounds.min.x, origin.y, bounds.min.z), bounds.max);

		List<Renderer> renderers = new();
		CollectSkinRenderers(renderers);
		foreach (Renderer renderer in renderers)
		{
			if ((renderer is MeshRenderer || !isHuman) && renderer.enabled && renderer.gameObject.activeInHierarchy)
				bounds.Encapsulate(renderer.bounds);
		}

		return bounds;
	}

	private Weapon GuardWeapon ()
	{
		if (m_guardWeapon != null && m_guardWeapon.Data != null)
			return m_guardWeapon;

		Weapon found = null;
		foreach (Weapon weapon in m_linkedEntity.Equipment.Weapons.Values)
		{
			if (weapon == null || weapon.Data == null || string.IsNullOrEmpty(weapon.Data.guardAnimation))
				continue;
			if (found == null || m_humanoidEntityIK != null && !m_humanoidEntityIK.IsHeldInLeftHand(weapon.transform))
				found = weapon;
		}

		return found;
	}

	private bool TryGetLinkedWeapon ( AEntityAction _action, out Weapon _weapon )
	{
		_weapon = null;
		return !string.IsNullOrEmpty(_action.linkedEquipmentId) && m_linkedEntity.Equipment.Weapons.TryGetValue(_action.linkedEquipmentId, out _weapon) && _weapon != null;
	}

	private void OnDestroy ()
	{
		TurnManager.onStartInputPhase -= OnStartInputPhase;
		m_attackPlayer?.Stop();
	}

	private AttackAnimationPlayer.Tuning TuningFor ( EntityActionData _data, string _key )
	{
		return m_animationProfile != null ? m_animationProfile.TuningFor(_data, _key) : new AttackAnimationPlayer.Tuning(1f, 1f);
	}

	public void OnStartActionPerform ( AEntityAction _action )
	{
		if (m_animator != null)
			m_animator.speed = 1;
		m_performedAction = _action;
		if (!_action.Data.isModAction)
			AttackPlayer.OnActionStarted(_action);
		if (_action is AttackAction attackAction)
		{
			StartPreparation(attackAction);
			return;
		}

		if (_action.enumID == EntityActionEnumID.Wait)
			return;

		PlayActionAnimation(_action.Data, null, IsLinkedItemInLeftHand(_action));
	}

	private bool IsLinkedItemInLeftHand ( AEntityAction _action )
	{
		if (m_humanoidEntityIK == null || string.IsNullOrEmpty(_action.linkedEquipmentId) || m_linkedEntity == null || m_linkedEntity.Equipment == null)
			return false;

		if (TryGetLinkedWeapon(_action, out Weapon weapon))
			return m_humanoidEntityIK.IsHeldInLeftHand(weapon.transform);

		return m_linkedEntity.Equipment.Tools.TryGetValue(_action.linkedEquipmentId, out ToolEquipment tool) && tool != null && m_humanoidEntityIK.IsHeldInLeftHand(tool.transform);
	}

	private void StartPreparation ( AttackAction _action )
	{
		EntityActionData data = _action.Data;
		if (m_animator == null || _action.preparationDuration <= 0 || data.preparationAnimationMode == EntityActionData.PreparationAnimationMode.None
			|| _action.IsPerformingAtTick(TurnManager.currentTick) || !TryGetLinkedWeapon(_action, out Weapon weapon))
			return;

		m_guardWeapon = weapon;
		if (data.AimsDuringPreparation)
		{
			VisualyAimAt(_action.linkedEquipmentId, weapon.GetAimPoint(_action, 0), data);
			return;
		}

		bool isLeftHand = m_humanoidEntityIK != null && m_humanoidEntityIK.IsHeldInLeftHand(weapon.transform);
		float seconds = Mathf.Max(1, _action.TimeAtStartPerform - TurnManager.currentTick) * GameConfig.current.game.actionDuration;
		AttackPlayer.StartPreparation(_action, data, ResolveAnimationKey(data, weapon.Data.attackAnimationSuccessId), isLeftHand, AttackAnimationPlayer.Timing.From(data), seconds);
	}

	public bool PlayActionAnimation ( EntityActionData _data, string _fallbackKey = null, bool _isLeftHand = false )
	{
		return m_animator != null && _data != null && AttackPlayer.Play(_data, ResolveAnimationKey(_data, _fallbackKey), _isLeftHand);
	}

	public bool PlayAttackAnimation ( AttackAction _action, string _fallbackKey, bool _isLeftHand, bool _isFinalStrike, out float _impactDelay )
	{
		_impactDelay = 0f;
		if (m_animator == null || _action == null || _action.Data == null)
			return false;

		EntityActionData data = _action.Data;
		if (TryGetLinkedWeapon(_action, out Weapon weapon))
			m_guardWeapon = weapon;
		return AttackPlayer.Strike(_action, data, ResolveAnimationKey(data, _fallbackKey), _isLeftHand, AttackAnimationPlayer.Timing.From(data), _isFinalStrike && _action.cooldownDuration > 0, out _impactDelay);
	}

	private string ResolveAnimationKey ( EntityActionData _data, string _fallbackKey )
	{
		return !string.IsNullOrEmpty(_data.performAnimationKey) ? _data.performAnimationKey
			: !string.IsNullOrEmpty(_fallbackKey) ? _fallbackKey
			: m_animationKeyPerActionDictionary.TryGetValue(_data.type, out string key) ? key : null;
	}

	public void OnEndActionPerform ()
	{
		if (m_attackPlayer != null && m_attackPlayer.Owner is AEntityAction action && !action.IsPerforming)
			m_attackPlayer.EndAction(action);

		m_performedAction = null;
		ReleaseCurrentAim();
	}

	private void OnStartInputPhase ()
	{
		m_attackPlayer?.Cancel();
	}

	public void ReleaseCurrentAim ()
	{
		if (m_isAiming)
			ReleaseAim();
	}

	public void VisualyAimAt ( string _weaponID, Vector3 _aimedPosition, EntityActionData _action )
	{
		if (m_humanoidEntityIK == null || !m_linkedEntity.Equipment.Weapons.TryGetValue(_weaponID, out Weapon weapon) || weapon == null)
			return;

		m_isAiming = true;
		m_guardWeapon = weapon;
		m_humanoidEntityIK.Aim(weapon.transform.parent, _aimedPosition, weapon.transform);
		if (m_animator != null)
			AttackPlayer.PlayAim(_action, weapon.Data.aimAnimation, m_humanoidEntityIK.IsHeldInLeftHand(weapon.transform));
	}

	public void ReleaseAim ()
	{
		m_isAiming = false;
		m_attackPlayer?.ReleaseAim();
		if (m_humanoidEntityIK != null)
			m_humanoidEntityIK.ReleaseAim();
	}

	public void SetIsMoving ( bool _isMoving )
	{
		m_isMoving = _isMoving;
		if (_isMoving)
			m_moveStyle = m_performedAction != null ? m_performedAction.Data.movementAnimation : null;
		if (m_animator != null)
			LocomotionPlayer.Set(_isMoving, m_moveStyle, IsFlying);
	}

	private void OnEntityDeath ( int _entityID )
	{
		m_attackPlayer?.Stop();
		Hide();
	}

	public void Show ( NeuronalMembraneEquipmentData.VisionTypes _type)
	{
		bool isThermic = _type == NeuronalMembraneEquipmentData.VisionTypes.Thermic;
		bool hasThermicMesh = isThermic && m_thermicMaterial != null && m_visualRoot != null;

		m_isThermicLookWanted = hasThermicMesh;
		RefreshLook();
		SetRootActive(m_visualRoot, _type == NeuronalMembraneEquipmentData.VisionTypes.Optic || hasThermicMesh);
		SetRootActive(m_thermicRoot, isThermic && !hasThermicMesh);
		SetRootActive(m_radarRoot, _type == NeuronalMembraneEquipmentData.VisionTypes.Radar);
	}

	public void Hide ()
	{
		SetRootActive(m_visualRoot, false);
		SetRootActive(m_thermicRoot, false);
		SetRootActive(m_radarRoot, false);
	}

	private static void SetRootActive ( GameObject _root, bool _isActive )
	{
		if (_root != null)
			_root.SetActive(_isActive);
	}

	public void SetForcedLook ( Material _material )
	{
		m_forcedLook = _material;
		RefreshLook();
	}

	private void RefreshLook ()
	{
		Material wantedLook = m_forcedLook != null ? m_forcedLook : m_isThermicLookWanted ? m_thermicMaterial : null;
		if (wantedLook == m_appliedLook)
			return;

		RestoreLook();
		m_appliedLook = wantedLook;
		if (wantedLook == null)
			return;

		CollectSkinRenderers(m_lookRenderers);
		foreach (Renderer renderer in m_lookRenderers)
		{
			m_lookOriginalMaterials.Add(renderer.sharedMaterials);
			m_lookOriginalSortingOrders.Add(renderer.sortingOrder);
			m_lookOriginalShadowModes.Add(renderer.shadowCastingMode);
			OverrideMaterials(renderer, wantedLook);
		}
	}

	private void RestoreLook ()
	{
		for (int i = 0; i < m_lookRenderers.Count; i++)
		{
			Renderer renderer = m_lookRenderers[i];
			if (renderer == null)
				continue;

			renderer.sharedMaterials = m_lookOriginalMaterials[i];
			renderer.sortingOrder = m_lookOriginalSortingOrders[i];
			renderer.shadowCastingMode = m_lookOriginalShadowModes[i];
		}

		m_lookRenderers.Clear();
		m_lookOriginalMaterials.Clear();
		m_lookOriginalSortingOrders.Clear();
		m_lookOriginalShadowModes.Clear();
	}

	public void CollectSkinRenderers ( List<Renderer> _renderers )
	{
		if (m_visualRoot == null)
			return;

		foreach (Renderer renderer in m_visualRoot.GetComponentsInChildren<Renderer>(true))
		{
			if (IsSkinRenderer(renderer))
				_renderers.Add(renderer);
		}
	}

	private bool IsSkinRenderer ( Renderer _renderer )
	{
		if (_renderer is not MeshRenderer && _renderer is not SkinnedMeshRenderer)
			return false;

		return !IsUnder(_renderer, m_radarRoot) && !IsUnder(_renderer, m_thermicRoot);
	}

	private static bool IsUnder ( Renderer _renderer, GameObject _root )
	{
		return _root != null && _renderer.transform.IsChildOf(_root.transform);
	}

	public static void OverrideMaterials ( Renderer _renderer, Material _material )
	{
		Material[] materials = new Material[Mathf.Max(1, _renderer.sharedMaterials.Length)];
		for (int i = 0; i < materials.Length; i++)
			materials[i] = _material;

		_renderer.sharedMaterials = materials;
		_renderer.sortingOrder = OverFogSortingOrder;
		_renderer.shadowCastingMode = ShadowCastingMode.Off;
	}
}
