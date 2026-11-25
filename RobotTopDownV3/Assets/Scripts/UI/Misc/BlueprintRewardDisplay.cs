public class BlueprintRewardDisplay : ComponentRewardDisplay
{
    protected override string GetDisplayedName ( EntityEquipmentData _component )
	{
        return string.Format(LocalizationManager.Instance.Get(LocalizationKey.reward_blueprint), _component.GetLocalizedName());
	}
}
