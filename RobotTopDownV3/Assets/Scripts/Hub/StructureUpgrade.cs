using UnityEngine;

[CreateAssetMenu(fileName = "StructureUpgrade", menuName = "ScriptableObject/StructureUpgrade")]
public abstract class StructureUpgrade : IncrementalUpgrade
{
	public string[] addonDescriptions;

	public abstract float GetAddonValue ( int _level, int _addonID );

	public string GetAddonTitle(int _addonID )
	{
		return addonDescriptions[_addonID];
	}
}
