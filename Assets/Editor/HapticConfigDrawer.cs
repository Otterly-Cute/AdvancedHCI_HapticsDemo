#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HapticsDemo.Editor
{
    [CustomPropertyDrawer(typeof(HapticConfig))]
    public class HapticConfigDrawer : PropertyDrawer
    {
        const float LabelWidth = 170f;
        const float VSP = 3f;
        const float HeaderGap = 3f;

        public override void OnGUI(Rect r, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(r, label, prop);

            var header = Line(ref r);
            prop.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(header, prop.isExpanded, label);
            EditorGUI.EndFoldoutHeaderGroup();

            if (prop.isExpanded)
            {
                EditorGUI.indentLevel++;
                float oldLW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LabelWidth;

                var sfx = prop.FindPropertyRelative("sfx");
                var sfxVolume = prop.FindPropertyRelative("sfxVolume");
                var basicAmplitude = prop.FindPropertyRelative("basicAmplitude");
                var advancedClip = prop.FindPropertyRelative("advancedClip");
                var advancedAmplitude = prop.FindPropertyRelative("advancedAmplitude");

                bool overridesAmplitude = prop.serializedObject.targetObject is IOverridesAmplitude;

                Header(ref r, "Audio");
                EditorGUI.PropertyField(Line(ref r), sfx, new GUIContent("SFX"));
                EditorGUI.PropertyField(Line(ref r), sfxVolume, new GUIContent("SFX Volume"));

                Header(ref r, "Basic (parametric)");
                using (new EditorGUI.DisabledGroupScope(overridesAmplitude))
                {
                    EditorGUI.PropertyField(
                        Line(ref r),
                        basicAmplitude,
                        new GUIContent("Basic Amplitude")
                    );
                }

                Header(ref r, "Advanced (.haptic via Meta SDK)");
                EditorGUI.PropertyField(Line(ref r), advancedClip, new GUIContent("Advanced Clip"));
                using (new EditorGUI.DisabledGroupScope(overridesAmplitude))
                {
                    EditorGUI.PropertyField(
                        Line(ref r),
                        advancedAmplitude,
                        new GUIContent("Advanced Amplitude")
                    );
                }

                if (overridesAmplitude)
                {
                    var infoRect = Line(ref r);
                    EditorGUI.LabelField(
                        infoRect,
                        new GUIContent("⚙ Amplitude controlled by station logic"),
                        EditorStyles.miniLabel
                    );
                }

                EditorGUIUtility.labelWidth = oldLW;
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            if (!prop.isExpanded)
                return LineHeight();

            int lines = 1 + (1 + 2) + (1 + 2) + (1 + 2);
            float gaps = HeaderGap * 3f;
            return LineHeight(lines) + gaps;
        }

        static Rect Line(ref Rect r)
        {
            var line = new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight);
            r.y += EditorGUIUtility.singleLineHeight + VSP;
            return line;
        }

        static float LineHeight(int lines = 1) => lines * (EditorGUIUtility.singleLineHeight + VSP);

        static void Header(ref Rect r, string text)
        {
            var rect = Line(ref r);
            EditorGUI.LabelField(rect, text, EditorStyles.boldLabel);
            r.y += HeaderGap;
        }
    }
}
#endif
