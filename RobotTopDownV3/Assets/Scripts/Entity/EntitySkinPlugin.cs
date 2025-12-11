using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntitySkinPlugin : EntityPlugin
{
	[SerializeField] private Animator m_animator;

	[SerializeField] private SerializableDictionary<EntityActionEnumID, string> m_animationKeyPerActionDictionary;


	public override void Init ()
	{
		base.Init();

		m_linkedEntity.onStartPerformAction += OnStartActionPerform;
		m_linkedEntity.onEndPerformAction += OnEndActionPerform;
	}

	public void OnStartActionPerform (AEntityAction _action)
	{
		m_animator.speed = 1;
		if (m_animationKeyPerActionDictionary.ContainsKey(_action.enumID))
			m_animator.SetTrigger(m_animationKeyPerActionDictionary[_action.enumID]);
	}

	public void OnEndActionPerform ()
	{
		m_animator.speed = 0;
		//freeze body anim
		//m_animator.SetTrigger("OnEndAction");
	}
}
