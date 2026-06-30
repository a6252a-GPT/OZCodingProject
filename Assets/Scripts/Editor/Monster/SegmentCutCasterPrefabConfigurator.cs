using TeamProject01.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TeamProject01.EditorTools
{
    public static class SegmentCutCasterPrefabConfigurator
    {
        private const string PrefabPath = "Assets/Prefabs/Monster/EnemyPrefab/Enemy_Elite_SegmentCutCaster.prefab";
        private const float FirstCastDelay = 5.0f;
        private const float CastInterval = 15.0f;

        [MenuItem("OZCodingProject/Monsters/Apply Segment Cut Caster Settings")]
        public static void ApplyFromMenu()
        {
            bool changed = ApplySettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"SegmentCutCasterPrefabConfigurator changed={changed}");
        }

        public static void RunOnceFromCommandLine()
        {
            bool changed = ApplySettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"SegmentCutCasterPrefabConfigurator changed={changed}");
        }

        private static bool ApplySettings()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                EnemySegmentCutCaster caster = root.GetComponentInChildren<EnemySegmentCutCaster>(true);

                if (caster == null)
                {
                    Debug.LogError($"EnemySegmentCutCaster not found in {PrefabPath}");
                    return false;
                }

                SerializedObject serializedObject = new SerializedObject(caster);
                bool changed = false;

                changed |= SetFloat(serializedObject, "firstCastDelay", FirstCastDelay);
                changed |= SetFloat(serializedObject, "castInterval", CastInterval);

                if (changed)
                {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }

                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError($"Property {propertyName} not found.");
                return false;
            }

            if (Mathf.Approximately(property.floatValue, value))
            {
                return false;
            }

            property.floatValue = value;
            return true;
        }
    }
}
