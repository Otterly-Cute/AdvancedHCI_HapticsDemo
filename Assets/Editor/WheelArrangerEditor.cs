using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WheelArranger))]
public class WheelArrangerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        WheelArranger script = (WheelArranger)target;

        if (GUILayout.Button("🌀 Arrange Podiums In Circle", GUILayout.Height(30)))
        {
            Undo.RecordObject(script.transform, "Arrange Podiums In Circle");
            script.ArrangeInCircle();
            EditorUtility.SetDirty(script);
        }
    }
}
