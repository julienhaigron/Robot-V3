using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntitySkinPlugin : EntityPlugin
{
	[SerializeField] private GameObject m_visualRoot;
	[SerializeField] private GameObject m_radarRoot;
	[SerializeField] private GameObject m_thermicRoot;
	[SerializeField] private Transform m_center;
	public Transform Center => m_center;

	[SerializeField] private Animator m_animator;
	[SerializeField] private HumanoidEntityIK m_humanoidEntityIK;
	public HumanoidEntityIK IK => m_humanoidEntityIK;

	[SerializeField] private SerializableDictionary<EntityActionData.ActionType, string> m_animationKeyPerActionDictionary;
	[SerializeField] private string m_idleAnimationKey;

	private string m_aimingWeaponID;


	public override void Init ( EntitySavedData _entityData )
	{
		base.Init(_entityData);

		m_linkedEntity.onStartPerformAction += OnStartActionPerform;
		m_linkedEntity.onEndPerformAction += OnEndActionPerform;
		m_linkedEntity.Equipment.onDeath += OnEntityDeath;
	}

	public void OnStartActionPerform (AEntityAction _action)
	{
		m_animator.speed = 1;
		if(_action.enumID == EntityActionEnumID.Wait)
		{
			//m_animator.SetTrigger(m_idleAnimationKey);

		}
		else if (m_animationKeyPerActionDictionary.ContainsKey(_action.Data.type))
			m_animator.SetTrigger(m_animationKeyPerActionDictionary[_action.Data.type]);
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
			//add ik to according hand
			//m_animator.SetTrigger("RightHandAim");
			m_humanoidEntityIK.rightHandTarget = _aimedPosition;
			m_humanoidEntityIK.Aim(_aimedPosition);
		}

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
		switch (_type)
		{
			case NeuronalMembraneEquipmentData.VisionTypes.Optic:
				m_visualRoot.SetActive(true);
				m_thermicRoot.SetActive(false);
				m_radarRoot.SetActive(false);
				break;
			case NeuronalMembraneEquipmentData.VisionTypes.Thermic:
				m_visualRoot.SetActive(false);
				m_thermicRoot.SetActive(true);
				m_radarRoot.SetActive(false);
				break;
			case NeuronalMembraneEquipmentData.VisionTypes.Radar:
				m_visualRoot.SetActive(false);
				m_thermicRoot.SetActive(false);
				m_radarRoot.SetActive(true);
				break;
		}
	}

	public void Hide ()
	{
		m_visualRoot.SetActive(false);
		m_thermicRoot.SetActive(false);
		m_radarRoot.SetActive(false);
	}
}
