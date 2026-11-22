using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SquadUnitFullMicroDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private TextMeshProUGUI m_hpValueTMP;
	[SerializeField] private Image m_mainFactionIconImg;
	[SerializeField] private TextMeshProUGUI m_mainFactionPercentageTMP;
	[SerializeField] private StatusDisplay[] m_statusDisplays;
	[SerializeField] private UnitStatBlock m_statBlock;

	private void Awake ()
	{
		PlayerController.onEntitySelected += OnEntitySelected;
		PlayerController.onEntityHovered += OnEntityHovered;
	}

	private void OnDestroy ()
	{
		PlayerController.onEntitySelected -= OnEntitySelected;
		PlayerController.onEntityHovered -= OnEntityHovered;
	}

	private void OnEntitySelected (int? _entityID)
	{
		RefreshVisual(_entityID);
	}

	private void OnEntityHovered ( int? _entityID )
	{
		if (_entityID.HasValue)
		{
			RefreshVisual(_entityID);
			return;
		}

		Entity selectedEntity = PlayerController.Instance == null ? null : PlayerController.Instance.SelectedEntity;
		RefreshVisual(selectedEntity == null ? null : selectedEntity.ID);
	}

	private bool AreStatsVisibleOn ( Entity _entity )
	{
		return _entity.IsAlliedTo(GameManager.Instance.PlayerID)
			|| (_entity.IsVisible && _entity.HowIsUnitVisible == NeuronalMembraneEquipmentData.VisionTypes.Thermic);
	}

	private void RefreshVisual ( int? _entityID )
	{
		if (_entityID == null || !_entityID.HasValue)
			return;

		Entity entity = GameManager.Instance.GetEntityFromID(_entityID.Value);
		m_nameTMP.text = entity.Data.name;
		m_hpValueTMP.text = entity.Equipment.CurrentHealth + " / " + entity.Equipment.MaxHealth;
		FrameEquipmentData.EntityFaction mainFaction = entity.Data.GetDominentFaction(out float percentage);
		m_mainFactionPercentageTMP.text = (percentage*100f).ToString() + "%" ;
		m_mainFactionIconImg.sprite = GameAssets.current.ui.corporationsIcons[mainFaction];

		m_statBlock.Init(entity.Data, AreStatsVisibleOn(entity));

		for (int i = 0; i < m_statusDisplays.Length; i++)
		{
			if (entity.Status.Count <= i)
				m_statusDisplays[i].Hide();
			else
			{
				m_statusDisplays[i].Init(GameAssets.current.game.entityStatus[entity.Status[i]]);
				m_statusDisplays[i].Show();
			}
		}
	}
}
