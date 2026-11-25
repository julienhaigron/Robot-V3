using UnityEngine;

[CreateAssetMenu(fileName = "StructureUpgrade", menuName = "ScriptableObject/StructureUpgrade")]
public abstract class StructureUpgrade : IncrementalUpgrade
{
	protected readonly struct Addon
	{
		public readonly LocalizationKey title;
		public readonly System.Func<int, float> valueAt;
		public readonly bool isRatio;

		public Addon ( LocalizationKey _title, System.Func<int, float> _valueAt, bool _isRatio = false )
		{
			title = _title;
			valueAt = _valueAt;
			isRatio = _isRatio;
		}

		public string Format ( float _value )
		{
			return isRatio ? _value.ToPercentText() : Mathf.RoundToInt(_value).ToString();
		}
	}

	private Addon[] m_addons;
	private Addon[] Addons => m_addons ??= CreateAddons();

	public int AddonCount => Addons.Length;

	protected abstract Addon[] CreateAddons ();

	public string GetAddonTitle ( int _addonID )
	{
		return LocalizationManager.Instance.Get(Addons[_addonID].title);
	}

	public string GetAddonValueText ( int _level, int _addonID )
	{
		Addon addon = Addons[_addonID];
		return addon.Format(addon.valueAt(_level));
	}

	public string GetAddonGainText ( int _level, int _addonID )
	{
		Addon addon = Addons[_addonID];
		return addon.Format(addon.valueAt(_level + 1) - addon.valueAt(_level));
	}
}
