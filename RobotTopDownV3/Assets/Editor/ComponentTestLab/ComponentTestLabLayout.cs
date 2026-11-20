using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EquipmentType = EntityEquipmentData.EquipmentType;
using ActionType = EntityActionData.ActionType;
using AOEType = EntityActionData.AOEType;

public enum ComponentTestLabCoverState { None, Cover, Wall }

public class ComponentTestLabUnitSpec
{
	public FrameEquipmentData frame;
	public ReactorEquipmentData reactor;
	public BrainEquipmentData brain;
	public NeuronalMembraneEquipmentData membrane;
	public List<EntityEquipmentData> arms = new();
	public List<EntityEquipmentData> auxiliary = new();
	public List<ChipsetEquipmentData> chipsets = new();

	public bool IsValid => frame != null && frame.prefab != null && reactor != null && brain != null && membrane != null;

	public ComponentTestLabUnitSpec Clone ()
	{
		return new ComponentTestLabUnitSpec
		{
			frame = frame,
			reactor = reactor,
			brain = brain,
			membrane = membrane,
			arms = new(arms),
			auxiliary = new(auxiliary),
			chipsets = new(chipsets),
		};
	}

	public UnitPreset CreatePreset ( string _name )
	{
		UnitPreset preset = ScriptableObject.CreateInstance<UnitPreset>();
		preset.name = "ComponentTestLab_" + _name;
		preset.displayName = _name;
		preset.frame = frame;
		preset.reactor = reactor;
		preset.brain = brain;
		preset.neuronalMembrane = membrane;
		preset.arms = arms.Where(component => component != null).ToArray();
		preset.auxiliary = auxiliary.Where(component => component != null).ToArray();
		preset.chipsets = chipsets.Where(component => component != null).ToArray();
		preset.icon = frame.icon;
		preset.aiRole = EnnemiAIRole.Immobile;
		return preset;
	}
}

public class ComponentTestLabStation
{
	public string id;
	public string label;
	public EntityEquipmentData component;
	public bool isBench;
	public bool hasDefaultWeapon;
	public ComponentTestLabUnitSpec unit;
	public ComponentTestLabUnitSpec dummy;
	public int distance;
	public int unitTile = -1;
	public int coverTile = -1;
	public readonly List<EntityActionEnumID> testedActions = new();
	public readonly List<int> dummyTiles = new();
	public readonly List<string> warnings = new();

	public string Group => isBench ? "Banc libre" : ComponentTestLabLayout.GetGroupName(component.GetEquipmentType());
}

public class ComponentTestLabLayout
{
	public const int c_maxDistance = 8;
	public const string c_benchID = "bench";
	private const int c_blockWidth = 11;
	private const int c_blockHeight = 7;
	private const int c_laneRow = 3;
	private const int c_unitColumn = 1;
	private const int c_defaultDistance = 2;
	private const int c_maxRangeDummies = 2;
	private const int c_maxColumns = 6;

	private readonly struct Interval
	{
		public readonly int min;
		public readonly int max;

		public Interval ( int _min, int _max )
		{
			min = _min;
			max = _max;
		}

		public bool Contains ( int _distance ) => _distance >= min && _distance <= max;
		public int Preferred => min < max ? min + 1 : min;
	}

	private class Requirements
	{
		public readonly List<Interval> intervals = new();
		public bool needsArea;
		public int rayRange;
	}

	private readonly Dictionary<ComponentTestLabStation, Requirements> m_requirements = new();
	private List<BrainEquipmentData> m_brains = new();
	private BrainEquipmentData m_fixedBrain;
	private ComponentTestLabUnitSpec m_dummySpec;

	public int Width { get; private set; }
	public int Height { get; private set; }
	public TileGroundType[] GroundTypes { get; private set; } = Array.Empty<TileGroundType>();
	public List<ComponentTestLabStation> Stations { get; } = new();
	public List<string> Errors { get; } = new();
	public List<string> Skipped { get; } = new();
	public bool IsValid => Errors.Count == 0;

	public FrameEquipmentData BaseFrame { get; private set; }
	public ReactorEquipmentData BaseReactor { get; private set; }
	public NeuronalMembraneEquipmentData BaseMembrane { get; private set; }
	public WeaponEquipmentData DefaultWeapon { get; private set; }
	public UnitPreset DummyPreset { get; private set; }

	public static ComponentTestLabLayout Build ( ComponentTestLabConfig _config )
	{
		ComponentTestLabLayout layout = new();
		if (_config == null)
			layout.Errors.Add("Configuration du labo introuvable.");
		else
			layout.Compute(_config);

		return layout;
	}

	private void Compute ( ComponentTestLabConfig _config )
	{
		GameAssets assets = GameAssets.current;
		if (assets == null || assets.equipments == null || assets.game == null || assets.game.entityActionsData == null)
		{
			Errors.Add("GameAssets introuvable.");
			return;
		}

		List<EntityEquipmentData> components = assets.equipments.Values
			.Where(component => component != null)
			.Distinct()
			.OrderBy(component => GetTypeOrder(component.GetEquipmentType()))
			.ThenBy(component => component.faction)
			.ThenBy(component => component.name, StringComparer.Ordinal)
			.ToList();

		m_brains = components.OfType<BrainEquipmentData>().ToList();
		m_fixedBrain = _config.baseBrain;
		BaseFrame = _config.baseFrame != null ? _config.baseFrame : PickBaseFrame(components);
		BaseReactor = _config.baseReactor != null ? _config.baseReactor : PickBaseReactor(components);
		BaseMembrane = _config.baseMembrane != null ? _config.baseMembrane : PickBaseMembrane(components);
		DefaultWeapon = _config.defaultWeapon != null ? _config.defaultWeapon : PickDefaultWeapon(components);
		DummyPreset = _config.dummyPreset != null ? _config.dummyPreset : PickDummyPreset();

		if (BaseFrame == null || BaseFrame.prefab == null)
			Errors.Add("Aucun châssis de base utilisable (il faut un prefab et une action de déplacement).");
		if (BaseReactor == null)
			Errors.Add("Aucun réacteur de base.");
		if (BaseMembrane == null)
			Errors.Add("Aucune membrane de base.");
		if (m_fixedBrain == null && m_brains.Count == 0)
			Errors.Add("Aucun cerveau.");
		if (!IsValid)
			return;

		m_dummySpec = CreateDummySpec();
		if (!m_dummySpec.IsValid)
		{
			Errors.Add("Le mannequin n'a pas de composants principaux complets.");
			return;
		}

		HashSet<EntityActionEnumID> baseActions = new(GetActionIDs(BaseFrame).Concat(GetActionIDs(BaseReactor)).Concat(GetActionIDs(BaseMembrane)));
		if (m_fixedBrain != null)
			baseActions.UnionWith(GetActionIDs(m_fixedBrain));

		Stations.Add(CreateBenchStation(_config));
		foreach (EntityEquipmentData component in components)
		{
			ComponentTestLabStation station = TryCreateStation(component, baseActions);
			if (station != null)
				Stations.Add(station);
		}

		Place(_config);
	}

	#region Stations

	private ComponentTestLabStation CreateBenchStation ( ComponentTestLabConfig _config )
	{
		ComponentTestLabStation station = new()
		{
			id = c_benchID,
			label = "Banc libre",
			isBench = true,
			unit = CreateBenchUnit(_config.benchUnit),
			dummy = CreateBenchDummy(_config.benchDummy),
		};
		m_requirements.Add(station, new Requirements());
		return station;
	}

	public ComponentTestLabUnitSpec CreateBenchUnit ( ComponentTestLabLoadout _loadout )
	{
		_loadout ??= new ComponentTestLabLoadout();
		ComponentTestLabUnitSpec unit = new()
		{
			frame = Or(_loadout.frame, BaseFrame),
			reactor = Or(_loadout.reactor, BaseReactor),
			membrane = Or(_loadout.membrane, BaseMembrane),
			arms = NonNull(_loadout.arms),
			auxiliary = NonNull(_loadout.auxiliary),
			chipsets = NonNull(_loadout.chipsets),
		};

		if (unit.arms.Count == 0 && DefaultWeapon != null)
			unit.arms.Add(DefaultWeapon);

		unit.brain = _loadout.brain != null ? _loadout.brain : PickBrain(unit.arms.SelectMany(GetActions));
		return unit;
	}

	public ComponentTestLabUnitSpec CreateBenchDummy ( ComponentTestLabLoadout _loadout )
	{
		_loadout ??= new ComponentTestLabLoadout();
		ComponentTestLabUnitSpec dummy = m_dummySpec.Clone();
		dummy.frame = Or(_loadout.frame, dummy.frame);
		dummy.reactor = Or(_loadout.reactor, dummy.reactor);
		dummy.brain = Or(_loadout.brain, dummy.brain);
		dummy.membrane = Or(_loadout.membrane, dummy.membrane);

		List<EntityEquipmentData> arms = NonNull(_loadout.arms);
		if (arms.Count > 0)
			dummy.arms = arms;
		List<EntityEquipmentData> auxiliary = NonNull(_loadout.auxiliary);
		if (auxiliary.Count > 0)
			dummy.auxiliary = auxiliary;
		List<ChipsetEquipmentData> chipsets = NonNull(_loadout.chipsets);
		if (chipsets.Count > 0)
			dummy.chipsets = chipsets;

		return dummy;
	}

	private ComponentTestLabStation TryCreateStation ( EntityEquipmentData _component, HashSet<EntityActionEnumID> _baseActions )
	{
		EquipmentType type = _component.GetEquipmentType();
		bool isArm = type == EquipmentType.Weapon || type == EquipmentType.Tool;
		List<EntityActionEnumID> grantedActions = GetActionIDs(_component).Where(id => !_baseActions.Contains(id)).Distinct().ToList();
		if (!isArm && grantedActions.Count == 0 && !HasPassiveEffects(_component))
			return null;

		ComponentTestLabUnitSpec unit = new() { frame = BaseFrame, reactor = BaseReactor, membrane = BaseMembrane };
		switch (type)
		{
			case EquipmentType.Weapon:
			case EquipmentType.Tool:
				unit.arms.Add(_component);
				break;
			case EquipmentType.Frame:
				unit.frame = (FrameEquipmentData)_component;
				break;
			case EquipmentType.Reactor:
				unit.reactor = (ReactorEquipmentData)_component;
				break;
			case EquipmentType.Brain:
				unit.brain = (BrainEquipmentData)_component;
				break;
			case EquipmentType.NeuronalMembrane:
				unit.membrane = (NeuronalMembraneEquipmentData)_component;
				break;
			case EquipmentType.Armor:
			case EquipmentType.Occultor:
				unit.auxiliary.Add(_component);
				break;
			case EquipmentType.Chipset:
				unit.chipsets.Add((ChipsetEquipmentData)_component);
				break;
		}

		ComponentTestLabStation station = new() { id = _component.name, label = GetLabel(_component), component = _component };
		station.testedActions.AddRange(grantedActions);

		List<EntityActionData> rangeActions = grantedActions.Select(GetActionData).Where(action => action != null).ToList();
		if (!isArm && !rangeActions.Any(IsAttack) && DefaultWeapon != null)
		{
			station.hasDefaultWeapon = true;
			unit.arms.Add(DefaultWeapon);
			rangeActions.AddRange(GetActions(DefaultWeapon));
		}

		if (unit.brain == null)
			unit.brain = PickBrain(rangeActions);

		if (!unit.IsValid)
		{
			Skipped.Add(GetLabel(_component) + " (" + _component.name + ") : unité incomplète, pas de banc.");
			return null;
		}

		station.unit = unit;
		station.dummy = m_dummySpec;
		m_requirements.Add(station, BuildRequirements(station, rangeActions, _component));
		return station;
	}

	private static Requirements BuildRequirements ( ComponentTestLabStation _station, List<EntityActionData> _actions, EntityEquipmentData _component )
	{
		Requirements requirements = new();
		foreach (EntityActionData action in _actions)
		{
			if (TryGetInterval(action, out Interval interval))
			{
				if (interval.min > interval.max)
					_station.warnings.Add(GetLabel(action) + " : portée hors du banc.");
				else
					requirements.intervals.Add(interval);
			}

			if ((action.aoeType != AOEType.Noone && action.aoeMaxEffectRange > 0) || action.maxTargetAmount > 1)
				requirements.needsArea = true;
			if (action.aoeType == AOEType.Ray && action.aoECenterType == EntityActionData.AOECenterType.Self)
				requirements.rayRange = Mathf.Max(requirements.rayRange, action.aoeMaxEffectRange);
		}

		if (_component.passiveEffects != null)
		{
			foreach (AEntityPassiveEffect.PassiveEffectContainer container in _component.passiveEffects)
			{
				if (GameAssets.current.game.entityEffects.TryGetValue(container.enumID, out AEntityPassiveEffect effect)
					&& effect is DamageOnDeathPassiveEffect deathEffect && deathEffect.explosionRange >= 1)
					requirements.intervals.Add(new Interval(1, Mathf.Min(deathEffect.explosionRange, c_maxDistance)));
			}
		}

		return requirements;
	}

	private static bool TryGetInterval ( EntityActionData _action, out Interval _interval )
	{
		_interval = default;
		if (_action.isModAction || _action.GetMainActionType() == EntityActionData.MainActionType.Movement)
			return false;

		switch (_action.codeType)
		{
			case EntityActionData.ActionCodeType.TurnShield:
			case EntityActionData.ActionCodeType.TurnEntity:
			case EntityActionData.ActionCodeType.Wait:
				return false;
			case EntityActionData.ActionCodeType.InvokeItem:
			case EntityActionData.ActionCodeType.MoveItem:
			case EntityActionData.ActionCodeType.InvokeEntity:
				_interval = Clamp(Mathf.Max(1, _action.minDistance) + 1, _action.maxDistance + 1);
				return true;
		}

		if (_action.targetType == EntityActionData.TargetType.Self)
		{
			if (_action.aoeType == AOEType.Noone || _action.aoeMaxEffectRange <= 0)
				return false;

			_interval = Clamp(_action.aoeMinEffectRange, _action.aoeMaxEffectRange);
			return true;
		}

		int min = _action.minDistance;
		int max = _action.maxDistance;
		if (_action.aoeType != AOEType.Noone && _action.aoECenterType == EntityActionData.AOECenterType.Self && _action.aoeMaxEffectRange > 0)
		{
			min = Mathf.Max(min, _action.aoeMinEffectRange);
			max = Mathf.Min(max, _action.aoeMaxEffectRange);
		}

		_interval = Clamp(min, max);
		return true;
	}

	private static Interval Clamp ( int _min, int _max )
	{
		return new Interval(Mathf.Max(1, _min), Mathf.Min(_max, c_maxDistance));
	}

	#endregion

	#region Placement

	private void Place ( ComponentTestLabConfig _config )
	{
		int count = Stations.Count;
		int columns = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(count * (float)c_blockHeight / c_blockWidth)), 1, c_maxColumns);
		int rows = Mathf.CeilToInt(count / (float)columns);
		Width = 2 + columns * c_blockWidth;
		Height = 2 + rows * c_blockHeight;
		GroundTypes = new TileGroundType[Width * Height];

		for (int i = 0; i < count; i++)
		{
			ComponentTestLabStation station = Stations[i];
			Requirements requirements = m_requirements[station];
			RectInt block = new(1 + (i % columns) * c_blockWidth, 1 + (i / columns) * c_blockHeight, c_blockWidth, c_blockHeight);
			Vector2Int unitCell = new(block.x + c_unitColumn, block.y + c_laneRow);
			int distance = station.isBench ? Mathf.Clamp(_config.benchDistance, 1, c_maxDistance) : PickDistance(requirements.intervals);
			Vector2Int targetCell = new(unitCell.x + distance, unitCell.y);
			HashSet<Vector2Int> used = new() { unitCell, targetCell };
			List<Vector2Int> dummyCells = new() { targetCell };

			station.distance = distance;
			station.unitTile = ToIndex(unitCell);
			if (distance >= 2)
			{
				Vector2Int coverCell = new(targetCell.x - 1, targetCell.y);
				used.Add(coverCell);
				station.coverTile = ToIndex(coverCell);
			}

			if (requirements.needsArea)
			{
				foreach (int rowOffset in new[] { 1, -1 })
				{
					if (TryFindAreaCell(block, unitCell, targetCell, distance, rowOffset, used, out Vector2Int areaCell))
						AddDummy(areaCell);
				}
			}

			if (requirements.rayRange > distance)
			{
				Vector2Int behindCell = new(targetCell.x + 1, targetCell.y);
				if (block.Contains(behindCell) && !used.Contains(behindCell))
					AddDummy(behindCell);
			}

			IEnumerable<int> missedDistances = requirements.intervals.Where(interval => !interval.Contains(distance))
				.Select(interval => interval.Preferred).Distinct().Take(c_maxRangeDummies);
			foreach (int missedDistance in missedDistances)
			{
				if (TryFindRangeCell(block, unitCell, missedDistance, used, out Vector2Int rangeCell))
					AddDummy(rangeCell);
				else
					station.warnings.Add("Pas de place pour une cible à " + missedDistance + " cases.");
			}

			GroundTypes[station.unitTile] = TileGroundType.StaticPlayerSpawn;
			foreach (Vector2Int cell in dummyCells)
			{
				int tile = ToIndex(cell);
				station.dummyTiles.Add(tile);
				GroundTypes[tile] = TileGroundType.StaticEnemySpawn;
			}

			void AddDummy ( Vector2Int _cell )
			{
				used.Add(_cell);
				dummyCells.Add(_cell);
			}
		}
	}

	private static int PickDistance ( List<Interval> _intervals )
	{
		int bestCount = 0;
		int start = -1;
		int end = -1;
		for (int distance = 1; distance <= c_maxDistance; distance++)
		{
			int count = _intervals.Count(interval => interval.Contains(distance));
			if (count > bestCount)
			{
				bestCount = count;
				start = distance;
				end = distance;
			}
			else if (count == bestCount && bestCount > 0 && distance == end + 1)
				end = distance;
		}

		if (bestCount == 0)
			return c_defaultDistance;

		return start < end ? start + 1 : start;
	}

	private static bool TryFindAreaCell ( RectInt _block, Vector2Int _unit, Vector2Int _target, int _distance, int _rowOffset, HashSet<Vector2Int> _used, out Vector2Int _cell )
	{
		_cell = default;
		int bestScore = int.MaxValue;
		for (int x = _block.xMin; x < _block.xMax; x++)
		{
			Vector2Int candidate = new(x, _target.y + _rowOffset);
			if (!_block.Contains(candidate) || _used.Contains(candidate) || GetDistance(candidate, _target) != 1)
				continue;

			int score = Mathf.Abs(GetDistance(candidate, _unit) - _distance) * 100 - x;
			if (score < bestScore)
			{
				bestScore = score;
				_cell = candidate;
			}
		}

		return bestScore != int.MaxValue;
	}

	private static bool TryFindRangeCell ( RectInt _block, Vector2Int _unit, int _distance, HashSet<Vector2Int> _used, out Vector2Int _cell )
	{
		foreach (int rowOffset in new[] { 2, -2, 1, -1 })
		{
			for (int x = _block.xMin + 1; x < _block.xMax - 1; x++)
			{
				Vector2Int candidate = new(x, _unit.y + rowOffset);
				if (_block.Contains(candidate) && !_used.Contains(candidate) && GetDistance(candidate, _unit) == _distance)
				{
					_cell = candidate;
					return true;
				}
			}
		}

		_cell = default;
		return false;
	}

	public int ToIndex ( Vector2Int _cell ) => _cell.y * Width + _cell.x;

	public static int GetDistance ( Vector2Int _a, Vector2Int _b )
	{
		int deltaX = (_a.x - _a.y / 2) - (_b.x - _b.y / 2);
		int deltaZ = _a.y - _b.y;
		int deltaY = -deltaX - deltaZ;
		return (Mathf.Abs(deltaX) + Mathf.Abs(deltaY) + Mathf.Abs(deltaZ)) / 2;
	}

	#endregion

	#region Picks

	private static FrameEquipmentData PickBaseFrame ( List<EntityEquipmentData> _components )
	{
		return _components.OfType<FrameEquipmentData>()
			.Where(frame => frame.prefab != null && !frame.isImmortal && GetActions(frame).Any(IsMovement))
			.OrderBy(frame => frame.faction == EntityEquipmentData.EntityFaction.Starting ? 0 : 1)
			.ThenBy(frame => HasPassiveEffects(frame) ? 1 : 0)
			.ThenBy(frame => frame.name, StringComparer.Ordinal)
			.FirstOrDefault();
	}

	private static ReactorEquipmentData PickBaseReactor ( List<EntityEquipmentData> _components )
	{
		return _components.OfType<ReactorEquipmentData>()
			.OrderBy(reactor => reactor.faction == EntityEquipmentData.EntityFaction.Starting ? 0 : 1)
			.ThenByDescending(reactor => reactor.energyProduced)
			.ThenBy(reactor => reactor.name, StringComparer.Ordinal)
			.FirstOrDefault();
	}

	private static NeuronalMembraneEquipmentData PickBaseMembrane ( List<EntityEquipmentData> _components )
	{
		return _components.OfType<NeuronalMembraneEquipmentData>()
			.OrderBy(membrane => membrane.visionType == NeuronalMembraneEquipmentData.VisionTypes.Optic ? 0 : 1)
			.ThenBy(membrane => membrane.faction == EntityEquipmentData.EntityFaction.Starting ? 0 : 1)
			.ThenBy(membrane => membrane.name, StringComparer.Ordinal)
			.FirstOrDefault();
	}

	private static WeaponEquipmentData PickDefaultWeapon ( List<EntityEquipmentData> _components )
	{
		List<WeaponEquipmentData> weapons = _components.OfType<WeaponEquipmentData>().Where(weapon => weapon.prefab != null).ToList();
		WeaponEquipmentData weaponWithSimpleShot = weapons
			.Where(weapon => GetActions(weapon).Any(IsSimpleShot))
			.OrderBy(weapon => weapon.faction == EntityEquipmentData.EntityFaction.Starting ? 0 : 1)
			.ThenBy(weapon => weapon.name, StringComparer.Ordinal)
			.FirstOrDefault();

		return weaponWithSimpleShot != null ? weaponWithSimpleShot : weapons.Where(weapon => GetActions(weapon).Any(IsAttack)).OrderBy(weapon => weapon.name, StringComparer.Ordinal).FirstOrDefault();
	}

	private static UnitPreset PickDummyPreset ()
	{
		return AssetDatabase.FindAssets("t:" + nameof(UnitPreset))
			.Select(guid => AssetDatabase.LoadAssetAtPath<UnitPreset>(AssetDatabase.GUIDToAssetPath(guid)))
			.Where(preset => preset != null && preset.frame != null && preset.frame.prefab != null && preset.reactor != null && preset.brain != null && preset.neuronalMembrane != null
				&& preset.frame.faction == EntityEquipmentData.EntityFaction.Dummy && !preset.frame.isImmortal
				&& (preset.arms == null || preset.arms.Length == 0))
			.OrderBy(preset => preset.aiRole == EnnemiAIRole.Immobile ? 0 : 1)
			.ThenBy(preset => preset.auxiliary == null ? 0 : preset.auxiliary.Length)
			.ThenBy(preset => preset.name, StringComparer.Ordinal)
			.FirstOrDefault();
	}

	private ComponentTestLabUnitSpec CreateDummySpec ()
	{
		if (DummyPreset == null)
			return new ComponentTestLabUnitSpec { frame = BaseFrame, reactor = BaseReactor, brain = Or(m_fixedBrain, m_brains.FirstOrDefault()), membrane = BaseMembrane };

		return new ComponentTestLabUnitSpec
		{
			frame = DummyPreset.frame,
			reactor = DummyPreset.reactor,
			brain = DummyPreset.brain,
			membrane = DummyPreset.neuronalMembrane,
			arms = NonNull(DummyPreset.arms),
			auxiliary = NonNull(DummyPreset.auxiliary),
			chipsets = NonNull(DummyPreset.chipsets),
		};
	}

	private BrainEquipmentData PickBrain ( IEnumerable<EntityActionData> _actions )
	{
		if (m_fixedBrain != null)
			return m_fixedBrain;

		List<EntityActionData> actions = _actions.ToList();
		bool isMelee = actions.Count(action => action.type == ActionType.MeleeAttack) > actions.Count(action => action.type == ActionType.DistanceAttack);
		return m_brains
			.OrderByDescending(brain => isMelee ? brain.agility : brain.distanceAccuracy)
			.ThenBy(brain => brain.name, StringComparer.Ordinal)
			.FirstOrDefault();
	}

	#endregion

	#region Helpers

	public static IEnumerable<EntityActionEnumID> GetActionIDs ( EntityEquipmentData _component )
	{
		if (_component == null || _component.knownedActions == null)
			yield break;

		foreach (EntityActionEnumID id in _component.knownedActions)
		{
			if (id != EntityActionEnumID.Unknowned && GameAssets.current.game.entityActionsData.ContainsKey(id))
				yield return id;
		}
	}

	public static EntityActionData GetActionData ( EntityActionEnumID _id )
	{
		return GameAssets.current.game.entityActionsData.TryGetValue(_id, out EntityActionData data) ? data : null;
	}

	private static IEnumerable<EntityActionData> GetActions ( EntityEquipmentData _component )
	{
		return GetActionIDs(_component).Select(GetActionData).Where(data => data != null);
	}

	private static bool IsMovement ( EntityActionData _action )
	{
		return !_action.isModAction && _action.codeType != EntityActionData.ActionCodeType.Wait
			&& _action.GetMainActionType() == EntityActionData.MainActionType.Movement;
	}

	private static bool IsAttack ( EntityActionData _action )
	{
		return !_action.isModAction && _action.GetMainActionType() == EntityActionData.MainActionType.Attack;
	}

	private static bool IsSimpleShot ( EntityActionData _action )
	{
		return !_action.isModAction && _action.type == ActionType.DistanceAttack && _action.targetType == EntityActionData.TargetType.OtherEntity
			&& _action.aoeType == AOEType.Noone && _action.conditionType == Condition.ConditionType.Noone;
	}

	public static bool HasPassiveEffects ( EntityEquipmentData _component )
	{
		return _component.passiveEffects != null && _component.passiveEffects.Any(effect => effect.enumID != EntityPassiveEffectEnumID.Unknown);
	}

	public static string GetLabel ( EntityEquipmentData _component )
	{
		return string.IsNullOrWhiteSpace(_component.displayName) || _component.displayName == "default" ? _component.name : _component.displayName;
	}

	public static string GetLabel ( EntityActionData _action )
	{
		return string.IsNullOrWhiteSpace(_action.displayName) ? _action.name : _action.displayName;
	}

	public static int GetTypeOrder ( EquipmentType _type )
	{
		return _type switch
		{
			EquipmentType.Weapon => 0,
			EquipmentType.Tool => 1,
			EquipmentType.Frame => 2,
			EquipmentType.Reactor => 3,
			EquipmentType.Brain => 4,
			EquipmentType.NeuronalMembrane => 5,
			EquipmentType.Armor => 6,
			EquipmentType.Occultor => 7,
			EquipmentType.Chipset => 8,
			_ => 9,
		};
	}

	public static string GetGroupName ( EquipmentType _type )
	{
		return _type switch
		{
			EquipmentType.Weapon => "Armes",
			EquipmentType.Tool => "Outils",
			EquipmentType.Frame => "Châssis",
			EquipmentType.Reactor => "Réacteurs",
			EquipmentType.Brain => "Cerveaux",
			EquipmentType.NeuronalMembrane => "Membranes",
			EquipmentType.Armor => "Blindages",
			EquipmentType.Occultor => "Occulteurs",
			EquipmentType.Chipset => "Chipsets",
			_ => "Autres",
		};
	}

	private static List<T> NonNull<T> ( IEnumerable<T> _items ) where T : UnityEngine.Object
	{
		return _items == null ? new List<T>() : _items.Where(item => item != null).ToList();
	}

	private static T Or<T> ( T _value, T _fallback ) where T : UnityEngine.Object
	{
		return _value != null ? _value : _fallback;
	}

	#endregion
}
