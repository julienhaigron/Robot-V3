using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ComponentTestLabMap
{
	public static bool IsLabScene ( Scene _scene, ComponentTestLabConfig _config )
	{
		return _config != null && _scene.IsValid() && string.Equals(_scene.path, _config.ScenePath, StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsLabSceneActive ( ComponentTestLabConfig _config )
	{
		return IsLabScene(SceneManager.GetActiveScene(), _config);
	}

	public static GridManager FindGrid ( Scene _scene )
	{
		if (!_scene.IsValid() || !_scene.isLoaded)
			return null;

		foreach (GameObject root in _scene.GetRootGameObjects())
		{
			GridManager grid = root.GetComponentInChildren<GridManager>(true);
			if (grid != null)
				return grid;
		}

		return null;
	}

	public static bool IsUpToDate ( ComponentTestLabConfig _config, ComponentTestLabLayout _layout )
	{
		GridManager grid = FindGrid(SceneManager.GetActiveScene());
		GridData data = _config.Map;
		if (grid == null || data == null || !_layout.IsValid)
			return false;

		return IsDataUpToDate(data, _layout) && IsGridUpToDate(grid, _layout);
	}

	public static bool Sync ( ComponentTestLabConfig _config, ComponentTestLabLayout _layout, out string _error )
	{
		_error = null;
		if (!_layout.IsValid)
		{
			_error = string.Join("\n", _layout.Errors);
			return false;
		}

		Scene scene = SceneManager.GetActiveScene();
		GridManager grid = FindGrid(scene);
		GridData data = _config.Map;
		if (grid == null || data == null)
		{
			_error = "La scène du labo doit contenir une GridManager, et la configuration doit pointer sur la GridData du labo.";
			return false;
		}

		if (new SerializedObject(grid).FindProperty("m_gridData").objectReferenceValue != data)
		{
			_error = "La GridManager de la scène du labo ne pointe pas sur " + data.name + ".";
			return false;
		}

		if (!IsDataUpToDate(data, _layout))
		{
			data.width = _layout.Width;
			data.height = _layout.Height;
			data.tiles = _layout.GroundTypes.Select(groundType => new GridData.TileData(groundType, Wall.WallType.VerticalStrait, 0)).ToArray();
			data.unitsFaceEnemySpawn = false;
			EditorUtility.SetDirty(data);
			AssetDatabase.SaveAssetIfDirty(data);
		}

		bool isSceneChanged = false;
		if (!HasMatchingTiles(grid, _layout) || grid.Tiles.Any(tile => tile.Wall != null))
		{
			try
			{
				EditorUtility.DisplayProgressBar("Labo de composants", "Génération de la carte " + _layout.Width + " x " + _layout.Height + "…", .5f);
				grid.GenerateGrid();
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			isSceneChanged = true;
		}
		else
		{
			for (int i = 0; i < grid.Tiles.Length; i++)
			{
				Tile tile = grid.Tiles[i];
				if (tile.GroundType != _layout.GroundTypes[i])
				{
					tile.SetGroundType(_layout.GroundTypes[i]);
					isSceneChanged = true;
				}
			}
		}

		if (isSceneChanged)
		{
			EditorSceneManager.MarkSceneDirty(scene);
			if (!EditorSceneManager.SaveScene(scene))
			{
				_error = "Impossible d'enregistrer la scène du labo.";
				return false;
			}
		}

		return true;
	}

	private static bool IsDataUpToDate ( GridData _data, ComponentTestLabLayout _layout )
	{
		if (_data.unitsFaceEnemySpawn || _data.width != _layout.Width || _data.height != _layout.Height || _data.tiles == null || _data.tiles.Length != _layout.GroundTypes.Length)
			return false;

		for (int i = 0; i < _data.tiles.Length; i++)
		{
			if (_data.tiles[i] == null || _data.tiles[i].groundType != _layout.GroundTypes[i])
				return false;
		}

		return true;
	}

	private static bool HasMatchingTiles ( GridManager _grid, ComponentTestLabLayout _layout )
	{
		return _grid.Width == _layout.Width && _grid.Height == _layout.Height && _grid.Tiles != null
			&& _grid.Tiles.Length == _layout.GroundTypes.Length && _grid.Tiles.All(tile => tile != null);
	}

	private static bool IsGridUpToDate ( GridManager _grid, ComponentTestLabLayout _layout )
	{
		if (!HasMatchingTiles(_grid, _layout))
			return false;

		for (int i = 0; i < _grid.Tiles.Length; i++)
		{
			if (_grid.Tiles[i].Wall != null || _grid.Tiles[i].GroundType != _layout.GroundTypes[i])
				return false;
		}

		return true;
	}
}
