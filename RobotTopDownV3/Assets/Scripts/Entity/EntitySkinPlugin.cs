using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

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
	[SerializeField] private string m_idleAnimationKey;
	[SerializeField] private EntityAnimationProfile m_animationProfile;
	public EntityAnimationProfile AnimationProfile => m_animationProfile;

	public const string LeftHandAttackParameter = AttackAnimationPlayer.LeftHandParameter;
	public const string AttackSpeedParameter = AttackAnimationPlayer.SpeedParameter;
	public const string AttackLayerName = AttackAnimationPlayer.LayerName;
	private AttackAnimationPlayer m_attackPlayer;
	public AttackAnimationPlayer AttackPlayer => m_attackPlayer ??= new AttackAnimationPlayer(m_animator, TuningFor);
	private string m_aimingWeaponID;

	private GuardAnimationPlayer m_guardPlayer;
	public GuardAnimationPlayer GuardPlayer => m_guardPlayer ??= new GuardAnimationPlayer(m_animator);
	private bool m_isMoving;
	private Weapon m_guardWeapon;

	private bool m_isThermicLookWanted;
	private Material m_forcedLook;
	private Material m_appliedLook;
	private readonly List<Renderer> m_lookRenderers = new();
	public IReadOnlyList<Renderer> LookRenderers => m_lookRenderers;
	private readonly List<Material[]> m_lookOriginalMaterials = new();
	private readonly List<int> m_lookOriginalSortingOrders = new();
	private readonly List<ShadowCastingMode> m_lookOriginalShadowModes = new();

	public override void Init ( EntitySavedData _entityData )
	{
		base.Init(_entityData);

		m_linkedEntity.onStartPerformAction += OnStartActionPerform;
		m_linkedEntity.onEndPerformAction += OnEndActionPerform;
		m_linkedEntity.Equipment.onDeath += OnEntityDeath;
		TurnManager.onStartInputPhase += OnStartInputPhase;
		if (m_humanoidEntityIK != null)
			m_humanoidEntityIK.GripAmount = m_animationProfile != null ? m_animationProfile.gripAmount : 1f;
	}

	private void Update ()
	{
		if (m_animator == null || m_linkedEntity == null || m_linkedEntity.Equipment == null)
			return;

		Weapon weapon = m_isMoving ? null : GuardWeapon();
		bool isLeftHand = weapon != null && m_humanoidEntityIK != null && m_humanoidEntityIK.IsHeldInLeftHand(weapon.transform);
		GuardPlayer.Set(weapon != null ? weapon.Data.guardAnimation : null, isLeftHand, weapon != null ? weapon.Data.guardTorsoYaw : 0f);
		GuardPlayer.Update(Time.deltaTime);
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

	private void RememberGuardWeapon ( AttackAction _action )
	{
		if (m_linkedEntity.Equipment.Weapons.TryGetValue(_action.linkedEquipmentId ?? "", out Weapon weapon) && weapon != null)
			m_guardWeapon = weapon;
	}

	private void OnDestroy ()
	{
		TurnManager.onStartInputPhase -= OnStartInputPhase;
		m_attackPlayer?.Stop();
	}

	private AttackAnimationPlayer.Tuning TuningFor ( EntityActionData _data, string _key )
	{
		return m_animationProfile != null
			? new AttackAnimationPlayer.Tuning(m_animationProfile.SpeedFor(_data, _key), m_animationProfile.IntensityFor(_data, _key))
			: new AttackAnimationPlayer.Tuning(1f, 1f);
	}

	public void OnStartActionPerform (AEntityAction _action)
	{
		if (m_animator != null)
			m_animator.speed = 1;
		if (!_action.Data.isModAction)
			AttackPlayer.OnActionStarted(_action);
		if (_action is AttackAction attackAction)
		{
			StartPreparation(attackAction);
			return;
		}

		if (_action.enumID == EntityActionEnumID.Wait)
			return;

		PlayActionAnimation(_action.Data);
	}

	private void StartPreparation ( AttackAction _action )
	{
		EntityActionData data = _action.Data;
		if (m_animator == null || _action.preparationDuration <= 0 || data.preparationAnimationMode == EntityActionData.PreparationAnimationMode.None
			|| _action.IsPerformingAtTick(TurnManager.currentTick)
			|| !m_linkedEntity.Equipment.Weapons.TryGetValue(_action.linkedEquipmentId ?? "", out Weapon weapon) || weapon == null)
			return;

		m_guardWeapon = weapon;
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
		RememberGuardWeapon(_action);
		return AttackPlayer.Strike(_action, data, ResolveAnimationKey(data, _fallbackKey), _isLeftHand, AttackAnimationPlayer.Timing.From(data), _isFinalStrike && _action.cooldownDuration > 0, out _impactDelay);
	}

	private string ResolveAnimationKey ( EntityActionData _data, string _fallbackKey )
	{
		return !string.IsNullOrEmpty(_data.performAnimationKey) ? _data.performAnimationKey
			: !string.IsNullOrEmpty(_fallbackKey) ? _fallbackKey
			: m_animationKeyPerActionDictionary.ContainsKey(_data.type) ? m_animationKeyPerActionDictionary[_data.type] : null;
	}

	public void OverrideAnimation(string _animationID )
	{
		m_animator.SetTrigger(_animationID);
	}

	public void OnEndActionPerform ()
	{
		//m_animator.speed = 0;
		//freeze body anim
		//m_animator.SetTrigger("OnEndAction");

		if (m_attackPlayer != null && m_attackPlayer.Owner is AEntityAction action && !action.IsPerforming)
			m_attackPlayer.EndAction(action);

		ReleaseCurrentAim();
	}

	private void OnStartInputPhase ()
	{
		m_attackPlayer?.Cancel();
	}

	public void ReleaseCurrentAim ()
	{
		if (string.IsNullOrEmpty(m_aimingWeaponID))
			return;

		ReleaseAim(m_aimingWeaponID);
	}

	public void VisualyAimAt(string _weaponID, Vector3 _aimedPosition )
	{
		m_aimingWeaponID = _weaponID;
		Weapon weapon = m_linkedEntity.Equipment.Weapons[_weaponID];
		if (weapon.Data.isTwoHanded)
		{
			//play aim anim + rotate entity
			m_animator.SetTrigger("TwoHandAim");
			//m_linkedEntity.Equipment.AimAtTile(_weaponID)
		}
		else
		{
			m_humanoidEntityIK.Aim(weapon.transform.parent, _aimedPosition, weapon.transform);
		}

	}

	public void SetIsMoving(bool _isMoving )
	{
		m_isMoving = _isMoving;
		m_animator.SetBool("IsMoving", _isMoving);
	}

	private void OnEntityDeath (int _entityID)
	{
		//TODO : actual clean death with anim and PS
		m_attackPlayer?.Stop();
		Hide();
	}

	public void ReleaseAim ( string _weaponID )
	{
		m_aimingWeaponID = null;
		//The weapon can be gone by the time the aim is dropped, a destroyed arm typically
		if (!m_linkedEntity.Equipment.Weapons.ContainsKey(_weaponID))
		{
			m_humanoidEntityIK.ReleaseAim();
			return;
		}

		Weapon weapon = m_linkedEntity.Equipment.Weapons[_weaponID];
		if (weapon.Data.isTwoHanded)
		{
			//remove aim anim + rotate entity back to origin
			m_animator.SetTrigger("ReleaseAim");
		}
		else
		{
			//add ik to according hand
			m_humanoidEntityIK.ReleaseAim();
		}
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
		if (wantedLook == null || m_visualRoot == null)
			return;

		foreach (Renderer renderer in m_visualRoot.GetComponentsInChildren<Renderer>(true))
		{
			if (!IsSkinRenderer(renderer))
				continue;

			m_lookRenderers.Add(renderer);
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

	private bool IsSkinRenderer ( Renderer _renderer )
	{
		if (_renderer is not MeshRenderer && _renderer is not SkinnedMeshRenderer)
			return false;

		if (IsUnder(_renderer, m_radarRoot) || IsUnder(_renderer, m_thermicRoot))
			return false;

		return _renderer.GetComponentInParent<WeaponCone>(true) == null;
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
