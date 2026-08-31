using UnityEditor;
using UnityEditor.UI;

/// <summary>
/// Sans cet editor, UniformHorizontalLayoutGroup heriterait de HorizontalOrVerticalLayoutGroupEditor
/// (declare avec editorForChildClasses = true) et ses champs de slots seraient invisibles dans l'inspector.
/// </summary>
[CustomEditor(typeof(UniformHorizontalLayoutGroup), true)]
[CanEditMultipleObjects]
public class UniformHorizontalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
{
	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI();

		serializedObject.Update();

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Uniform Slots", EditorStyles.boldLabel);

		SerializedProperty slotWidthMode = serializedObject.FindProperty("m_slotWidthMode");
		EditorGUILayout.PropertyField(slotWidthMode);

		switch ((UniformHorizontalLayoutGroup.SlotWidthMode)slotWidthMode.enumValueIndex)
		{
			case UniformHorizontalLayoutGroup.SlotWidthMode.Fixed:
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_slotWidth"));
				break;

			case UniformHorizontalLayoutGroup.SlotWidthMode.SplitParent:
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_slotCount"));
				break;
		}

		EditorGUILayout.PropertyField(serializedObject.FindProperty("m_shrinkSlotsToFit"));

		serializedObject.ApplyModifiedProperties();
	}
}
