using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrades/Basic Upgrade")]
public class UpgradeAsset : ScriptableObject
{
    //public static MissionEvent onUpgradeAssetUpgradedMissionEvent = new(ContextType.UpgradeAsset);

    [SerializeField]
    [TitleGroup("Save")]
    [DisableIf("@this.IsInGameAssets()")]
    [InfoBox("Can't change save key once it was added to GameAssets. (Renaming save key could mess up existing save!)", InfoMessageType.Info, "@IsInGameAssets()")]
    [InfoBox("Upgrade is missing in GameAssets!", InfoMessageType.Warning, "@!this.IsInGameAssets() && this.IsKeyValid()")]
    [InfoBox("Another upgrade with same key is referenced in GameAssets", InfoMessageType.Error, "@this.AnotherAssetHasSameKey()")]
    [InfoBox("Invalid key", InfoMessageType.Error, "@!this.IsKeyValid()")]
    protected string m_saveKey;
    public string saveKey => m_saveKey;

    [TitleGroup("Display")]
    [SerializeField]
    protected string m_displayName;
    public string displayName => m_displayName;
    [SerializeField]
    protected string m_suffix;
    public string suffix => m_suffix;
    [SerializeField]
    protected string m_prefix;
    public string prefix => m_prefix;
    [SerializeField]
    protected bool m_useEngineeringNotation;
    public bool useEngineeringNotation => m_useEngineeringNotation;

    [HorizontalGroup("Icon", .5f), LabelWidth(0), BoxGroup("Icon/Sprite")]
    [HideLabel]
    [PreviewField(ObjectFieldAlignment.Center, Height = 100f)]
    [SerializeField]
    protected Sprite m_sprite;
    public Sprite sprite => m_sprite;

    [HorizontalGroup("Icon", .5f), LabelWidth(0), BoxGroup("Icon/Outline")]
    [HideLabel]
    [PreviewField(ObjectFieldAlignment.Center, Height = 100f)]
    [SerializeField]
    protected Sprite m_outlineSprite;
    public Sprite outlineSprite => m_outlineSprite;


    [TitleGroup("Value")]
    [SerializeField]
    protected float m_baseValue;
    [SerializeField]
    protected float m_displayedValueOffset = 0;

    [TitleGroup("Price")]
    [SerializeField]
    protected ulong[] m_price;
    [SerializeField]
    protected CurrencyType m_currencyType;
    public CurrencyType currencyType => m_currencyType;


    [TitleGroup("Level")]
    [SerializeField] protected bool m_isInfinite = false;
    [ShowIf("@m_isInfinite == false")]
    [Min(0), SerializeField] protected int m_maxLevel = 0;
    public bool IsInfinite => m_isInfinite;
    public int maxLevel => m_maxLevel;

    public Action onUpgradeDone;
    public Action onMaxedOut;

    [ShowIf("@this.AppIsPlaying() && this.IsInGameAssets()")]
    [Button("Upgrade")]
    public virtual void Upgrade ( bool removeMoney = true )
    {
        GameDatas.current.upgradeLevels[m_saveKey]++;
        if (removeMoney)
        {
            GameDatas.current.RemoveCurrency(m_currencyType, GetCurrentPrice(), GameDatas.CurrencyEvent.Meta, "Upgrade" + m_displayName + GetCurrentLevel());
        }
       /* AnalyticsManager.SendEvent("upgrade", new Dictionary<string, object>()
            {
                { "name", m_displayName },
                { "level", GetCurrentLevel() },
                { "upgrade_category", (this is StructureUpgrade ? "building" : "other") }
            });*/
        onUpgradeDone?.Invoke();
        //onUpgradeAssetUpgradedMissionEvent?.Invoke(new UpgradeAssetContext() { amount = 1, upgrade = this });

        if (IsMaxedOut())
            onMaxedOut?.Invoke();
    }

    #region Methods using datas
    public virtual ulong GetCurrentPrice ()
    {
        return GetPrice(GetCurrentLevel());
    }

    public virtual float GetCurrentValue ()
    {
        return GetValue(GetCurrentLevel());
    }

    public virtual string GetCurrentDisplayedValue ()
    {
        float valueToDisplay = GetCurrentValue() - m_displayedValueOffset;

        if (m_useEngineeringNotation)
            return m_prefix + MathHelper.ConvertToEngineeringNotation(valueToDisplay, 3) + m_suffix;
        else
            return m_prefix + valueToDisplay.ToString() + m_suffix;
    }

    public virtual string GetNextDisplayedValue ()
    {
        float valueToDisplay = GetValue(GetCurrentLevel() + 1) - m_displayedValueOffset;

        if (m_useEngineeringNotation)
            return m_prefix + MathHelper.ConvertToEngineeringNotation(valueToDisplay, 3) + m_suffix;
        else
            return m_prefix + valueToDisplay.ToString() + m_suffix;
    }

    public virtual int GetCurrentLevel ()
    {
        return GameDatas.current.upgradeLevels[m_saveKey];
    }

    public virtual bool CanUpgrade ()
    {
        return CanUpgrade(GetCurrentLevel(), GameDatas.current.currencies[m_currencyType]);
    }

    public virtual int GetPurchasableUpgradesAmount ()
    {
        return GetPurchasableUpgradesAmount(GetCurrentLevel(), GameDatas.current.currencies[m_currencyType]);
    }
    #endregion

    public virtual ulong GetPrice ( int _level )
    {
        return m_price[Mathf.Min(_level, m_price.Length - 1)];
    }

    public virtual float GetValue ( int level )
    {
        return m_baseValue;
    }

    public virtual bool CanUpgrade ( int level, ulong moneyAmount )
    {
        if (IsMaxedOut(level)) return false;

        return moneyAmount >= GetPrice(level);
    }

    public virtual int GetPurchasableUpgradesAmount ( int fromLevel, ulong moneyAmount )
    {
        int total = 0;
        if (CanUpgrade(fromLevel, moneyAmount))
        {
            total++;
            moneyAmount -= GetPrice(fromLevel);
            fromLevel++;
        }
        return total;
    }

    public virtual bool HasEnoughCurrencyForCurrentUpgrade ()
    {
        return GameDatas.current.currencies[currencyType] >= GetCurrentPrice();
    }

    public virtual bool IsMaxedOut ()
    {
        if (IsInfinite)
            return false;

        return GetCurrentLevel() >= maxLevel;
    }

    public virtual bool IsMaxedOut ( int _levelTarget )
    {
        if (IsInfinite)
            return false;

        return _levelTarget >= maxLevel;
    }

    #region Editor
#if UNITY_EDITOR

    private bool IsInGameAssets ()
    {
        return GameAssets.current.upgrades.Contains(this);
    }

    private bool AppIsPlaying ()
    {
        return Application.isPlaying;
    }

    private bool IsKeyValid ()
    {
        if (m_saveKey == null)
        {
            m_saveKey = "";
        }
        m_saveKey = String.Concat(m_saveKey.Where(c => !Char.IsWhiteSpace(c)));
        m_saveKey = m_saveKey.ToLower();
        EditorUtility.SetDirty(this);
        return !string.IsNullOrWhiteSpace(m_saveKey);
    }

    private bool AnotherAssetHasSameKey ()
    {
        foreach (UpgradeAsset upgrade in GameAssets.current.upgrades)
        {
            if (upgrade != this && upgrade.saveKey == saveKey)
                return true;
        }
        return false;
    }

    [TitleGroup("Save")]
    [ShowIf("@this.IsKeyValid() && !this.IsInGameAssets() && !this.AnotherAssetHasSameKey()")]
    [Button("Add to GameAssets")]
    private void AddToGameAssets ()
    {
        GameAssets.current.upgrades.Add(this);
        if (!GameDatas.current.upgradeLevels.ContainsKey(m_saveKey))
        {
            GameDatas.current.upgradeLevels.Add(m_saveKey, 0);
        }
        string assetPath = AssetDatabase.GetAssetPath(GetInstanceID());
        AssetDatabase.RenameAsset(assetPath, GenerateAssetName());
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(GameAssets.current);
        EditorUtility.SetDirty(GameDatas.current);
        AssetDatabase.SaveAssets();
    }

    private string GenerateAssetName ()
    {
        string generatedName = m_saveKey[0].ToString().ToUpper();
        generatedName += m_saveKey.Substring(1);
        if (string.IsNullOrWhiteSpace(m_displayName))
        {
            m_displayName = generatedName;
        }
        return generatedName + "Upgrade";
    }


    //[TitleGroup("Editor")]
    //[SerializeField, Min(0)]
    //private int m_levelSimulation = 0;
    //[TitleGroup("Editor")]
    //[ReadOnly, SerializeField]
    //private ulong m_priceAtLevel = 0;
    //[TitleGroup("Editor")]
    //[ReadOnly, SerializeField]
    //private float m_valueAtLevel = 0;
    //
    //[OnInspectorGUI]
    //private void SimulatePrice()
    //{
    //    if (!IsInfinite &&  m_levelSimulation > maxLevel)
    //        m_levelSimulation = maxLevel;
    //    m_priceAtLevel = GetPrice(m_levelSimulation);
    //    m_valueAtLevel = GetValue(m_levelSimulation);
    //}
#endif
    #endregion
}
