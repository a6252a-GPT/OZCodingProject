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
            DrawHelp("컨보이 머리가 열림 거리 안에 들어오면 상자가 열리고, 보상 드랍 거리 안에서 딜레이가 끝나면 보상이 떨어집니다.");
            DrawProperty("openDistance", "열림 거리");
            DrawProperty("collectDistance", "보상 드랍 거리");

            DrawSectionTitle("보상 설정");
            DrawHelp("총 보상량은 밸런스, 드랍 개수는 화면 연출입니다. 개수를 늘리면 보상이 우르르 떨어지는 느낌이 강해집니다.");
            DrawProperty("experienceReward", "총 경험치 보상");
            DrawProperty("goldReward", "총 골드 보상");
            DrawProperty("experienceDropCount", "경험치 드랍 개수");
            DrawProperty("goldDropCount", "골드 드랍 개수");
            DrawProperty("rewardSpreadRadius", "보상 퍼짐 반경");
            DrawProperty("rewardDropDelay", "보상 드랍 딜레이(초)");

            DrawSectionTitle("애니메이션 설정");
            DrawHelp("상자가 자동으로 열리면 애니메이터 정지를 켜고, 너무 느리면 열림 애니메이션 속도를 올립니다.");
            DrawProperty("animator", "상자 애니메이터");
            DrawProperty("openTriggerName", "열림 트리거 이름");
            DrawProperty("openAnimationSpeed", "열림 애니메이션 속도");
            DrawProperty("openAnimationStart", "열림 애니메이션 시작 지점");
            DrawProperty("pauseAnimatorUntilOpen", "열리기 전 애니메이터 정지");
            DrawProperty("destroyAfterReward", "보상 후 상자 제거");
            DrawProperty("destroyDelay", "제거 대기 시간(초)");

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
