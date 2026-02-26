using UnityEditor;
using UnityEngine;

public enum shadowType { None, Hard, DynamicSoft }

public class LightData : MonoBehaviour
{
    public Color color;
    public float range;
    public float intensity;
    public bool baked;
    public shadowType shading;
    public float blurMultiplier;
    public float blurPower;
}

#if UNITY_EDITOR

[CustomEditor(typeof(LightData))]
public class LightDataEditor : Editor
{
    SerializedProperty color;
    SerializedProperty range;
    SerializedProperty intensity;
    SerializedProperty baked;
    SerializedProperty shading;
    SerializedProperty blurMultiplier;
    SerializedProperty blurPower;

    void OnEnable()
    {
        color = serializedObject.FindProperty("color");
        range = serializedObject.FindProperty("range");
        intensity = serializedObject.FindProperty("intensity");
        baked = serializedObject.FindProperty("baked");
        shading = serializedObject.FindProperty("shading");
        blurMultiplier = serializedObject.FindProperty("blurMultiplier");
        blurPower = serializedObject.FindProperty("blurPower");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(color);
        EditorGUILayout.PropertyField(range);
        EditorGUILayout.PropertyField(intensity);
        EditorGUILayout.PropertyField(baked);
        EditorGUILayout.PropertyField(shading);

        if ((shadowType)shading.enumValueIndex == shadowType.DynamicSoft)
        {
            EditorGUI.indentLevel = 1;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Soft Shadow Settings", EditorStyles.boldLabel);
            EditorGUILayout.Slider(blurMultiplier, 0f, 200f, new GUIContent("Blur Multiplier"));
            EditorGUILayout.Slider(blurPower, 1f, 15, new GUIContent("Blur Power"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
