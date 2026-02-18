using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntitySkinPlugin : EntityPlugin
{
	[SerializeField] private GameObject m_visualRoot;
	[SerializeField] private Transform m_center;
	public Transform Center => m_center;

	[SerializeField] private Animator m_animator;
	[SerializeField] private HumanoidEntityIK m_humanoidEntityIK;
	public HumanoidEntityIK IK => m_humanoidEntityIK;

	[SerializeField] private SerializableDictionary<EntityActionData.ActionType, string> m_animationKeyPerActionDictionary;


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
		if (_action is WaitAction waitAction && waitAction.isLinkedToAction)
			m_animator.SetTrigger(GameAssets.current.game.entityActionsData[waitAction.linkedActionID].preparationAnimationKey);
		else if (m_animationKeyPerActionDictionary.ContainsKey(_action.Data.type))
			m_animator.SetTrigger(m_animationKeyPerActionDictionary[_action.Data.type]);
	}

	public void OverrideAnimation(string _animationID )
	{
		m_animator.SetTrigger(_animationID);
	}

	public void OnEndActionPerform ()
	{
		m_animator.speed = 0;
		//freeze body anim
		//m_animator.SetTrigger("OnEndAction");
	}

	public void VisualyAimAt(string _weaponID, Vector3 _aimedPosition )
	{
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
			m_humanoidEntityIK.Aim(_aimedPosition);
		}

	}

	private void OnEntityDeath (int _entityID)
	{
		//TODO : actual clean death with anim and PS
		m_visualRoot.SetActive(false);
	}

	public void ReleaseAim ( string _weaponID )
	{
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
}
