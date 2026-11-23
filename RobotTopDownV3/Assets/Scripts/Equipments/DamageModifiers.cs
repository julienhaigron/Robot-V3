using UnityEngine;
using StatType = EntityEquipmentData.SecondaryStat.StatType;

public readonly struct DamageModifiers
{
	public readonly float type;
	public readonly float category;
	public readonly float general;
	public readonly float flank;
	public readonly float final;

	public float Multiplier => (1f + type) * (1f + category) * (1f + general) * (1f + flank) * (1f + final);

	public DamageModifiers ( Entity _user, Entity _target, AEntityAction _action, WeaponEquipmentData.DamageType _damageType, Tile.TileDirectionType _side )
	{
		GameConfig.GameSettings config = GameConfig.current.game;
		WeaponEquipmentData.DamageCategory damageCategory = config.damageCategoryPerDamageType[_damageType];
		float flankStats = _side == Tile.TileDirectionType.Front ? 0f
			: Bonus(_user, StatType.FlankDamageBonus, _action) - Resistance(_target, StatType.FlankResistance);

		type = Combine(_user, config.statTypePerDamageType[_damageType], _action, _target, config.statTypePerDamageResistanceType[_damageType]);
		category = Combine(_user, config.statTypePerDamageCategory[damageCategory], _action, _target, config.statTypePerResistanceCategory[damageCategory]);
		general = Combine(_user, StatType.GeneralDamageBonus, _action, _target, StatType.GeneralDamageResistance);
		flank = Combine(config.entityFlankRatio[_side] + flankStats, 0f);
		final = Combine(Bonus(_user, StatType.FinalDamageBonus, _action), 0f);
	}

	public static float Combine ( float _bonus, float _resistance )
	{
		return Mathf.Max(_bonus - _resistance, -1f);
	}

	private static float Combine ( Entity _user, StatType _bonus, AEntityAction _action, Entity _target, StatType _resistance )
	{
		return Combine(Bonus(_user, _bonus, _action), Resistance(_target, _resistance));
	}

	private static float Bonus ( Entity _user, StatType _stat, AEntityAction _action )
	{
		return _user == null ? 0f : _user.GetStatBonus(_stat, _action);
	}

	private static float Resistance ( Entity _target, StatType _stat )
	{
		return _target == null ? 0f : _target.GetStatBonus(_stat);
	}
}
