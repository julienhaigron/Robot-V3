using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public enum MatchStrategy
{
	Rush,
	Encerclement,
	Isolation,
	Groupement,
}

[Serializable]
public enum PlaythroughPhase
{
	Early,
	Mid,
	Late,
}

[Serializable]
public class MapList
{
	public List<GridData> maps = new();
}

[Serializable]
public class UnitPresetList
{
	public List<UnitPreset> presets = new();
}

[Serializable]
public class ChallengerData
{
	public string id;
	public MissionData.MissionType type;
	public MatchStrategy strategy;
	public List<UnitPreset> team = new();

	public string NameKey => "challenger/" + id;
}

public static class ProceduralMatchGenerator
{
	public static List<GameDatas.ProceduralMatchData> Generate ( int _count, int _cycleCount )
	{
		PlaythroughPhase phase = GetPhase(_cycleCount);
		List<GameDatas.ProceduralMatchData> matches = new();
		for (int i = 0; i < _count; i++)
		{
			GameDatas.ProceduralMatchData match = Generate(phase);
			if (match != null)
				matches.Add(match);
		}

		return matches;
	}

	public static GameDatas.ProceduralMatchData Generate ( PlaythroughPhase _phase )
	{
		List<MissionData.MissionType> types = GameAssets.current.game.mapsPerMissionType
			.Where(pair => pair.Value != null && pair.Value.maps.Any(map => map != null))
			.Select(pair => pair.Key).ToList();
		if (types.Count == 0)
		{
			Debug.LogError("No map in GameAssets.game.mapsPerMissionType: no procedural match can be generated.");
			return null;
		}

		GameDatas.ProceduralMatchData match = new();
		match.phase = _phase;
		match.type = types[UnityEngine.Random.Range(0, types.Count)];

		List<GridData> maps = GameAssets.current.game.mapsPerMissionType[match.type].maps.Where(map => map != null).ToList();
		match.mapName = maps[UnityEngine.Random.Range(0, maps.Count)].name;

		Array strategies = Enum.GetValues(typeof(MatchStrategy));
		match.strategy = (MatchStrategy)strategies.GetValue(UnityEngine.Random.Range(0, strategies.Length));
		match.generatedEnemies = EnemyUnitGenerator.GenerateSquad(_phase);
		if (match.generatedEnemies.Count == 0)
		{
			Debug.LogWarning("No faction has a complete set of droppable components: the enemy squad falls back to the " + _phase + " presets.");
			match.enemyPresetNames = GeneratePresetSquad(_phase, match.strategy).Select(preset => preset.name).ToList();
		}

		MissionData.RewardSet rewardSet = MissionData.GenerateRewardSet(MissionData.RollRewardTemplate());
		match.rewardCredits = new List<ulong>(rewardSet.creditAmounts);
		match.rewardComponentNames = rewardSet.components.Where(component => component != null).Select(component => component.name).ToList();

		return match;
	}

	public static List<UnitPreset> GeneratePresetSquad ( PlaythroughPhase _phase, MatchStrategy _strategy )
	{
		List<UnitPreset> squad = new();
		GameConfig.EnemySquadRules rules = GetSquadRules(_phase);
		if (rules == null || !GameAssets.current.game.enemyPresetsPerPhase.TryGetValue(_phase, out UnitPresetList presetList))
		{
			Debug.LogError("Missing enemy squad rules or enemy presets for the " + _phase + " phase.");
			return squad;
		}

		List<UnitPreset> pool = presetList.presets.Where(preset => preset != null).ToList();
		Archetype[] preferredArchetypes = GetPreferredArchetypes(_strategy);
		int remainingEnergy = rules.maxEnergy;

		while (squad.Count < rules.maxUnitCount)
		{
			List<UnitPreset> affordable = pool.Where(preset => preset.GetEnergyCost() <= remainingEnergy).ToList();
			if (affordable.Count == 0)
				break;

			List<UnitPreset> preferred = affordable.Where(preset => preferredArchetypes.Contains(preset.archetype)).ToList();
			List<UnitPreset> candidates = preferred.Count > 0 ? preferred : affordable;
			UnitPreset pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
			squad.Add(pick);
			remainingEnergy -= pick.GetEnergyCost();
		}

		if (squad.Count == 0 && pool.Count > 0)
			squad.Add(pool.OrderBy(preset => preset.GetEnergyCost()).First());

		return squad;
	}

	public static PlaythroughPhase GetPhase ( int _cycleCount )
	{
		PlaythroughPhase phase = PlaythroughPhase.Early;
		int bestStartCycle = int.MinValue;
		foreach (GameConfig.EnemySquadRules rules in GameConfig.current.game.enemySquadRules ?? Array.Empty<GameConfig.EnemySquadRules>())
		{
			if (rules.startCycle <= _cycleCount && rules.startCycle >= bestStartCycle)
			{
				phase = rules.phase;
				bestStartCycle = rules.startCycle;
			}
		}

		return phase;
	}

	public static GameConfig.EnemySquadRules GetSquadRules ( PlaythroughPhase _phase )
	{
		return GameConfig.current.game.enemySquadRules?.FirstOrDefault(rules => rules.phase == _phase);
	}

	public static Archetype[] GetPreferredArchetypes ( MatchStrategy _strategy )
	{
		GameConfig.StrategyArchetypes strategyArchetypes = GameConfig.current.game.strategyArchetypes?.FirstOrDefault(entry => entry.strategy == _strategy);
		return strategyArchetypes?.archetypes ?? Array.Empty<Archetype>();
	}

	public static GridData FindMap ( string _mapName )
	{
		foreach (MapList mapList in GameAssets.current.game.mapsPerMissionType.Values)
		{
			GridData map = mapList?.maps.FirstOrDefault(candidate => candidate != null && candidate.name == _mapName);
			if (map != null)
				return map;
		}

		return null;
	}

	public static UnitPreset FindEnemyPreset ( string _presetName )
	{
		foreach (UnitPresetList presetList in GameAssets.current.game.enemyPresetsPerPhase.Values)
		{
			UnitPreset preset = presetList?.presets.FirstOrDefault(candidate => candidate != null && candidate.name == _presetName);
			if (preset != null)
				return preset;
		}

		foreach (ChallengerData challenger in GameAssets.current.game.challengers)
		{
			UnitPreset preset = challenger?.team.FirstOrDefault(candidate => candidate != null && candidate.name == _presetName);
			if (preset != null)
				return preset;
		}

		return null;
	}

	public static List<GridData> GetMaps ( MissionData.MissionType _type )
	{
		return GameAssets.current.game.mapsPerMissionType.TryGetValue(_type, out MapList mapList) && mapList != null
			? mapList.maps.Where(map => map != null).ToList()
			: new List<GridData>();
	}

	public static ChallengerData RollChallenger ( int _cycleCount )
	{
		int interval = GameConfig.current.game.challengerTournamentInterval;
		if (interval <= 0 || _cycleCount <= 0 || _cycleCount % interval != 0)
			return null;

		List<string> beatenChallengers = GameDatas.current.currentPlayerSave.beatenChallengers;
		List<ChallengerData> pool = GameAssets.current.game.challengers
			.Where(challenger => challenger != null && !string.IsNullOrEmpty(challenger.id) && !beatenChallengers.Contains(challenger.id)
				&& challenger.team.Any(preset => preset != null) && GetMaps(challenger.type).Count > 0)
			.ToList();

		return pool.Count == 0 ? null : pool[UnityEngine.Random.Range(0, pool.Count)];
	}

	public static GameDatas.ProceduralMatchData GenerateChallengerMatch ( ChallengerData _challenger, int _cycleCount )
	{
		List<GridData> maps = GetMaps(_challenger.type);
		if (maps.Count == 0)
			return null;

		GameDatas.ProceduralMatchData match = new();
		match.phase = GetPhase(_cycleCount);
		match.type = _challenger.type;
		match.strategy = _challenger.strategy;
		match.mapName = maps[UnityEngine.Random.Range(0, maps.Count)].name;
		match.challengerId = _challenger.id;
		match.enemyPresetNames = _challenger.team.Where(preset => preset != null).Select(preset => preset.name).ToList();
		return match;
	}

	public static ChallengerData FindChallenger ( string _id )
	{
		return string.IsNullOrEmpty(_id) ? null : GameAssets.current.game.challengers.FirstOrDefault(challenger => challenger != null && challenger.id == _id);
	}

	public static void RollTournamentReward ( List<ulong> _credits, List<string> _blueprints )
	{
		_credits.Clear();
		_blueprints.Clear();

		MissionData.RewardTemplate template = MissionData.RollRewardTemplate();
		for (int i = 0; i < template.creditCount; i++)
			_credits.Add(MissionData.RollCreditAmount());

		AddBlueprints(template.mainComponentCount, true, _credits, _blueprints);
		AddBlueprints(template.secondaryComponentCount, false, _credits, _blueprints);
	}

	private static void AddBlueprints ( int _count, bool _isMain, List<ulong> _credits, List<string> _blueprints )
	{
		for (int i = 0; i < _count; i++)
		{
			List<EntityEquipmentData> pool = GetLockedBlueprints(_isMain).Where(data => !_blueprints.Contains(data.name)).ToList();
			if (pool.Count > 0)
			{
				_blueprints.Add(pool[UnityEngine.Random.Range(0, pool.Count)].name);
				continue;
			}

			int creditCount = (_isMain ? MissionData.mainComponentPointValue : MissionData.secondaryComponentPointValue) / MissionData.creditPointValue;
			for (int j = 0; j < creditCount; j++)
				_credits.Add(MissionData.RollCreditAmount());
		}
	}

	public static List<EntityEquipmentData> GetLockedBlueprints ( bool _isMain )
	{
		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;
		return GameAssets.current.equipments.Values
			.Where(data => data != null && data.canDropRandomly && data.requiresBlueprint && !save.IsBlueprintUnlocked(data.name) && !save.OwnsBlueprint(data.name)
				&& data.IsMainComponent() == _isMain)
			.ToList();
	}

	public static UnitPreset[] GetAllies ( MissionData.MissionType _type )
	{
		return GameAssets.current.game.alliesPerMissionType.TryGetValue(_type, out UnitPresetList allies) && allies != null
			? allies.presets.Where(preset => preset != null).ToArray()
			: Array.Empty<UnitPreset>();
	}
}
