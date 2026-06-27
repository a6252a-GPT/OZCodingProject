using UnityEditor;
using UnityEngine;

namespace TeamProject01.Gameplay.EditorTools
{
    [CustomEditor(typeof(BonusChest))]
    public sealed class BonusChestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            EditorGUILayout.Space(8.0f);

            DrawSectionTitle("상자 감지 설정");
            DrawHelp("컨보이 머리가 열림 거리 안에 들어오면 상자가 열립니다. 실제 보상 지급은 보상 시스템에서 연결합니다.");
            DrawProperty("openDistance", "열림 거리");

            DrawSectionTitle("애니메이션 설정");
            DrawHelp("상자가 자동으로 열리면 애니메이터 정지를 켜고, 너무 느리면 열림 애니메이션 속도를 올립니다.");
            DrawProperty("animator", "상자 애니메이터");
            DrawProperty("openTriggerName", "열림 트리거 이름");
            DrawProperty("openAnimationSpeed", "열림 애니메이션 속도");
            DrawProperty("openAnimationStart", "열림 애니메이션 시작 지점");
            DrawProperty("pauseAnimatorUntilOpen", "열리기 전 애니메이터 정지");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                MonoScript script = MonoScript.FromMonoBehaviour((BonusChest)target);
                EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            }
        }

        private static void DrawSectionTitle(string title)
        {
            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawHelp(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void DrawProperty(string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                EditorGUILayout.HelpBox($"{propertyName} 항목을 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }
    }
}
