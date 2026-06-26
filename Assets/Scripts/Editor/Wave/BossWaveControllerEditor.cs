using UnityEditor;
using UnityEngine;

namespace TeamProject01.Gameplay.EditorTools
{
    [CustomEditor(typeof(BossWaveController))]
    public sealed class BossWaveControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            WaveInspectorUtility.DrawScriptField(target);

            WaveInspectorUtility.DrawSection("참조");
            WaveInspectorUtility.DrawProperty(serializedObject, "enemySpawner", "몬스터 스포너");
            WaveInspectorUtility.DrawProperty(serializedObject, "bonusChestWaveSpawner", "보너스 상자 스포너");

            WaveInspectorUtility.DrawSection("보스 진행 설정", "보스는 등록된 순서대로 등장합니다.");
            WaveInspectorUtility.DrawProperty(serializedObject, "enableBossWave", "보스 웨이브 사용");
            WaveInspectorUtility.DrawProperty(serializedObject, "bossStartStage", "보스 시작 Stage");
            WaveInspectorUtility.DrawProperty(serializedObject, "bossIntervalStage", "보스 재등장 대기 Stage");
            WaveInspectorUtility.DrawProperty(serializedObject, "blockAdditionalBossWhileAlive", "보스 생존 중 추가 등장 금지");
            WaveInspectorUtility.DrawProperty(serializedObject, "spawnChestAfterBossClear", "보스 처치 후 상자 생성");

            WaveInspectorUtility.DrawSection("보스 Stage 규칙", "보스 Stage에서는 기존 몬스터는 남기고, 새 일반/엘리트 스폰만 멈출 수 있습니다.");
            WaveInspectorUtility.DrawProperty(serializedObject, "pauseNormalSpawnWhileBossAlive", "보스 Stage 중 새 일반 스폰 중지");
            WaveInspectorUtility.DrawProperty(serializedObject, "endBossStageOnBossClear", "보스 Stage는 처치 시 종료");

            DrawBossSequence();

            WaveInspectorUtility.DrawSection("확장 설정", "나중에 보스 종류가 부족할 때 조합 보스로 이어갈 자리입니다.");
            WaveInspectorUtility.DrawProperty(serializedObject, "enableBossCombination", "보스 조합 사용");

            if (!serializedObject.FindProperty("enableBossCombination").boolValue)
            {
                EditorGUILayout.HelpBox("현재는 잠금 상태입니다. 보스가 더 필요해지면 이 옵션을 켜고 확장하면 됩니다.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBossSequence()
        {
            WaveInspectorUtility.DrawSection("보스 등장 순서");
            SerializedProperty bosses = serializedObject.FindProperty("bossSequence");
            WaveInspectorUtility.DrawArray(
                bosses,
                "보스 목록",
                (element, index) => WaveInspectorUtility.GetIdNameLabel(element, "bossId", "displayName", index),
                DrawBossBody,
                "+ 보스 추가",
                "- 마지막 보스 삭제");
        }

        private static void DrawBossBody(SerializedProperty boss)
        {
            EditorGUILayout.PropertyField(boss.FindPropertyRelative("bossId"), new GUIContent("보스 ID"));
            EditorGUILayout.PropertyField(boss.FindPropertyRelative("displayName"), new GUIContent("보스 이름"));
            EditorGUILayout.PropertyField(boss.FindPropertyRelative("prefab"), new GUIContent("보스 Prefab"));
        }
    }
}
