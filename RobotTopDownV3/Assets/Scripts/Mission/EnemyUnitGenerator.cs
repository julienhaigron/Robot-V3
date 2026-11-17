using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class GeneratedUnitData
{
	public string displayName;
	public bool isExpensive;
	public int maxEnergy;
	public string frame;
	public string reactor;
	public string brain;
	public string neuronalMembrane;
	public List<string> arms = new();
	public List<string> auxiliary = new();
	public List<string> chipsets = new();
}

public static class EnemyUnitGenerator
{
	private const int c_armsSlot = 0;
	private const int c_armorSlot = 1;
	private const int c_occultorSlot = 2;
	private const int c_chipsetSlot = 3;

	private class FactionPool
	{
		public EntityEquipmentData.EntityFaction faction;
		public readonly List<FrameEquipmentData> frames = new();
		public readonly List<ReactorEquipmentData> reactors = new();
		public readonly List<BrainEquipmentData> brains = new();
		public readonly List<NeuronalMembraneEquipmentData> membranes = new();
		public readonly List<EntityEquipmentData>[] slotComponents = { new(), new(), new(), new() };

		public List<EntityEquipmentData> Armaments => slotComponents[c_armsSlot];
		public bool IsComplete => frames.Count > 0 && reactors.Count > 0 && brains.Count > 0 && membranes.Count > 0 && Armaments.Count > 0;
		public int MinimumUnitCost => frames.Min(data => data.energyCost) + brains.Min(data => data.energyCost)
			+ membranes.Min(data => data.energyCost) + Armaments.Min(data => data.energyCost);
	}

	private class MainComponents
	{
		public readonly FrameEquipmentData frame;
		public readonly BrainEquipmentData brain;
		public readonly NeuronalMembraneEquipmentData membrane;
		public readonly int[] slotCounts;
		public readonly int fixedCost;

		public MainComponents ( FrameEquipmentData _frame, BrainEquipmentData _brain, NeuronalMembraneEquipmentData _membrane )
		{
			frame = _frame;
			brain = _brain;
			membrane = _membrane;
			slotCounts = new[] { _membrane.equipmentSlotAvailable, _frame.armouringSlotAvailable, _frame.occultorSlotAvailable, _brain.chipsetSlotAvailable };
			fixedCost = _frame.energyCost + _brain.energyCost + _membrane.energyCost;
		}

		public int GetCapacity ( int[] _maxSlotCosts )
		{
			return fixedCost + slotCounts.Select(( count, slotType ) => count * _maxSlotCosts[slotType]).Sum();
		}
	}

	public static List<GeneratedUnitData> GenerateSquad ( PlaythroughPhase _phase )
	{
		List<GeneratedUnitData> squad = new();
		GameConfig.EnemySquadRules rules = ProceduralMatchGenerator.GetSquadRules(_phase);
		List<FactionPool> pools = BuildPools();
		if (rules == null || rules.maxUnitCount <= 0 || pools.Count == 0)
			return squad;

		int unitCount = rules.maxUnitCount;
		int cheapestUnit = pools.Min(pool => pool.MinimumUnitCost);
		while (unitCount > 1 && unitCount * cheapestUnit > rules.maxEnergy)
			unitCount--;

		bool[] isExpensive = RollExpensiveUnits(unitCount);
		float expensiveRatio = Mathf.Max(1f, GameConfig.current.game.expensiveEnemyEnergyRatio);
		float energyPerShare = rules.maxEnergy / isExpensive.Sum(expensive => expensive ? expensiveRatio : 1f);
		int cheapUnitMaxEnergy = Mathf.FloorToInt(energyPerShare);

		for (int i = 0; i < unitCount; i++)
		{
			int maxEnergy = isExpensive[i] ? Mathf.FloorToInt(energyPerShare * expensiveRatio) : cheapUnitMaxEnergy;
			int energyToExceed = isExpensive[i] ? cheapUnitMaxEnergy : 0;
			GeneratedUnitData unit = Shuffle(pools).Select(pool => GenerateUnit(pool, maxEnergy, energyToExceed)).FirstOrDefault(generated => generated != null);
			if (unit == null)
				continue;

			unit.isExpensive = isExpensive[i];
			unit.maxEnergy = maxEnergy;
			squad.Add(unit);
		}

		return squad;
	}

	public static UnitPreset BuildPreset ( GeneratedUnitData _unit )
	{
		FrameEquipmentData frame = Find<FrameEquipmentData>(_unit?.frame);
		ReactorEquipmentData reactor = Find<ReactorEquipmentData>(_unit?.reactor);
		BrainEquipmentData brain = Find<BrainEquipmentData>(_unit?.brain);
		NeuronalMembraneEquipmentData membrane = Find<NeuronalMembraneEquipmentData>(_unit?.neuronalMembrane);
		if (frame == null || reactor == null || brain == null || membrane == null)
			return null;

		UnitPreset preset = ScriptableObject.CreateInstance<UnitPreset>();
		preset.name = "Generated_" + _unit.displayName;
		preset.displayName = _unit.displayName;
		preset.frame = frame;
		preset.reactor = reactor;
		preset.brain = brain;
		preset.neuronalMembrane = membrane;
		preset.arms = FindAll<EntityEquipmentData>(_unit.arms);
		preset.auxiliary = FindAll<EntityEquipmentData>(_unit.auxiliary);
		preset.chipsets = FindAll<ChipsetEquipmentData>(_unit.chipsets);
		preset.icon = frame.icon;
		preset.aiRole = EnnemiAIRole.Aggressive;
		return preset;
	}

	private static bool[] RollExpensiveUnits ( int _unitCount )
	{
		float chance = GameConfig.current.game.expensiveEnemyChance;
		bool[] isExpensive = new bool[_unitCount];
		for (int i = 0; i < _unitCount; i++)
			isExpensive[i] = UnityEngine.Random.value < chance;

		if (_unitCount > 1 && isExpensive.All(expensive => expensive == isExpensive[0]))
			isExpensive[UnityEngine.Random.Range(0, _unitCount)] ^= true;

		return isExpensive;
	}

	private static GeneratedUnitData GenerateUnit ( FactionPool _pool, int _maxEnergy, int _energyToExceed )
	{
		ReactorEquipmentData reactor = PickReactor(_pool, _maxEnergy);
		int energyCap = Mathf.Min(_maxEnergy, reactor.energyProduced);
		MainComponents main = PickMainComponents(_pool, energyCap, _energyToExceed);
		if (main == null)
			return null;

		GeneratedUnitData unit = new()
		{
			displayName = GetDisplayName(_pool.faction, main.frame),
			frame = main.frame.name,
			reactor = reactor.name,
			brain = main.brain.name,
			neuronalMembrane = main.membrane.name
		};

		List<string>[] containers = { unit.arms, unit.auxiliary, unit.auxiliary, unit.chipsets };
		int[] freeSlots = (int[])main.slotCounts.Clone();
		int usedEnergy = main.fixedCost;

		void Equip ( int _slotType, EntityEquipmentData _data )
		{
			containers[_slotType].Add(_data.name);
			freeSlots[_slotType]--;
			usedEnergy += _data.energyCost;
		}

		Equip(c_armsSlot, PickRandom(_pool.Armaments.Where(data => usedEnergy + data.energyCost <= energyCap).ToList()));

		while (true)
		{
			List<(int slotType, EntityEquipmentData data)> options = new();
			for (int slotType = 0; slotType < freeSlots.Length; slotType++)
			{
				if (freeSlots[slotType] > 0)
					options.AddRange(_pool.slotComponents[slotType].Where(data => usedEnergy + data.energyCost <= energyCap).Select(data => (slotType, data)));
			}

			if (options.Count == 0)
				return unit;

			(int slotType, EntityEquipmentData data) option = PickRandom(options);
			Equip(option.slotType, option.data);
		}
	}

	private static ReactorEquipmentData PickReactor ( FactionPool _pool, int _maxEnergy )
	{
		int bestUsableEnergy = _pool.reactors.Max(reactor => Mathf.Min(_maxEnergy, reactor.energyProduced));
		return PickRandom(_pool.reactors.Where(reactor => Mathf.Min(_maxEnergy, reactor.energyProduced) == bestUsableEnergy).ToList());
	}

	private static MainComponents PickMainComponents ( FactionPool _pool, int _energyCap, int _energyToExceed )
	{
		int cheapestArmament = _pool.Armaments.Min(data => data.energyCost);
		int[] maxSlotCosts = _pool.slotComponents.Select(components => components.Count == 0 ? 0 : components.Max(data => data.energyCost)).ToArray();
		List<MainComponents> candidates = _pool.frames
			.SelectMany(frame => _pool.brains.SelectMany(brain => _pool.membranes.Select(membrane => new MainComponents(frame, brain, membrane))))
			.Where(main => main.slotCounts[c_armsSlot] > 0 && main.fixedCost + cheapestArmament <= _energyCap)
			.ToList();
		if (candidates.Count == 0)
			return null;

		List<MainComponents> bigEnough = candidates.Where(main => Mathf.Min(_energyCap, main.GetCapacity(maxSlotCosts)) > _energyToExceed).ToList();
		if (bigEnough.Count > 0)
			return PickRandom(bigEnough);

		int bestCapacity = candidates.Max(main => main.GetCapacity(maxSlotCosts));
		return PickRandom(candidates.Where(main => main.GetCapacity(maxSlotCosts) == bestCapacity).ToList());
	}

	private static List<FactionPool> BuildPools ()
	{
		Dictionary<EntityEquipmentData.EntityFaction, FactionPool> pools = new();
		foreach (EntityEquipmentData data in GameAssets.current.equipments.Values)
		{
			if (data == null || !data.canDropRandomly || data.faction == EntityEquipmentData.EntityFaction.Noone)
				continue;

			if (!pools.TryGetValue(data.faction, out FactionPool pool))
			{
				pool = new FactionPool { faction = data.faction };
				pools.Add(data.faction, pool);
			}

			switch (data)
			{
				case FrameEquipmentData frame:
					if (!frame.isImmortal && frame.prefab != null)
						pool.frames.Add(frame);
					break;
				case ReactorEquipmentData reactor:
					pool.reactors.Add(reactor);
					break;
				case BrainEquipmentData brain:
					pool.brains.Add(brain);
					break;
				case NeuronalMembraneEquipmentData membrane:
					pool.membranes.Add(membrane);
					break;
				case WeaponEquipmentData _:
				case ToolEquipmentData _:
					pool.slotComponents[c_armsSlot].Add(data);
					break;
				case ArmorEquipmentData _:
					pool.slotComponents[c_armorSlot].Add(data);
					break;
				case OccultorEquipmentData _:
					pool.slotComponents[c_occultorSlot].Add(data);
					break;
				case ChipsetEquipmentData _:
					pool.slotComponents[c_chipsetSlot].Add(data);
					break;
			}
		}

		return pools.Values.Where(pool => pool.IsComplete).ToList();
	}

	private static string GetDisplayName ( EntityEquipmentData.EntityFaction _faction, FrameEquipmentData _frame )
	{
		string frameName = string.IsNullOrEmpty(_frame.displayName) || _frame.displayName == "default" ? _frame.name : _frame.displayName;
		return _faction + " " + frameName;
	}

	private static T Find<T> ( string _name ) where T : EntityEquipmentData
	{
		return !string.IsNullOrEmpty(_name) && GameAssets.current.equipments.TryGetValue(_name, out EntityEquipmentData data) ? data as T : null;
	}

	private static T[] FindAll<T> ( List<string> _names ) where T : EntityEquipmentData
	{
		return _names == null ? Array.Empty<T>() : _names.Select(Find<T>).Where(data => data != null).ToArray();
	}

	private static List<T> Shuffle<T> ( List<T> _items )
	{
		return _items.OrderBy(_ => UnityEngine.Random.value).ToList();
	}

	private static T PickRandom<T> ( List<T> _items )
	{
		return _items.Count == 0 ? default : _items[UnityEngine.Random.Range(0, _items.Count)];
	}
}
