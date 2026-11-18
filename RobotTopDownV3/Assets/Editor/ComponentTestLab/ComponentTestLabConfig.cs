using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ComponentTestLabConfig : ScriptableObject
{
	public const string c_assetPath = "Assets/Editor/ComponentTestLab/ComponentTestLabConfig.asset";
	private const string c_defaultScenePath = "Assets/Scenes/Test/ComponentTestLab.unity";
	private const string c_defaultMapPath = "Assets/Objects/Grid/Test/ComponentTestLab.asset";

	public SceneAsset scene;
	public GridData map;

	public FrameEquipmentData baseFrame;
	public ReactorEquipmentData baseReactor;
	public BrainEquipmentData baseBrain;
	public NeuronalMembraneEquipmentData baseMembrane;
	public WeaponEquipmentData defaultWeapon;
	public UnitPreset dummyPreset;

	[Min(0f)] public float respawnDelay = 3f;
	public bool revealMapOnStart;
	[Range(1, ComponentTestLabLayout.c_maxDistance)] public int benchDistance = 3;

	public ComponentTestLabLoadout benchUnit = new();
	public ComponentTestLabLoadout benchDummy = new();

	public List<string> performedActions = new();

	public string ScenePath => scene != null ? AssetDatabase.GetAssetPath(scene) : c_defaultScenePath;
	public GridData Map => map != null ? map : AssetDatabase.LoadAssetAtPath<GridData>(c_defaultMapPath);

	public static ComponentTestLabConfig Find ()
	{
		ComponentTestLabConfig config = AssetDatabase.LoadAssetAtPath<ComponentTestLabConfig>(c_assetPath);
		if (config != null)
			return config;

		string[] guids = AssetDatabase.FindAssets("t:" + nameof(ComponentTestLabConfig));
		return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<ComponentTestLabConfig>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
	}

	public static ComponentTestLabConfig LoadOrCreate ()
	{
		ComponentTestLabConfig config = Find();
		if (config != null)
			return config;

		config = CreateInstance<ComponentTestLabConfig>();
		config.scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(c_defaultScenePath);
		config.map = AssetDatabase.LoadAssetAtPath<GridData>(c_defaultMapPath);
		AssetDatabase.CreateAsset(config, c_assetPath);
		AssetDatabase.SaveAssets();
		return config;
	}
}

[System.Serializable]
public class ComponentTestLabLoadout
{
	public FrameEquipmentData frame;
	public ReactorEquipmentData reactor;
	public BrainEquipmentData brain;
	public NeuronalMembraneEquipmentData membrane;
	public List<EntityEquipmentData> arms = new();
	public List<EntityEquipmentData> auxiliary = new();
	public List<ChipsetEquipmentData> chipsets = new();
}
