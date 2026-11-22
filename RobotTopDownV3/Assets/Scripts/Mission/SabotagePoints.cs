using System;
using UnityEngine;

public static class SabotagePoints
{
	public static Action onAnyChanged;

	public static int GetOwnerOf ( Tile _tile )
	{
		return _tile.GroundType == TileGroundType.PlayerStructure ? 0 : 1;
	}

	public static bool TryGetLivingPoint ( Tile _tile, out int _ownerID )
	{
		_ownerID = _tile != null && _tile.IsSabotagePoint ? GetOwnerOf(_tile) : -1;
		return _ownerID != -1 && _tile.Wall.Health > 0;
	}

	public static string GetLocalizedName ( int _ownerID )
	{
		return LocalizationManager.Instance.Get(_ownerID == GameManager.Instance.PlayerID ? LocalizationKey.sabotage_point_ally : LocalizationKey.sabotage_point_enemy);
	}

	public static void OnWallDamaged ( Wall _wall )
	{
		Tile tile = _wall.LinkedTile;
		if (tile == null || !tile.IsSabotagePoint)
			return;

		int ownerID = GetOwnerOf(tile);
		int health = Mathf.Max(0, _wall.Health);
		string pointName = GetLocalizedName(ownerID);
		string healthText = string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_sabotage_point_damaged), pointName, health, GameConfig.current.game.sabotagePointHealth);
		LogCard card = new(LogCard.Kind.Sabotage, pointName);
		card.SetHealth(health, GameConfig.current.game.sabotagePointHealth);
		LogConsole.AddLog(healthText, LogConsole.LogEventType.Damage, new LogConsole.LogDetails(card));

		if (health == 0)
		{
			bool isAllyPoint = ownerID == GameManager.Instance.PlayerID;
			LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(isAllyPoint ? LocalizationKey.log_sabotage_point_destroyed_ally : LocalizationKey.log_sabotage_point_destroyed_enemy)
				, GameManager.Instance.GetSabotagePointCount(ownerID, true))
				, LogConsole.LogEventType.Round);
		}

		onAnyChanged?.Invoke();
	}
}
