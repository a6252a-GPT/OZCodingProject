// 안건준 추가 - 0623
// Tools > OZ > Auto-Assign Weapon Card Icons 메뉴 실행 시
// 모든 WeaponDefinition + SegmentDefinition 에셋에 레벨별 스프라이트를 자동 할당

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TeamProject01.Gameplay;

public static class WeaponCardIconAssigner
{
    private const string IconFolderPath = "Assets/UI/Card/CardDetailImage/WeaponCardIcons";
    private const int MaxLevelSearch = 10; // 최대 레벨 탐색 수

    [MenuItem("Tools/OZ/Auto-Assign Weapon Card Icons")]
    public static void AssignIcons()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponDefinition");
        int assignedCount = 0;
        int skippedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            WeaponDefinition def = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(assetPath);

            if (def == null || string.IsNullOrWhiteSpace(def.TargetSegmentId))
            {
                skippedCount++;
                continue;
            }

            string segmentId = def.TargetSegmentId.Trim();

            // 레벨별 스프라이트 수집 (Lv1 ~ MaxLevelSearch까지 존재하는 것만)
            List<Sprite> levelSprites = new List<Sprite>();
            for (int lv = 1; lv <= MaxLevelSearch; lv++)
            {
                Sprite s = FindSprite($"{segmentId}_Lv{lv}");
                if (s == null)
                {
                    break; // 해당 레벨 없으면 중단
                }

                levelSprites.Add(s);
            }

            if (levelSprites.Count == 0)
            {
                Debug.LogWarning($"[WeaponCardIconAssigner] 스프라이트 없음: '{segmentId}_Lv1' ~ (WeaponDefinition: {def.name})", def);
                skippedCount++;
                continue;
            }

            bool dirty = false;

            // CardIconSprite (Lv1 fallback) 할당
            if (def.CardIconSprite != levelSprites[0])
            {
                def.CardIconSprite = levelSprites[0];
                dirty = true;
            }

            // CardIconSpritesPerLevel 배열 할당
            Sprite[] newArray = levelSprites.ToArray();
            bool arrayChanged = def.CardIconSpritesPerLevel == null || def.CardIconSpritesPerLevel.Length != newArray.Length;
            if (!arrayChanged)
            {
                for (int i = 0; i < newArray.Length; i++)
                {
                    if (def.CardIconSpritesPerLevel[i] != newArray[i])
                    {
                        arrayChanged = true;
                        break;
                    }
                }
            }

            if (arrayChanged)
            {
                def.CardIconSpritesPerLevel = newArray;
                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(def);
                assignedCount++;
                Debug.Log($"[WeaponCardIconAssigner] 할당 완료: {def.name} → Lv1~{levelSprites.Count} ({string.Join(", ", levelSprites.ConvertAll(s => s.name))})", def);
            }
        }

        // SegmentDefinition 에셋도 자동 할당
        string[] segGuids = AssetDatabase.FindAssets("t:SegmentDefinition");
        int segAssigned = 0;

        foreach (string guid in segGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            SegmentDefinition def = AssetDatabase.LoadAssetAtPath<SegmentDefinition>(assetPath);

            if (def == null || string.IsNullOrWhiteSpace(def.SegmentId))
            {
                continue;
            }

            string segmentId = def.SegmentId.Trim();
            List<Sprite> levelSprites = new List<Sprite>();
            for (int lv = 1; lv <= MaxLevelSearch; lv++)
            {
                Sprite s = FindSprite($"{segmentId}_Lv{lv}");
                if (s == null)
                {
                    break;
                }

                levelSprites.Add(s);
            }

            if (levelSprites.Count == 0)
            {
                continue; // 해당 세그먼트 아이콘 없음 (정상)
            }

            Sprite[] newArray = levelSprites.ToArray();
            bool arrayChanged = def.CardIconSpritesPerLevel == null || def.CardIconSpritesPerLevel.Length != newArray.Length;
            if (!arrayChanged)
            {
                for (int i = 0; i < newArray.Length; i++)
                {
                    if (def.CardIconSpritesPerLevel[i] != newArray[i])
                    {
                        arrayChanged = true;
                        break;
                    }
                }
            }

            if (arrayChanged)
            {
                def.CardIconSpritesPerLevel = newArray;
                EditorUtility.SetDirty(def);
                segAssigned++;
                Debug.Log($"[WeaponCardIconAssigner] SegmentDef 할당: {def.name} → Lv1~{levelSprites.Count}", def);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Weapon Card Icons 자동 할당",
            $"완료!\nWeaponDefinition 할당: {assignedCount}개 / 건너뜀: {skippedCount}개\nSegmentDefinition 할당: {segAssigned}개",
            "확인");
    }

    private static Sprite FindSprite(string spriteName)
    {
        // 1차: t:Sprite 로 검색
        string[] spriteGuids = AssetDatabase.FindAssets($"{spriteName} t:Sprite", new[] { IconFolderPath });
        foreach (string guid in spriteGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != spriteName)
            {
                continue; // 이름 정확히 일치해야 함
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
        }

        // 2차: t:Texture2D 로 검색 후 Sprite 변환 시도
        string[] texGuids = AssetDatabase.FindAssets($"{spriteName} t:Texture2D", new[] { IconFolderPath });
        foreach (string guid in texGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != spriteName)
            {
                continue;
            }

            // PNG가 Sprite 모드로 임포트 됐을 경우 서브에셋에서 Sprite 추출
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in allAssets)
            {
                if (asset is Sprite s)
                {
                    return s;
                }
            }

            // 텍스처를 스프라이트로 직접 변환
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                // Import Settings를 Sprite 모드로 변경
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    Debug.Log($"[WeaponCardIconAssigner] 텍스처 임포트 설정 변경 (Sprite 모드): {path}");
                }

                Sprite converted = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (converted != null)
                {
                    return converted;
                }
            }
        }

        return null;
    }
}
