using UnityEditor;
using UnityEngine;

namespace TeamProject01.Gameplay.EditorTools
{
    [CustomEditor(typeof(WaveController))]
    public sealed class WaveControllerEditor : Editor // WaveController Inspector를 초보자용 설명형으로 보여준다.
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update(); // Inspector에 표시할 최신 SerializedProperty 값을 가져온다.

            DrawScriptField(); // 어떤 스크립트인지 확인용으로 보여준다.
            EditorGUILayout.Space(8.0f);

            DrawSectionTitle("참조 연결");
            DrawSectionHelp("EnemySpawner 연결과 기존 Stage Rules 중복 실행 여부를 정합니다.");
            DrawProperty("enemySpawner", "몬스터 스포너 연결");
            DrawProperty("disableSpawnerStageRulesUpdate", "기존 스폰 규칙 중지");

            DrawSectionTitle("스테이지 진행 설정");
            DrawSectionHelp("스테이지 시간과 반복 스폰 간격을 정합니다.");
            DrawProperty("stageDurationSeconds", "스테이지 시간 (초)");
            DrawProperty("firstSpawnDelay", "첫 스폰 대기 (초)");
            DrawProperty("baseSpawnInterval", "기본 스폰 간격 (초)");
            DrawProperty("minSpawnInterval", "최소 스폰 간격 (초)");
            DrawProperty("intervalReductionPerStage", "스테이지당 간격 감소 (초)");

            DrawSectionTitle("조합풀 선택 설정");
            DrawSectionHelp("몇 스테이지부터 조합풀을 더 많이 섞을지 정합니다.");
            DrawProperty("secondPoolThreatLevel", "두 번째 조합 시작 스테이지");
            DrawProperty("thirdPoolThreatLevel", "세 번째 조합 시작 스테이지");
            DrawProperty("maxPoolsPerSpawn", "한 번에 섞을 최대 조합 수");

            DrawMonsterPools();

            DrawSectionTitle("게이트 방향 설정");
            DrawSectionHelp("스테이지가 오를수록 몇 방향 게이트를 랜덤으로 사용할지 정합니다.");
            DrawProperty("baseGateDirectionCount", "초반 사용 게이트 방향 수");
            DrawProperty("midGateDirectionThreatLevel", "중반 게이트 시작 스테이지");
            DrawProperty("midGateDirectionCount", "중반 사용 게이트 방향 수");
            DrawProperty("lateGateDirectionThreatLevel", "후반 게이트 시작 스테이지");
            DrawProperty("lateGateDirectionCount", "후반 사용 게이트 방향 수");
            DrawProperty("fullGateDirectionThreatLevel", "최종 게이트 시작 스테이지");
            DrawProperty("fullGateDirectionCount", "최종 사용 게이트 방향 수");

            DrawSectionTitle("특수 웨이브 자동 설정");
            DrawSectionHelp("고정 스테이지가 아니라 최소 등장 스테이지, 확률, 재등장 대기로 Elite/Boss Wave를 판정합니다.");
            DrawProperty("enableEliteWave", "엘리트 웨이브 사용");
            DrawProperty("eliteWaveStartStage", "엘리트 최소 등장 스테이지");
            DrawPercentProperty("eliteBaseChance", "엘리트 기본 등장 확률");
            DrawPercentProperty("eliteChanceIncreasePerMiss", "엘리트 확률 증가량");
            DrawPercentProperty("eliteMaxChance", "엘리트 최대 등장 확률");
            DrawProperty("eliteWaveInterval", "엘리트 재등장 대기 스테이지");
            DrawProperty("enableBossWave", "보스 웨이브 사용");
            DrawProperty("bossWaveStartStage", "보스 최소 등장 스테이지");
            DrawPercentProperty("bossBaseChance", "보스 기본 등장 확률");
            DrawPercentProperty("bossChanceIncreasePerMiss", "보스 확률 증가량");
            DrawPercentProperty("bossMaxChance", "보스 최대 등장 확률");
            DrawProperty("bossWaveInterval", "보스 재등장 대기 스테이지");
            DrawProperty("specialWaveSpawnRules", "특수 웨이브 직접 스폰 규칙");

            DrawSectionTitle("보너스 상자 웨이브 설정");
            DrawSectionHelp("특수웨이브 몬스터가 모두 정리된 뒤 보너스 상자 웨이브를 생성할지 정합니다.");
            DrawProperty("enableBonusChestAfterSpecialWave", "특수웨이브 클리어 후 상자 생성");
            DrawProperty("bonusChestWaveSpawner", "보너스 상자 스포너 연결");

            DrawSectionTitle("특수 웨이브 수량 증가 설정");
            DrawSectionHelp("기준 스테이지마다 특수웨이브 수량 배율을 올립니다. 예: 0.5면 1배 → 1.5배 → 2배");
            DrawProperty("enableSpecialWaveCountScaling", "특수 웨이브 수량 자동 증가 사용");
            DrawProperty("specialWaveCountStageStep", "수량 증가 기준 스테이지");
            DrawProperty("specialWaveCountIncreasePerStep", "단계마다 추가 배율");
            DrawProperty("specialWaveMaxCountMultiplier", "수량 최대 배율");

            serializedObject.ApplyModifiedProperties(); // Inspector에서 바꾼 값을 실제 SerializedObject에 반영한다.
        }

        private void DrawScriptField() // 기본 Inspector처럼 Script 칸을 읽기 전용으로 보여준다.
        {
            using (new EditorGUI.DisabledScope(true))
            {
                MonoScript script = MonoScript.FromMonoBehaviour((WaveController)target);
                EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            }
        }

        private void DrawSectionTitle(string title) // 섹션 제목을 굵게 표시한다.
        {
            EditorGUILayout.Space(10.0f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private void DrawSectionHelp(string helpText) // 섹션 전체에 대한 짧은 설명을 표시한다.
        {
            EditorGUILayout.HelpBox(helpText, MessageType.Info);
        }

        private void DrawProperty(string propertyName, string label) // 한 항목을 표시한다.
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                EditorGUILayout.HelpBox($"{propertyName} 항목을 찾을 수 없습니다. WaveController 필드 이름이 바뀌었는지 확인하세요.", MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private void DrawPercentProperty(string propertyName, string label) // 0~1 값을 Inspector에서는 0~100%로 보여준다.
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                EditorGUILayout.HelpBox($"{propertyName} 항목을 찾을 수 없습니다. WaveController 필드 이름이 바뀌었는지 확인하세요.", MessageType.Warning);
                return;
            }

            float percentValue = Mathf.Clamp01(property.floatValue) * 100.0f;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                percentValue = EditorGUILayout.Slider(percentValue, 0.0f, 100.0f);
                EditorGUILayout.LabelField("%", GUILayout.Width(16.0f));
            }

            property.floatValue = Mathf.Clamp01(percentValue / 100.0f);
        }

        private void DrawMonsterPools() // 조합풀 배열을 P01 같은 이름으로 접어 볼 수 있게 표시한다.
        {
            SerializedProperty monsterPools = serializedObject.FindProperty("monsterPools");

            if (monsterPools == null)
            {
                EditorGUILayout.HelpBox("monsterPools 항목을 찾을 수 없습니다. WaveController 필드 이름이 바뀌었는지 확인하세요.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("P10까지 기본 Pool 채우기")) // 기존 오브젝트의 Pool 배열을 P10까지 확장한다.
            {
                FillDefaultPoolsToP10(monsterPools);
            }

            monsterPools.isExpanded = EditorGUILayout.Foldout(monsterPools.isExpanded, $"Monster Pools ({monsterPools.arraySize})", true);

            if (!monsterPools.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", monsterPools.arraySize));

            if (newSize != monsterPools.arraySize)
            {
                monsterPools.arraySize = newSize;
            }

            for (int i = 0; i < monsterPools.arraySize; i++)
            {
                SerializedProperty pool = monsterPools.GetArrayElementAtIndex(i);
                DrawMonsterPoolElement(pool, i);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("+", GUILayout.Width(28.0f)))
                {
                    monsterPools.arraySize++;
                }

                using (new EditorGUI.DisabledScope(monsterPools.arraySize <= 0))
                {
                    if (GUILayout.Button("-", GUILayout.Width(28.0f)))
                    {
                        monsterPools.DeleteArrayElementAtIndex(monsterPools.arraySize - 1);
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawMonsterPoolElement(SerializedProperty pool, int index) // Monster Pool 하나를 P01 - 이름 형태로 표시한다.
        {
            string label = GetMonsterPoolLabel(pool, index);

            pool.isExpanded = EditorGUILayout.Foldout(pool.isExpanded, label, true);

            if (!pool.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            SerializedProperty child = pool.Copy();
            SerializedProperty end = pool.GetEndProperty();
            bool enterChildren = true;

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                EditorGUILayout.PropertyField(child, true);
                enterChildren = false;
            }

            EditorGUI.indentLevel--;
        }

        private static string GetMonsterPoolLabel(SerializedProperty pool, int index) // 접힌 상태에서 보일 Pool 제목을 만든다.
        {
            SerializedProperty poolId = pool.FindPropertyRelative("poolId");
            SerializedProperty displayName = pool.FindPropertyRelative("displayName");

            string id = poolId != null ? poolId.stringValue : string.Empty;
            string name = displayName != null ? displayName.stringValue : string.Empty;

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
            {
                return $"{id} - {name}";
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return $"Element {index}";
        }
        private void FillDefaultPoolsToP10(SerializedProperty monsterPools) // P09/P10 특수 Pool을 빠르게 추가하기 위한 보조 기능
        {
            if (monsterPools.arraySize < 10)
            {
                monsterPools.arraySize = 10;
            }

            SetupPool(monsterPools.GetArrayElementAtIndex(8), "P09", "엘리트 웨이브", WaveController.SpecialWaveType.Elite, 10, 100, 1, 3, false, 2);
            SetupPool(monsterPools.GetArrayElementAtIndex(9), "P10", "보스 웨이브", WaveController.SpecialWaveType.Boss, 20, 100, 1, 1, false, 1);

            serializedObject.ApplyModifiedProperties();
        }

        private void SetupPool(
            SerializedProperty pool,
            string poolId,
            string displayName,
            WaveController.SpecialWaveType waveType,
            int minThreatLevel,
            int weight,
            int spawnGroupCount,
            int frontRowCount,
            bool alwaysIncludeAsBasePool,
            int entryCount) // Pool 하나의 기본값을 설정한다.
        {
            pool.FindPropertyRelative("waveType").enumValueIndex = (int)waveType;
            pool.FindPropertyRelative("poolId").stringValue = poolId;
            pool.FindPropertyRelative("displayName").stringValue = displayName;
            pool.FindPropertyRelative("minThreatLevel").intValue = minThreatLevel;
            pool.FindPropertyRelative("weight").intValue = weight;
            pool.FindPropertyRelative("spawnGroupCount").intValue = spawnGroupCount;
            pool.FindPropertyRelative("frontRowCount").intValue = frontRowCount;
            pool.FindPropertyRelative("alwaysIncludeAsBasePool").boolValue = alwaysIncludeAsBasePool;

            SerializedProperty entries = pool.FindPropertyRelative("entries");
            entries.arraySize = entryCount;

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("prefab").objectReferenceValue = null;
                entry.FindPropertyRelative("count").intValue = 1;
            }
        }
    }
}
