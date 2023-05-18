using TMPro.EditorUtilities;
using UnityEditor;

[CustomEditor(typeof(FlexibleTextMesh))]
public class FlexibleTextMeshEditor : TMP_EditorPanelUI
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
