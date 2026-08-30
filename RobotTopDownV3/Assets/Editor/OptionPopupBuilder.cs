using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class OptionPopupBuilder
{
	private const string PrefabPath = "Assets/Prefabs/UI/Popup/OptionPopup.prefab";
	private const string BaseButtonPrefabPath = "Assets/Prefabs/UI/Button/BaseButton.prefab";
	private const string FontGuid = "8f586378b4e144a9851e7b34d9b748ee";

	private static readonly (string display, string action, int bindingIndex)[] RebindDefinitions =
	{
		("Avancer", "Move", 2),
		("Reculer", "Move", 4),
		("Aller à gauche", "Move", 6),
		("Aller à droite", "Move", 8),
		("Interagir", "Interact", 0),
		("Rotate Right", "RotateCameraCW", 0),
		("Rotate Left", "RotateCameraCCW", 0),
	};

	[MenuItem("Tools/UI/Build Option Popup")]
	public static void Build ()
	{
		GameObject baseButtonSource = AssetDatabase.LoadAssetAtPath<GameObject>(BaseButtonPrefabPath);
		if (baseButtonSource == null)
		{
			Debug.LogError($"[OptionPopupBuilder] Introuvable : {BaseButtonPrefabPath}");
			return;
		}

		TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));

		GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

		Transform background = root.transform.Find("Background");
		Transform title = background.Find("Title");
		Transform closeBtn = background.Find("CloseBtn");
		Transform placeholder = background.Find("ContentTMP");

		RectTransform rootRect = root.GetComponent<RectTransform>();
		rootRect.sizeDelta = new Vector2(760, 900);

		RectTransform titleRect = (RectTransform)title;
		titleRect.anchorMin = new Vector2(0.5f, 1f);
		titleRect.anchorMax = new Vector2(0.5f, 1f);
		titleRect.pivot = new Vector2(0.5f, 1f);
		titleRect.anchoredPosition = new Vector2(0, -50);

		TextMeshProUGUI titleTmp = title.Find("TitleTMP").GetComponent<TextMeshProUGUI>();
		titleTmp.text = "Options";

		RectTransform closeRect = (RectTransform)closeBtn;
		closeRect.anchorMin = new Vector2(1f, 1f);
		closeRect.anchorMax = new Vector2(1f, 1f);
		closeRect.pivot = new Vector2(1f, 1f);
		closeRect.anchoredPosition = new Vector2(-14, -21);

		if (placeholder != null)
			Object.DestroyImmediate(placeholder.gameObject);

		Transform existingContent = background.Find("Content");
		if (existingContent != null)
			Object.DestroyImmediate(existingContent.gameObject);

		Transform existingPrompt = background.Find("RebindPromptLabel");
		if (existingPrompt != null)
			Object.DestroyImmediate(existingPrompt.gameObject);

		GameObject contentGO = new GameObject("Content", typeof(RectTransform));
		contentGO.layer = background.gameObject.layer;
		contentGO.transform.SetParent(background, false);

		RectTransform contentRect = contentGO.GetComponent<RectTransform>();
		contentRect.anchorMin = Vector2.zero;
		contentRect.anchorMax = Vector2.one;
		contentRect.offsetMin = new Vector2(50, 20);
		contentRect.offsetMax = new Vector2(-50, -110);

		VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
		vlg.spacing = 16;
		vlg.childAlignment = TextAnchor.UpperCenter;
		vlg.childControlWidth = true;
		vlg.childForceExpandWidth = true;
		vlg.childControlHeight = true;
		vlg.childForceExpandHeight = false;

		DefaultControls.Resources uguiResources = GetUguiResources();
		TMP_DefaultControls.Resources tmpResources = GetTmpResources(uguiResources);

		AddSectionHeader(contentGO.transform, "Volume", font);
		RectTransform volumeRow = CreateRow(contentGO.transform, "VolumeRow", 40);
		AddLabel(volumeRow, "Volume général", font, 220, TextAlignmentOptions.Left, 0);
		Slider volumeSlider = CreateSlider(volumeRow, uguiResources);
		volumeSlider.minValue = 0f;
		volumeSlider.maxValue = 1f;
		volumeSlider.value = 1f;

		AddSectionHeader(contentGO.transform, "Langue", font);
		RectTransform langRow = CreateRow(contentGO.transform, "LanguageRow", 40);
		AddLabel(langRow, "Langue", font, 220, TextAlignmentOptions.Left, 0);
		TMP_Dropdown languageDropdown = CreateDropdown(langRow, tmpResources, font);

		AddSectionHeader(contentGO.transform, "Contrôles", font);

		var rebindLabels = new List<TextMeshProUGUI>();
		var rebindButtons = new List<BaseButton>();

		foreach (var def in RebindDefinitions)
		{
			RectTransform row = CreateRow(contentGO.transform, $"{def.action}_{def.bindingIndex}Row", 46);
			AddLabel(row, def.display, font, 300, TextAlignmentOptions.Left, 0);
			TextMeshProUGUI bindingLabel = AddLabel(row, "-", font, 140, TextAlignmentOptions.Center, 0);
			BaseButton rebindBtn = CreateActionButton(row, baseButtonSource, "Modifier", font, 130, 40);

			rebindLabels.Add(bindingLabel);
			rebindButtons.Add(rebindBtn);
		}

		RectTransform resetRow = CreateRow(contentGO.transform, "ResetRow", 60);
		HorizontalLayoutGroup resetHlg = resetRow.GetComponent<HorizontalLayoutGroup>();
		resetHlg.childAlignment = TextAnchor.MiddleCenter;
		resetHlg.childControlWidth = false;
		resetHlg.childForceExpandWidth = false;
		BaseButton resetBtn = CreateActionButton(resetRow, baseButtonSource, "Réinitialiser les contrôles", font, 320, 50);

		GameObject promptGO = new GameObject("RebindPromptLabel", typeof(RectTransform));
		promptGO.layer = background.gameObject.layer;
		promptGO.transform.SetParent(background, false);

		RectTransform promptRect = promptGO.GetComponent<RectTransform>();
		promptRect.anchorMin = new Vector2(0.5f, 0f);
		promptRect.anchorMax = new Vector2(0.5f, 0f);
		promptRect.pivot = new Vector2(0.5f, 0f);
		promptRect.anchoredPosition = new Vector2(0, 16);
		promptRect.sizeDelta = new Vector2(560, 36);

		TextMeshProUGUI promptTmp = promptGO.AddComponent<TextMeshProUGUI>();
		promptTmp.font = font;
		promptTmp.fontSize = 24;
		promptTmp.alignment = TextAlignmentOptions.Center;
		promptTmp.color = Color.black;
		promptTmp.text = "Appuyez sur une touche...";
		promptGO.SetActive(false);

		OptionPopup optionPopup = root.GetComponent<OptionPopup>();
		SerializedObject so = new SerializedObject(optionPopup);

		so.FindProperty("m_closeBtn").objectReferenceValue = closeBtn.GetComponent<BaseButton>();
		so.FindProperty("m_volumeSlider").objectReferenceValue = volumeSlider;
		so.FindProperty("m_languageDropdown").objectReferenceValue = languageDropdown;
		so.FindProperty("m_resetControlsBtn").objectReferenceValue = resetBtn;
		so.FindProperty("m_rebindPromptLabel").objectReferenceValue = promptTmp;

		SerializedProperty entriesProp = so.FindProperty("m_rebindEntries");
		entriesProp.arraySize = RebindDefinitions.Length;
		for (int i = 0; i < RebindDefinitions.Length; i++)
		{
			SerializedProperty entry = entriesProp.GetArrayElementAtIndex(i);
			entry.FindPropertyRelative("actionName").stringValue = RebindDefinitions[i].action;
			entry.FindPropertyRelative("bindingIndex").intValue = RebindDefinitions[i].bindingIndex;
			entry.FindPropertyRelative("bindingLabel").objectReferenceValue = rebindLabels[i];
			entry.FindPropertyRelative("rebindButton").objectReferenceValue = rebindButtons[i];
		}

		so.ApplyModifiedProperties();

		PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
		PrefabUtility.UnloadPrefabContents(root);

		Debug.Log("[OptionPopupBuilder] OptionPopup.prefab mis à jour.");
	}

	private static RectTransform CreateRow ( Transform parent, string name, float height )
	{
		GameObject row = new GameObject(name, typeof(RectTransform));
		row.layer = parent.gameObject.layer;
		row.transform.SetParent(parent, false);

		LayoutElement le = row.AddComponent<LayoutElement>();
		le.preferredHeight = height;
		le.flexibleWidth = 1;

		HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
		hlg.spacing = 16;
		hlg.childAlignment = TextAnchor.MiddleLeft;
		hlg.childControlWidth = true;
		hlg.childForceExpandWidth = false;
		hlg.childControlHeight = true;
		hlg.childForceExpandHeight = false;

		return row.GetComponent<RectTransform>();
	}

	private static void AddSectionHeader ( Transform parent, string text, TMP_FontAsset font )
	{
		GameObject go = new GameObject(text + "Header", typeof(RectTransform));
		go.layer = parent.gameObject.layer;
		go.transform.SetParent(parent, false);

		LayoutElement le = go.AddComponent<LayoutElement>();
		le.preferredHeight = 28;

		TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
		tmp.font = font;
		tmp.text = text;
		tmp.fontSize = 26;
		tmp.fontStyle = FontStyles.Bold;
		tmp.color = Color.black;
		tmp.alignment = TextAlignmentOptions.Left;
	}

	private static TextMeshProUGUI AddLabel ( Transform parent, string text, TMP_FontAsset font, float width, TextAlignmentOptions alignment, float flexibleWidth )
	{
		GameObject go = new GameObject("Label", typeof(RectTransform));
		go.layer = parent.gameObject.layer;
		go.transform.SetParent(parent, false);

		LayoutElement le = go.AddComponent<LayoutElement>();
		le.preferredWidth = width;
		le.flexibleWidth = flexibleWidth;

		TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
		tmp.font = font;
		tmp.text = text;
		tmp.fontSize = 22;
		tmp.color = Color.black;
		tmp.alignment = alignment;

		return tmp;
	}

	private static Slider CreateSlider ( Transform row, DefaultControls.Resources resources )
	{
		GameObject sliderGO = DefaultControls.CreateSlider(resources);
		sliderGO.transform.SetParent(row, false);
		SetLayerRecursively(sliderGO, row.gameObject.layer);

		LayoutElement le = sliderGO.AddComponent<LayoutElement>();
		le.flexibleWidth = 1;
		le.minWidth = 150;
		le.preferredHeight = 20;

		return sliderGO.GetComponent<Slider>();
	}

	private static TMP_Dropdown CreateDropdown ( Transform row, TMP_DefaultControls.Resources resources, TMP_FontAsset font )
	{
		GameObject dropdownGO = TMP_DefaultControls.CreateDropdown(resources);
		dropdownGO.transform.SetParent(row, false);
		SetLayerRecursively(dropdownGO, row.gameObject.layer);

		LayoutElement le = dropdownGO.AddComponent<LayoutElement>();
		le.flexibleWidth = 1;
		le.minWidth = 150;
		le.preferredHeight = 34;

		if (font != null)
		{
			foreach (TextMeshProUGUI tmp in dropdownGO.GetComponentsInChildren<TextMeshProUGUI>(true))
				tmp.font = font;
		}

		return dropdownGO.GetComponent<TMP_Dropdown>();
	}

	private static BaseButton CreateActionButton ( Transform row, GameObject baseButtonSource, string label, TMP_FontAsset font, float width, float height )
	{
		GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(baseButtonSource, row);

		RectTransform rect = instance.GetComponent<RectTransform>();
		rect.sizeDelta = new Vector2(width, height);

		LayoutElement le = instance.AddComponent<LayoutElement>();
		le.preferredWidth = width;
		le.preferredHeight = height;
		le.flexibleWidth = 0;

		Transform btnChild = instance.transform.Find("Btn");
		Transform labelParent = btnChild != null ? btnChild : instance.transform;

		GameObject labelGO = new GameObject("Label", typeof(RectTransform));
		labelGO.layer = instance.layer;
		labelGO.transform.SetParent(labelParent, false);

		RectTransform labelRect = labelGO.GetComponent<RectTransform>();
		labelRect.anchorMin = Vector2.zero;
		labelRect.anchorMax = Vector2.one;
		labelRect.offsetMin = Vector2.zero;
		labelRect.offsetMax = Vector2.zero;

		TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
		tmp.font = font;
		tmp.text = label;
		tmp.fontSize = 20;
		tmp.color = Color.black;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.raycastTarget = false;

		return instance.GetComponent<BaseButton>();
	}

	private static void SetLayerRecursively ( GameObject go, int layer )
	{
		go.layer = layer;
		for (int i = 0; i < go.transform.childCount; i++)
			SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
	}

	private static DefaultControls.Resources GetUguiResources ()
	{
		return new DefaultControls.Resources
		{
			standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
			background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
			inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
			knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
			checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
			dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
			mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
		};
	}

	private static TMP_DefaultControls.Resources GetTmpResources ( DefaultControls.Resources source )
	{
		return new TMP_DefaultControls.Resources
		{
			standard = source.standard,
			background = source.background,
			inputField = source.inputField,
			knob = source.knob,
			checkmark = source.checkmark,
			dropdown = source.dropdown,
			mask = source.mask,
		};
	}
}
