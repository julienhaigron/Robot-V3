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

	private const string MirrorAttackParameter = "MirrorAttack";
	private HashSet<string> m_animatorParameters;
	private string m_aimingWeaponID;

	private bool m_isThermicLookWanted;
	private Material m_forcedLook;
	private Material m_appliedLook;
	private readonly List<Renderer> m_lookRenderers = new();
	private readonly List<Material[]> m_lookOriginalMaterials = new();
	private readonly List<int> m_lookOriginalSortingOrders = new();
	private readonly List<ShadowCastingMode> m_lookOriginalShadowModes = new();

	public override void Init ( EntitySavedData _entityData )
	{
		base.Init(_entityData);

		m_linkedEntity.onStartPerformAction += OnStartActionPerform;
		m_linkedEntity.onEndPerformAction += OnEndActionPerform;
		m_linkedEntity.Equipment.onDeath += OnEntityDeath;
	}

	public void OnStartActionPerform (AEntityAction _action)
	{
		if (m_animator != null)
			m_animator.speed = 1;
		if (_action.enumID == EntityActionEnumID.Wait || _action is AttackAction)
			return;

		PlayActionAnimation(_action.Data);
	}

	public bool PlayActionAnimation ( EntityActionData _data, string _fallbackKey = null, bool _isMirrored = false )
	{
		if (m_animator == null || _data == null)
			return false;

		string key = !string.IsNullOrEmpty(_data.performAnimationKey) ? _data.performAnimationKey
			: !string.IsNullOrEmpty(_fallbackKey) ? _fallbackKey
			: m_animationKeyPerActionDictionary.ContainsKey(_data.type) ? m_animationKeyPerActionDictionary[_data.type] : null;
		if (string.IsNullOrEmpty(key))
			return false;

		if (HasAnimatorParameter(MirrorAttackParameter))
			m_animator.SetBool(MirrorAttackParameter, _isMirrored);
		m_animator.SetTrigger(key);
		return true;
	}

	private bool HasAnimatorParameter ( string _name )
	{
		if (m_animatorParameters == null)
		{
			if (m_animator.runtimeAnimatorController == null || m_animator.parameterCount == 0)
				return false;

			m_animatorParameters = new HashSet<string>(m_animator.parameters.Select(parameter => parameter.name));
		}

		return m_animatorParameters.Contains(_name);
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

		ReleaseCurrentAim();
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
			m_humanoidEntityIK.Aim(weapon.transform.parent, _aimedPosition);
		}

	}

	public void SetIsMoving(bool _isMoving )
	{
		m_animator.SetBool("IsMoving", _isMoving);
	}

	private void OnEntityDeath (int _entityID)
	{
		//TODO : actual clean death with anim and PS
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
