using UnityEngine;

[CreateAssetMenu(fileName = "UISoundConfig", menuName = "Audio/UI Sound Config")]
public class UISoundConfig : ScriptableObject
{
	[Header("Buttons")]
	public SfxId buttonClick = SfxId.None;
	public SfxId buttonHover = SfxId.None;
	public SfxId buttonBlocked = SfxId.None;

	[Header("Popups")]
	public SfxId popupOpen = SfxId.None;
	public SfxId popupClose = SfxId.None;

	[Header("Panels")]
	public SfxId panelOpen = SfxId.None;
	public SfxId panelClose = SfxId.None;

	[Header("Widgets")]
	public SfxId toggleOn = SfxId.None;
	public SfxId toggleOff = SfxId.None;
	public SfxId sliderChanged = SfxId.None;
	public SfxId dropdownChanged = SfxId.None;

	[Header("Drag & drop")]
	public SfxId dragPickUp = SfxId.None;
	public SfxId dragDrop = SfxId.None;
}
