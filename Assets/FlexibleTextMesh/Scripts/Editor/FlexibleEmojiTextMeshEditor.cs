#if HAS_EMOJI_SEARCH
using KyubEditor.EmojiSearch.UI;
using UnityEditor;

[CustomEditor(typeof(FlexibleEmojiTextMesh))]
public class FlexibleEmojiTextMeshEditor : TMP_EmojiTextUGUIEditor
{
	SerializedProperty _shrinkContent;
	SerializedProperty _shrinkLineByLine;
	SerializedProperty _curveType;
	SerializedProperty _radius;

	new void OnEnable()
	{
		base.OnEnable();
		_shrinkContent = serializedObject.FindProperty("_shrinkContent");
		_shrinkLineByLine = serializedObject.FindProperty("_shrinkLineByLine");
		_curveType = serializedObject.FindProperty("_curveType");
		_radius = serializedObject.FindProperty("_radius");
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		EditorGUILayout.PropertyField(_shrinkContent);
		EditorGUILayout.PropertyField(_shrinkLineByLine);
		EditorGUILayout.PropertyField(_curveType);
		EditorGUILayout.PropertyField(_radius);
		serializedObject.ApplyModifiedProperties();
	}
}
#endif
