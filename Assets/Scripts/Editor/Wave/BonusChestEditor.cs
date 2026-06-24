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
            DrawProperty("openDistance", "열림 거리");
            DrawProperty("collectDistance", "보상 획득 거리");

            DrawSectionTitle("보상 설정");
            DrawProperty("experienceReward", "경험치 보상");
            DrawProperty("goldReward", "골드 보상");

            DrawSectionTitle("애니메이션 설정");
            DrawProperty("animator", "상자 애니메이터");
            DrawProperty("openTriggerName", "열림 트리거 이름");
            DrawProperty("pauseAnimatorUntilOpen", "열리기 전 애니메이터 정지");
            DrawProperty("destroyAfterReward", "보상 후 상자 제거");
            DrawProperty("destroyDelay", "제거 대기 시간");

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
