using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationProfile", menuName = "ScriptableObject/EntityAnimationProfile")]
public class EntityAnimationProfile : ScriptableObject
{
	public const float MinSpeed = .3f;
	public const float MaxSpeed = 2f;
	public const float MinIntensity = .1f;

	public enum TuningSource { Default, Trigger, Action }

	[Serializable]
	public class AttackTuning
	{
		[Tooltip("Action réglée. Vide : l'entrée vaut pour toutes les actions du déclencheur qui n'ont pas leur propre entrée.")]
		public EntityActionData action;
		[Tooltip("Déclencheur (performAnimationKey) de l'action, ou déclencheur réglé quand l'action est vide.")]
		public string trigger;
		[Range(MinSpeed, MaxSpeed)] public float speed = 1f;
		[Range(MinIntensity, 1f)] public float intensity = 1f;
	}

	[Tooltip("Multiplicateur de vitesse des attaques de l'unité, appliqué par-dessus la vitesse des états du contrôleur. Le délai d'impact des actions est divisé d'autant.")]
	[Range(MinSpeed, MaxSpeed)] public float attackSpeed = 1f;
	[Tooltip("Poids du calque d'attaque : 1 joue le clip entier, moins garde une part de la pose de repos et réduit l'amplitude.")]
	[Range(MinIntensity, 1f)] public float attackIntensity = 1f;
	[Tooltip("Réglages par action, puis par déclencheur (action vide). Ce qui n'a pas d'entrée utilise les deux valeurs ci-dessus.")]
	public List<AttackTuning> attacks = new();

	public AttackTuning Find ( EntityActionData _action, string _trigger )
	{
		return FindOwn(_action) ?? FindTrigger(_trigger);
	}

	public AttackTuning FindOwn ( EntityActionData _action )
	{
		if (_action == null || attacks == null)
			return null;

		foreach (AttackTuning tuning in attacks)
		{
			if (tuning != null && tuning.action == _action)
				return tuning;
		}

		return null;
	}

	public AttackTuning FindTrigger ( string _trigger )
	{
		if (string.IsNullOrEmpty(_trigger) || attacks == null)
			return null;

		foreach (AttackTuning tuning in attacks)
		{
			if (tuning != null && tuning.action == null && tuning.trigger == _trigger)
				return tuning;
		}

		return null;
	}

	public TuningSource SourceFor ( EntityActionData _action, string _trigger )
	{
		return FindOwn(_action) != null ? TuningSource.Action : FindTrigger(_trigger) != null ? TuningSource.Trigger : TuningSource.Default;
	}

	public float SpeedFor ( EntityActionData _action, string _trigger )
	{
		AttackTuning tuning = Find(_action, _trigger);
		return Mathf.Clamp(tuning != null ? tuning.speed : attackSpeed, MinSpeed, MaxSpeed);
	}

	public float IntensityFor ( EntityActionData _action, string _trigger )
	{
		AttackTuning tuning = Find(_action, _trigger);
		return Mathf.Clamp(tuning != null ? tuning.intensity : attackIntensity, MinIntensity, 1f);
	}
}
