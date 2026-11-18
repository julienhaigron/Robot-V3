using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class RotateEntityAction : AEntityAction
{
	public int[] targetedOrientationID; //0 - 5
	public bool isFacingOrder;

	public override bool IsPlayedFirst => !isFacingOrder && base.IsPlayedFirst;
	public int FacingOrientation => targetedOrientationID != null && targetedOrientationID.Length > 0 ? targetedOrientationID[^1] : -1;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetedOrientationID);
		serializer.SerializeValue(ref isFacingOrder);
	}

	public void SetFacingOrder ( Tile _pivot, int _orientation, Tile _aimedTile )
	{
		isFacingOrder = true;
		supposedPositionAtActionStartID = _pivot.coordinates.ID;
		positionAtActionEndID = _pivot.coordinates.ID;
		targetedOrientationID = new int[1] { _orientation };
		targetTileIDs = new int[1] { (_aimedTile != null ? _aimedTile : _pivot).coordinates.ID };
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		SetFacingOrder(GridManager.Instance.Tiles[supposedPositionAtActionStartID]
			, GridManager.Instance.GetClosestOrientation(GridManager.Instance.Tiles[supposedPositionAtActionStartID], _tile), _tile);

		base.RegisterInteraction(_tile);
	}

	public override void SetResolvedTargets ( int[] _targetTileIDs, int[] _targetedEntityIDs )
	{
		base.SetResolvedTargets(_targetTileIDs, _targetedEntityIDs);

		if (targetedOrientationID != null && targetedOrientationID.Length > 0)
			return;

		if (_targetTileIDs == null || _targetTileIDs.Length == 0)
			return;

		Tile from = PerformingEntity.Displacement.Coordinates.GetTile();
		targetedOrientationID = new int[1] { GridManager.Instance.GetClosestOrientation(from, GridManager.Instance.Tiles[_targetTileIDs[0]]) };
	}

	public override void Prepare ( Entity.EntityState _state )
	{

	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?

		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		if(targetedOrientationID == null || targetedOrientationID.Length <= lifetime)
		{
			//shouldnt happen
			EndTick();
		}
		else
		{
			PerformingEntity.Displacement.Rotate(targetedOrientationID[lifetime], GameConfig.current.game.entityRotationDuration, EndTick);
		}

		base.Perform(_state);
	}

	public Tile GetPivotOn ( TurnManager.RecordedAction _host )
	{
		if (_host == null || _host.action == null)
			return GridManager.Instance.Tiles[supposedPositionAtActionStartID];

		return GridManager.Instance.Tiles[IsPlayedFirst ? _host.action.supposedPositionAtActionStartID : _host.action.positionAtActionEndID];
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		if (FacingOrientation < 0 || _recordedAction == null || _recordedAction.action == null)
			return;

		ShowArrow(this, performingEntityID, GetPivotOn(_recordedAction), FacingOrientation
			, GetArrowMaterial(_recordedAction.entityState, !isFacingOrder));
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
	}

	public static RotationActionDisplay ShowArrow ( RotateEntityAction _action, int _performingEntityID, Tile _pivot, int _orientation, Material _material )
	{
		if (_pivot == null || _orientation < 0 || GameAssets.current.game.rotationHandlePoolData == null)
			return null;

		RotationActionDisplay display = ObjectsPooling.GetElement(GameAssets.current.game.rotationHandlePoolData) as RotationActionDisplay;
		if (display == null)
			return null;

		display.Init(_action, _material);
		display.transform.position = _pivot.transform.position;
		display.transform.rotation = GetFacingRotation(_orientation);

		PlayerController.Instance.AddRotationActionDisplay(display, _performingEntityID);
		return display;
	}

	public static Quaternion GetFacingRotation ( int _orientation )
	{
		return Quaternion.Euler(0f, 30f + _orientation * 60f, 0f);
	}

	public static Material GetArrowMaterial ( Entity.EntityState _state, bool _isGhost )
	{
		SerializableDictionary<Entity.EntityState, Material> materials = _isGhost
			? GameAssets.current.ui.ghostEntityStateMaterials
			: GameAssets.current.ui.entityStateMaterials;

		return materials != null && materials.TryGetValue(_state, out Material material) ? material : null;
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		Tile pivot = GridManager.Instance.Tiles[supposedPositionAtActionStartID];
		return _tile != null && _tile != pivot && pivot.Neighbors != null && System.Array.IndexOf(pivot.Neighbors, _tile) >= 0;
	}
}
