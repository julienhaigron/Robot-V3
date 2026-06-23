using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "MissionData", menuName = "ScriptableObject/MissionData")]
public class MissionData : ScriptableEnum<MissionDataEnumID>
{
    public string missionName;
    public Sprite icon;

    public DialogueData preMissionDialogue;
    public DialogueData afterMissionDialogue;
    public LevelData levelMission;
    public MissionType type;
    public enum MissionType
	{
        MME,
        CapturePosition,
        DestroyItem
	}

    public bool areRewardsRandom = false;
    public SerializableDictionary<CurrencyType, ulong> currencyRewards;
    public List<EntityEquipmentData> equipmentRewards;

	protected override void OnValidate ()
	{
		base.OnValidate();

#if UNITY_EDITOR
        if (!GameAssets.current.game.missions.ContainsKey(enumID))
            GameAssets.current.game.missions.Add(enumID, this);
        EditorUtility.SetDirty(GameAssets.current);
#endif

    }

    public string GetDescription ()
	{
        return "Description";
	}
}
