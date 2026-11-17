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
    [System.NonSerialized] private RewardSet m_rewardSet;
    [System.NonSerialized] private RewardSet m_fixedRewardSet;
    [System.NonSerialized] private bool m_hasTeamStrategy;
    [System.NonSerialized] private MatchStrategy m_teamStrategy;

    public void SetTeamStrategy ( MatchStrategy _strategy )
    {
        m_teamStrategy = _strategy;
        m_hasTeamStrategy = true;
    }

    public bool TryGetTeamStrategy ( out MatchStrategy _strategy )
    {
        _strategy = m_teamStrategy;
        return m_hasTeamStrategy;
    }

	protected override void OnValidate ()
	{
		base.OnValidate();

#if UNITY_EDITOR
        if (!GameAssets.current.game.missions.ContainsKey(enumID))
            GameAssets.current.game.missions.Add(enumID, this);
        EditorUtility.SetDirty(GameAssets.current);
#endif

    }

    #region Rewards

    public const int creditBaseAmount = 25;
    public const float creditMinFactor = 0.8f;
    public const float creditMaxFactor = 1.2f;

    public const int creditPointValue = 1;
    public const int secondaryComponentPointValue = 2;
    public const int mainComponentPointValue = 3;
    public const int unitPointValue = 6;

    public const int drawRewardPoints = 3;
    public const int defeatRewardPoints = 2;

    public const int tournamentFinalLossRewardPoints = 3;
    public const int tournamentSecondMatchLossRewardPoints = 2;

    public class RewardTemplate
    {
        public int creditCount;
        public int secondaryComponentCount;
        public int mainComponentCount;
        public int unitCount;
        public bool isScriptedOnly;
    }

    //The scripted ones never come out of the random roll: they can only be handed out by
    //authoring the reward on a specific mission.
    public static readonly RewardTemplate[] rewardTemplates =
    {
        new() { creditCount = 6 },
        new() { secondaryComponentCount = 1, creditCount = 4 },
        new() { secondaryComponentCount = 2, creditCount = 2 },
        new() { secondaryComponentCount = 3, isScriptedOnly = true },
        new() { mainComponentCount = 1, creditCount = 3 },
        new() { mainComponentCount = 1, secondaryComponentCount = 1, creditCount = 1 },
        new() { mainComponentCount = 2, isScriptedOnly = true },
        new() { secondaryComponentCount = 1, isScriptedOnly = true },
        new() { unitCount = 1, isScriptedOnly = true },
    };

    public class RewardSet
    {
        public List<ulong> creditAmounts = new();
        public List<EntityEquipmentData> components = new();
        public List<EntityEquipmentData> blueprints = new();
        public List<UnitPreset> units = new();

        public int GetTotalPoints ()
        {
            int total = creditAmounts.Count * creditPointValue + units.Count * unitPointValue;
            foreach (EntityEquipmentData component in components)
                total += GetPointValueOf(component);
            foreach (EntityEquipmentData blueprint in blueprints)
                total += GetPointValueOf(blueprint);

            return total;
        }
    }

    public static bool IsMainComponent ( EntityEquipmentData _data )
    {
        return _data.IsMainComponent();
    }

    public static int GetPointValueOf ( EntityEquipmentData _data )
    {
        return IsMainComponent(_data) ? mainComponentPointValue : secondaryComponentPointValue;
    }

    public static ulong RollCreditAmount ()
    {
        return (ulong)Mathf.Max(0, Mathf.RoundToInt(creditBaseAmount * Random.Range(creditMinFactor, creditMaxFactor)));
    }

    public static RewardTemplate RollRewardTemplate ()
    {
        List<RewardTemplate> pool = new();
        foreach (RewardTemplate template in rewardTemplates)
            if (!template.isScriptedOnly)
                pool.Add(template);

        return pool.Count == 0 ? rewardTemplates[0] : pool[Random.Range(0, pool.Count)];
    }

    public static RewardSet GenerateRewardSet ( RewardTemplate _template )
    {
        RewardSet set = new();
        if (_template == null)
            return set;

        //Each credit occurrence rolls on its own and stays a separate reward, so the player
        //picks them one by one when the budget does not cover everything.
        for (int i = 0; i < _template.creditCount; i++)
            set.creditAmounts.Add(RollCreditAmount());

        for (int i = 0; i < _template.mainComponentCount; i++)
            AddRandomComponentTo(set, true);

        for (int i = 0; i < _template.secondaryComponentCount; i++)
            AddRandomComponentTo(set, false);

        return set;
    }

    private static void AddRandomComponentTo ( RewardSet _set, bool _isMain )
    {
        List<EntityEquipmentData> pool = new();
        foreach (EntityEquipmentData data in GameAssets.current.GetRandomlyDroppableEquipments())
            if (IsMainComponent(data) == _isMain)
                pool.Add(data);

        if (pool.Count > 0)
            _set.components.Add(pool[Random.Range(0, pool.Count)]);
    }

    //An authored reward wins over the random roll: that is how the scripted-only templates
    //are handed out.
    public void SetFixedRewardSet ( RewardSet _rewardSet )
    {
        m_fixedRewardSet = _rewardSet;
    }

    public RewardSet GetRewardSet ()
    {
        if (m_fixedRewardSet != null)
            return m_fixedRewardSet;

        if (m_rewardSet != null)
            return m_rewardSet;

        bool hasAuthoredReward = (currencyRewards != null && currencyRewards.Length > 0)
            || (equipmentRewards != null && equipmentRewards.Count > 0)
            || (unitReward != null && unitReward.Count > 0);

        if (hasAuthoredReward)
        {
            m_rewardSet = new RewardSet();

            if (currencyRewards != null)
                foreach (CurrencyReward reward in currencyRewards)
                    m_rewardSet.creditAmounts.Add(reward.amount);

            if (equipmentRewards != null)
                m_rewardSet.components.AddRange(equipmentRewards);

            if (unitReward != null)
                m_rewardSet.units.AddRange(unitReward);
        }
        else
            m_rewardSet = GenerateRewardSet(RollRewardTemplate());

        return m_rewardSet;
    }

    public void ClearRewardSet ()
    {
        m_rewardSet = null;
    }

    #endregion

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
        return type.GetLocalizedTitle();
	}
}
