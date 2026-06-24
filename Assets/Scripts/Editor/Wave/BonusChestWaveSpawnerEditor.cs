using UnityEditor;
using UnityEngine;

namespace TeamProject01.Gameplay.EditorTools
{
    [CustomEditor(typeof(BonusChestWaveSpawner))]
    public sealed class BonusChestWaveSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            EditorGUILayout.Space(8.0f);

            DrawSectionTitle("참조 설정");
            DrawProperty("chestPrefab", "상자 프리팹");
            DrawProperty("chestRoot", "상자 생성 부모");

            DrawSectionTitle("생성 위치 설정");
            DrawProperty("spawnAroundConvoy", "컨보이 주변에 생성");
            DrawProperty("fallbackCenter", "대체 기준 위치");
            DrawProperty("minSpawnRadius", "최소 생성 반경");
            DrawProperty("maxSpawnRadius", "최대 생성 반경");
            DrawProperty("groundHeightOffset", "바닥 높이 보정");

            DrawSectionTitle("상자 등급별 생성 규칙");
            DrawProperty("chestRules", "상자 생성 규칙");

            EditorGUILayout.Space(8.0f);

            if (GUILayout.Button("보너스 상자 웨이브 생성"))
            {
                foreach (Object targetObject in targets)
                {
                    if (targetObject is BonusChestWaveSpawner spawner)
                    {
                        spawner.SpawnBonusChestWave();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                MonoScript script = MonoScript.FromMonoBehaviour((BonusChestWaveSpawner)target);
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
