using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "MissionData", menuName = "ScriptableObject/MissionData")]
public class MissionData : ScriptableEnum<MissionDataEnumID>
{
    public string missionName;
    public Sprite icon;

    public DialogueData preMissionDialogue;
    public DialogueData afterMissionDialogue;

    public GridData map;
    public UnitPreset[] enemies;
    public MissionType type;
    public enum MissionType
	{
        Extermination,
        DefenseDeZone,
        ControleDePoint,
        Construction,
        Sabotage,
        Assassinat,
	}

    [ShowIf("@type == MissionType.Construction || type == MissionType.Sabotage ")]
    public UnitPreset[] allies;

    [ShowIf("@type == MissionType.Assassinat ")]
    public UnitPreset kingUnit;

    //public bool areRewardsRandom = false;
    public CurrencyReward[] currencyRewards;

    [System.Serializable]
    public class CurrencyReward
	{
        public CurrencyType type;
        public ulong amount;
    }

    public List<EntityEquipmentData> equipmentRewards;
    public List<UnitPreset> unitReward;

	protected override void OnValidate ()
	{
		base.OnValidate();

#if UNITY_EDITOR
        if (!GameAssets.current.game.missions.ContainsKey(enumID))
            GameAssets.current.game.missions.Add(enumID, this);
        EditorUtility.SetDirty(GameAssets.current);
#endif

    }

    public void GiveAllRewards ()
	{
        foreach(CurrencyReward reward in currencyRewards)
            GameDatas.current.currentPlayerSave.AddCurrency(reward.type, reward.amount);

        foreach (EntityEquipmentData reward in equipmentRewards)
            GameDatas.current.currentPlayerSave.AddComponentToInventory(reward);

        foreach (UnitPreset reward in unitReward)
            reward.AddToUnits(false);
    }

    public string GetDescription ()
	{
        return "Description";
	}
}
