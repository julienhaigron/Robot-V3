using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "IncrementalUpgrade", menuName = "ScriptableObject/IncrementalUpgrade")]
public class IncrementalUpgrade : UpgradeAsset
{
    /*[TitleGroup("Price")]
    [SerializeField]
    protected ulong m_incrementalPrice;
    [TitleGroup("Value")]
    [SerializeField]
    protected float m_incrementalValue;*/
    
    //public override ulong GetPrice(int level)
    //{
    //    return m_basePrice + m_incrementalPrice * (ulong)level;
    //}
    //
    //public override float GetValue(int level)
    //{
    //    return m_baseValue + m_incrementalValue * level;
    //}
}
