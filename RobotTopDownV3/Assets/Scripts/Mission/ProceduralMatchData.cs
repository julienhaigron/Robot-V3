using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameDatas
{
	[Serializable]
	public class ProceduralMatchData
	{
		public MissionData.MissionType type;
		public MatchStrategy strategy;
		public PlaythroughPhase phase;
		public string mapName;
		public List<string> enemyPresetNames = new();
		public List<ulong> rewardCredits = new();
		public List<string> rewardComponentNames = new();
		public string challengerId;

		[NonSerialized] private MissionData m_missionData;

		public void ClearRewards ()
		{
			rewardCredits.Clear();
			rewardComponentNames.Clear();
			m_missionData = null;
		}

		public MissionData GetMissionData ()
		{
			if (m_missionData != null)
				return m_missionData;

			MissionData missionData = ScriptableObject.CreateInstance<MissionData>();
			missionData.name = "ProceduralMatch_" + type;
			missionData.type = type;
			missionData.map = ProceduralMatchGenerator.FindMap(mapName);
			missionData.missionName = type + "_" + mapName;
			missionData.enemies = enemyPresetNames.Select(ProceduralMatchGenerator.FindEnemyPreset).Where(preset => preset != null).ToArray();
			missionData.allies = ProceduralMatchGenerator.GetAllies(type);
			missionData.currencyRewards = Array.Empty<MissionData.CurrencyReward>();
			missionData.equipmentRewards = new();
			missionData.unitReward = new();
			missionData.SetFixedRewardSet(BuildRewardSet());
			missionData.SetTeamStrategy(strategy);

			if (missionData.map == null)
				Debug.LogError("Procedural match map " + mapName + " is no longer in GameAssets.game.mapsPerMissionType.");
			if (missionData.enemies.Length < enemyPresetNames.Count)
				Debug.LogWarning("Some enemy presets of a procedural match are no longer in GameAssets.game.enemyPresetsPerPhase.");

			m_missionData = missionData;
			return missionData;
		}

		private MissionData.RewardSet BuildRewardSet ()
		{
			MissionData.RewardSet rewardSet = new();
			rewardSet.creditAmounts.AddRange(rewardCredits);
			foreach (string componentName in rewardComponentNames)
			{
				if (GameAssets.current.equipments.TryGetValue(componentName, out EntityEquipmentData component))
					rewardSet.components.Add(component);
			}

			return rewardSet;
		}
	}
}
