using System;
using System.Collections.Generic;
using System.IO;
using TeamProject01.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TeamProject01.EditorTools
{
    public static class StarterSegmentFormalizer
    {
        private const string StarterCatalogPath = "Assets/Segments/_Catalog/StarterCatalog.asset";
        private const string StarterRoot = "Assets/Segments/Starter";
        private const string StarterResourceRoot = "Assets/Resources/StarterSegments";
        private static readonly string[] SharedWeaponAuxiliaryRootNames =
        {
            "Muzzles",
            "LoadedProjectiles",
            "MissileList",
            "MisslieList"
        };

        [MenuItem("OZCodingProject/Segments/Formalize Starter Segments")]
        public static void RunFromMenu()
        {
            Debug.Log(Run());
        }

        public static void RunOnceFromCommandLine()
        {
            bool failed = false;
            try
            {
                Debug.Log(Run());
            }
            catch (Exception exception)
            {
                failed = true;
                Debug.LogException(exception);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(failed ? 1 : 0);
            }
        }

        private static string Run()
        {
            List<string> report = new List<string> { "[Starter Formalizer]" };
            StarterSpec[] specs = CreateSpecs();
            EnsureFolder(StarterRoot);
            EnsureFolder(StarterResourceRoot);

            Dictionary<string, GameObject> levelOnePrefabsByWorm = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < specs.Length; i++)
            {
                FormalizeStarter(specs[i], report, levelOnePrefabsByWorm);
            }

            UpdateStarterCatalog(specs, levelOnePrefabsByWorm, report);
            NormalizeSharedSegmentPrefabLevels(specs, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            report.Add("Done.");
            return string.Join("\n", report);
        }

        private static StarterSpec[] CreateSpecs()
        {
            return new[]
            {
                new StarterSpec(
                    "worm_basic",
                    "SG00_StarterCannon",
                    "SG01_Cannon",
                    "",
                    "Assets/Segments/Starter/SG00_StarterCannon/SG00_StarterCannon.asset",
                    "Assets/Segments/SG01_Cannon/SG01_Cannon.asset",
                    "Assets/Segments/Starter/SG00_StarterCannon/Prefabs/SG00_StarterCannon.prefab",
                    "Assets/Segments/_Common/Bodies/SegmentBody_StarterCannon.prefab",
                    "Assets/Resources/StarterSegments/SG00_StarterCannon.prefab"),
                new StarterSpec(
                    "worm_attack",
                    "SG00_StarterAttack",
                    "SG02_Missile",
                    "AttackWormStarter",
                    "Assets/Segments/Starter/SG00_StarterAttack/SG00_StarterAttack.asset",
                    "Assets/Segments/SG02_Missile/SG02_Missile.asset",
                    "Assets/Segments/Starter/SG00_StarterAttack/Prefabs/SG00_StarterAttack.prefab",
                    "Assets/Segments/_Common/Bodies/SegmentBody_AttackWormStarter.prefab",
                    "Assets/Resources/StarterSegments/SG00_StarterAttack.prefab"),
                new StarterSpec(
                    "worm_mobility",
                    "SG00_StarterMobility",
                    "SG04_SawLauncher",
                    "MobilityWormStarter",
                    "Assets/Segments/Starter/SG00_StarterMobility/SG00_StarterMobility.asset",
                    "Assets/Segments/SG04_SawLauncher/SG04_SawLauncher.asset",
                    "Assets/Segments/Starter/SG00_StarterMobility/Prefabs/SG00_StarterMobility.prefab",
                    "Assets/Segments/_Common/Bodies/SegmentBody_MobilityWormStarter.prefab",
                    "Assets/Resources/StarterSegments/SG00_StarterMobility.prefab"),
                new StarterSpec(
                    "worm_support",
                    "SG00_StarterSupport",
                    "SG21_FireballTower",
                    "SupportWormStarter",
                    "Assets/Segments/Starter/SG00_StarterSupport/SG00_StarterSupport.asset",
                    "Assets/Segments/SG21_FireballTower/SG21_FireballTower.asset",
                    "Assets/Segments/Starter/SG00_StarterSupport/Prefabs/SG00_StarterSupport.prefab",
                    "Assets/Segments/_Common/Bodies/SegmentBody_SupportWormStarter.prefab",
                    "Assets/Resources/StarterSegments/SG00_StarterSupport.prefab"),
                new StarterSpec(
                    "worm_magic",
                    "SG00_StarterMagic",
                    "SG20_LightningObelisk",
                    "MagicWormStarter",
                    "Assets/Segments/Starter/SG00_StarterMagic/SG00_StarterMagic.asset",
                    "Assets/Segments/SG20_LightningObelisk/SG20_LightningObelisk.asset",
                    "Assets/Segments/Starter/SG00_StarterMagic/Prefabs/SG00_StarterMagic.prefab",
                    "Assets/Segments/_Common/Bodies/SegmentBody_MagicWormStarter.prefab",
                    "Assets/Resources/StarterSegments/SG00_StarterMagic.prefab")
            };
        }

        private static void FormalizeStarter(
            StarterSpec spec,
            List<string> report,
            Dictionary<string, GameObject> levelOnePrefabsByWorm)
        {
            SegmentDefinition starterDefinition = AssetDatabase.LoadAssetAtPath<SegmentDefinition>(spec.StarterDefinitionPath);
            SegmentDefinition sharedDefinition = AssetDatabase.LoadAssetAtPath<SegmentDefinition>(spec.SharedDefinitionPath);
            if (starterDefinition == null || sharedDefinition == null)
            {
                report.Add("Missing definition: " + spec.StarterId);
                return;
            }

            MoveLegacyFolders(spec, report);
            EnsureFolder(spec.PrefabFolder);

            int maxLevel = Mathf.Max(1, sharedDefinition.MaxLevel);
            SegmentLevelDefinition[] levels = new SegmentLevelDefinition[maxLevel];
            for (int level = 1; level <= maxLevel; level++)
            {
                if (!sharedDefinition.TryGetLevel(level, out SegmentLevelDefinition sharedLevel))
                {
                    report.Add("Missing shared level: " + spec.SharedSegmentId + " Lv" + level);
                    continue;
                }

                string outputPath = spec.GetFormalPrefabPath(level);
                GameObject savedPrefab = BuildStarterLevelPrefab(spec, sharedLevel, level, outputPath, report);
                if (savedPrefab == null)
                {
                    continue;
                }

                levels[level - 1] = new SegmentLevelDefinition
                {
                    Level = level,
                    SegmentPrefab = savedPrefab,
                    BodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.BodyPrefabPath),
                    HeadPrefab = sharedLevel.HeadPrefab,
                    AttackProfile = sharedLevel.AttackProfile,
                    Memo = spec.SharedSegmentId + " Lv" + level + " starter full prefab"
                };

                if (level == 1)
                {
                    levelOnePrefabsByWorm[spec.WormId] = savedPrefab;
                    SaveResourceFallback(spec, outputPath, report);
                }
            }

            starterDefinition.StarterOnly = true;
            starterDefinition.SharedUpgradeSegmentId = spec.SharedSegmentId;
            starterDefinition.UseLevels = true;
            starterDefinition.MaxLevel = maxLevel;
            starterDefinition.CanAddByLevelChoice = false;
            starterDefinition.CanUpgradeByLevelChoice = false;
            starterDefinition.Levels = levels;
            EditorUtility.SetDirty(starterDefinition);
            report.Add("Definition: " + spec.StarterId + " -> " + spec.SharedSegmentId + " Lv1-" + maxLevel);
        }

        private static GameObject BuildStarterLevelPrefab(StarterSpec spec, SegmentLevelDefinition sharedLevel, int level, string outputPath, List<string> report)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.SourcePrefabPath);
            if (sourcePrefab == null)
            {
                report.Add("Missing source starter prefab: " + spec.SourcePrefabPath);
                return null;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(spec.SourcePrefabPath);
            try
            {
                root.name = spec.StarterId + "_Lv" + level;
                ConfigureRuntime(root, spec.SharedSegmentId, sharedLevel, level);
                ReplaceHead(root.transform, sharedLevel.HeadPrefab, report);
                CopySharedWeaponAuxiliaryRoots(root.transform, sharedLevel.SegmentPrefab, report);
                ConfigureRuntime(root, spec.SharedSegmentId, sharedLevel, level);

                PrefabUtility.SaveAsPrefabAsset(root, outputPath);
                GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
                report.Add("Prefab: " + outputPath);
                return saved;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureRuntime(GameObject root, string sharedSegmentId, SegmentLevelDefinition sharedLevel, int level)
        {
            ConvoySegmentRuntime runtime = root.GetComponent<ConvoySegmentRuntime>();
            if (runtime != null)
            {
                runtime.SetSegmentLevel(level);
                runtime.Weapon = root.GetComponent<SegmentWeaponBehaviour>();
            }

            GenericSegmentWeapon weapon = root.GetComponent<GenericSegmentWeapon>();
            if (weapon == null)
            {
                return;
            }

            weapon.SegmentId = sharedSegmentId;
            weapon.AttackProfile = sharedLevel.AttackProfile;

            Transform headRoot = FindWeaponHeadRoot(root.transform, weapon);
            Transform searchRoot = headRoot != null ? headRoot : root.transform;
            weapon.HeadYawPivot = FindChildRecursive(searchRoot, "YawPivot");
            weapon.Muzzle = FindChildRecursive(searchRoot, "Muzzle");
            if (weapon.Muzzle == null)
            {
                weapon.Muzzle = FindFirstMuzzle(searchRoot);
            }

            Transform vfxRoot = weapon.Muzzle != null ? weapon.Muzzle : searchRoot;
            weapon.MuzzleVfxSocket = FindChildRecursive(vfxRoot, "VFX_Muzzle");
            if (weapon.MuzzleVfxSocket == null)
            {
                weapon.MuzzleVfxSocket = FindChildRecursive(vfxRoot, "MuzzleVFX");
            }

            weapon.LoadedProjectileRoot = FindChildRecursive(searchRoot, "LoadedProjectiles");
            if (weapon.LoadedProjectileRoot == null)
            {
                weapon.LoadedProjectileRoot = FindChildRecursive(searchRoot, "MissileList");
            }

            if (weapon.LoadedProjectileRoot == null)
            {
                weapon.LoadedProjectileRoot = FindChildRecursive(searchRoot, "MisslieList");
            }

            weapon.ProjectileMuzzles = CollectProjectileMuzzles(searchRoot);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(weapon);
            if (runtime != null)
            {
                EditorUtility.SetDirty(runtime);
            }
        }

        private static void ReplaceHead(Transform segmentRoot, GameObject headPrefab, List<string> report)
        {
            if (segmentRoot == null || headPrefab == null)
            {
                return;
            }

            GenericSegmentWeapon weapon = segmentRoot.GetComponent<GenericSegmentWeapon>();
            Transform oldHead = FindWeaponHeadRoot(segmentRoot, weapon);
            string headName = oldHead != null ? oldHead.name : headPrefab.name;
            int siblingIndex = oldHead != null ? oldHead.GetSiblingIndex() : segmentRoot.childCount;
            Vector3 localPosition = oldHead != null ? oldHead.localPosition : Vector3.zero;
            Quaternion localRotation = oldHead != null ? oldHead.localRotation : Quaternion.identity;
            Vector3 localScale = oldHead != null ? oldHead.localScale : Vector3.one;

            if (oldHead != null)
            {
                UnityEngine.Object.DestroyImmediate(oldHead.gameObject);
            }

            GameObject headInstance = PrefabUtility.InstantiatePrefab(headPrefab, segmentRoot) as GameObject;
            if (headInstance == null)
            {
                headInstance = UnityEngine.Object.Instantiate(headPrefab, segmentRoot, false);
            }
            else if (PrefabUtility.IsPartOfPrefabInstance(headInstance))
            {
                PrefabUtility.UnpackPrefabInstance(headInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            headInstance.name = headName;
            headInstance.transform.localPosition = localPosition;
            headInstance.transform.localRotation = localRotation;
            headInstance.transform.localScale = localScale;
            headInstance.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, segmentRoot.childCount - 1));
            ClearWeaponBindings(weapon);
            report.Add("  Head: " + segmentRoot.name + " <- " + headPrefab.name);
        }

        private static void ClearWeaponBindings(GenericSegmentWeapon weapon)
        {
            if (weapon == null)
            {
                return;
            }

            weapon.HeadYawPivot = null;
            weapon.Muzzle = null;
            weapon.ProjectileMuzzles = Array.Empty<Transform>();
            weapon.MuzzleVfxSocket = null;
            weapon.LoadedProjectileRoot = null;
            EditorUtility.SetDirty(weapon);
        }

        private static void CopySharedWeaponAuxiliaryRoots(Transform starterRoot, GameObject sharedSegmentPrefab, List<string> report)
        {
            if (starterRoot == null || sharedSegmentPrefab == null)
            {
                return;
            }

            string sharedPath = AssetDatabase.GetAssetPath(sharedSegmentPrefab);
            if (string.IsNullOrWhiteSpace(sharedPath))
            {
                return;
            }

            GameObject sharedRoot = PrefabUtility.LoadPrefabContents(sharedPath);
            try
            {
                for (int i = 0; i < SharedWeaponAuxiliaryRootNames.Length; i++)
                {
                    string childName = SharedWeaponAuxiliaryRootNames[i];
                    Transform sourceChild = FindChildRecursive(sharedRoot.transform, childName);
                    if (sourceChild == null)
                    {
                        continue;
                    }

                    Transform targetParent = ResolveAuxiliaryTargetParent(starterRoot, sharedRoot.transform, sourceChild);
                    Transform existingChild = FindDirectChild(targetParent, childName);
                    if (existingChild != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existingChild.gameObject);
                    }

                    GameObject clone = UnityEngine.Object.Instantiate(sourceChild.gameObject, targetParent, false);
                    clone.name = sourceChild.name;
                    clone.transform.localPosition = sourceChild.localPosition;
                    clone.transform.localRotation = sourceChild.localRotation;
                    clone.transform.localScale = sourceChild.localScale;
                    report.Add("  Aux: " + starterRoot.name + "/" + targetParent.name + " <- " + sharedSegmentPrefab.name + "/" + childName);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(sharedRoot);
            }
        }

        private static Transform ResolveAuxiliaryTargetParent(Transform starterRoot, Transform sharedRoot, Transform sourceChild)
        {
            if (starterRoot == null || sharedRoot == null || sourceChild == null || sourceChild.parent == null)
            {
                return starterRoot;
            }

            if (sourceChild.parent == sharedRoot)
            {
                return starterRoot;
            }

            Transform matchingParent = FindChildRecursive(starterRoot, sourceChild.parent.name);
            return matchingParent != null ? matchingParent : starterRoot;
        }

        private static void SaveResourceFallback(StarterSpec spec, string formalLevelOnePath, List<string> report)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(formalLevelOnePath);
            try
            {
                root.name = spec.StarterId;
                PrefabUtility.SaveAsPrefabAsset(root, spec.ResourcePrefabPath);
                report.Add("Resource fallback: " + spec.ResourcePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateStarterCatalog(
            StarterSpec[] specs,
            Dictionary<string, GameObject> levelOnePrefabsByWorm,
            List<string> report)
        {
            StarterCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<StarterCatalogAsset>(StarterCatalogPath);
            if (catalog == null)
            {
                report.Add("Missing starter catalog: " + StarterCatalogPath);
                return;
            }

            List<StarterSegmentEntry> entries = new List<StarterSegmentEntry>(catalog.Starters ?? Array.Empty<StarterSegmentEntry>());
            for (int i = 0; i < specs.Length; i++)
            {
                StarterSpec spec = specs[i];
                SegmentDefinition starterDefinition = AssetDatabase.LoadAssetAtPath<SegmentDefinition>(spec.StarterDefinitionPath);
                levelOnePrefabsByWorm.TryGetValue(spec.WormId, out GameObject levelOnePrefab);

                int index = FindStarterCatalogIndex(entries, spec.WormId);
                StarterSegmentEntry entry = index >= 0 ? entries[index] : default;
                entry.WormId = spec.WormId;
                entry.StarterDefinition = starterDefinition;
                entry.StarterPrefab = levelOnePrefab;
                entry.Memo = spec.StarterId + " formal Lv1 starter";

                if (index >= 0)
                {
                    entries[index] = entry;
                }
                else
                {
                    entries.Add(entry);
                }
            }

            catalog.Starters = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            report.Add("StarterCatalog refreshed.");
        }

        private static int FindStarterCatalogIndex(List<StarterSegmentEntry> entries, string wormId)
        {
            string normalized = MetaWormIds.Normalize(wormId);
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].NormalizedWormId, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void NormalizeSharedSegmentPrefabLevels(StarterSpec[] specs, List<string> report)
        {
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < specs.Length; i++)
            {
                if (!visited.Add(specs[i].SharedDefinitionPath))
                {
                    continue;
                }

                SegmentDefinition definition = AssetDatabase.LoadAssetAtPath<SegmentDefinition>(specs[i].SharedDefinitionPath);
                if (definition == null || definition.Levels == null)
                {
                    continue;
                }

                for (int j = 0; j < definition.Levels.Length; j++)
                {
                    SegmentLevelDefinition level = definition.Levels[j];
                    if (level.SegmentPrefab == null)
                    {
                        continue;
                    }

                    string prefabPath = AssetDatabase.GetAssetPath(level.SegmentPrefab);
                    if (SetPrefabRuntimeLevel(prefabPath, level.Level, out bool changed) && changed)
                    {
                        report.Add("Level fixed: " + prefabPath + " -> Lv" + level.Level);
                    }
                }
            }
        }

        private static bool SetPrefabRuntimeLevel(string prefabPath, int level, out bool changed)
        {
            changed = false;
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ConvoySegmentRuntime runtime = root.GetComponent<ConvoySegmentRuntime>();
                if (runtime == null)
                {
                    return false;
                }

                int safeLevel = Mathf.Max(1, level);
                if (runtime.SegmentLevel == safeLevel)
                {
                    return true;
                }

                runtime.SetSegmentLevel(safeLevel);
                EditorUtility.SetDirty(runtime);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                changed = true;
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void MoveLegacyFolders(StarterSpec spec, List<string> report)
        {
            if (string.IsNullOrWhiteSpace(spec.LegacyFolderName))
            {
                return;
            }

            MoveLegacySubFolder(spec, "Art", report);
            MoveLegacySubFolder(spec, "Materials", report);
        }

        private static void MoveLegacySubFolder(StarterSpec spec, string subFolder, List<string> report)
        {
            string source = StarterRoot + "/" + spec.LegacyFolderName + "/" + subFolder;
            string destination = StarterRoot + "/" + spec.StarterId + "/" + subFolder;
            if (!AssetDatabase.IsValidFolder(source) || AssetDatabase.IsValidFolder(destination))
            {
                return;
            }

            EnsureFolder(Path.GetDirectoryName(destination)?.Replace('\\', '/'));
            string error = AssetDatabase.MoveAsset(source, destination);
            if (string.IsNullOrEmpty(error))
            {
                report.Add("Moved folder: " + source + " -> " + destination);
            }
            else
            {
                report.Add("Move failed: " + source + " -> " + destination + " / " + error);
            }
        }

        private static Transform FindWeaponHeadRoot(Transform segment, GenericSegmentWeapon weapon)
        {
            Transform headRoot = FindDirectChildContaining(segment, weapon != null ? weapon.HeadYawPivot : null);
            if (headRoot != null)
            {
                return headRoot;
            }

            for (int i = 0; segment != null && i < segment.childCount; i++)
            {
                Transform child = segment.GetChild(i);
                if (FindChildRecursive(child, "YawPivot") != null
                    || FindChildRecursive(child, "Muzzle") != null
                    || FindFirstMuzzle(child) != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindDirectChildContaining(Transform root, Transform descendant)
        {
            if (root == null || descendant == null)
            {
                return null;
            }

            Transform current = descendant;
            while (current != null && current.parent != null && current.parent != root)
            {
                current = current.parent;
            }

            return current != null && current.parent == root ? current : null;
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindFirstMuzzle(Transform root)
        {
            Transform[] transforms = root != null ? root.GetComponentsInChildren<Transform>(true) : Array.Empty<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.name.StartsWith("Muzzle_", StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform[] CollectProjectileMuzzles(Transform root)
        {
            List<Transform> results = new List<Transform>();
            Transform muzzleRoot = FindChildRecursive(root, "Muzzles");
            if (muzzleRoot != null)
            {
                for (int i = 0; i < muzzleRoot.childCount; i++)
                {
                    Transform child = muzzleRoot.GetChild(i);
                    if (child != null && child.name.StartsWith("Muzzle_", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(child);
                    }
                }
            }

            if (results.Count == 0 && root != null)
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform candidate = transforms[i];
                    if (candidate != null && candidate.name.StartsWith("Muzzle_", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(candidate);
                    }
                }
            }

            results.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
            return results.ToArray();
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (string.IsNullOrWhiteSpace(assetFolder))
            {
                return;
            }

            string normalized = assetFolder.Replace('\\', '/');
            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private readonly struct StarterSpec
        {
            public readonly string WormId;
            public readonly string StarterId;
            public readonly string SharedSegmentId;
            public readonly string LegacyFolderName;
            public readonly string StarterDefinitionPath;
            public readonly string SharedDefinitionPath;
            public readonly string SourcePrefabPath;
            public readonly string BodyPrefabPath;
            public readonly string ResourcePrefabPath;
            public readonly string PrefabFolder;

            public StarterSpec(
                string wormId,
                string starterId,
                string sharedSegmentId,
                string legacyFolderName,
                string starterDefinitionPath,
                string sharedDefinitionPath,
                string sourcePrefabPath,
                string bodyPrefabPath,
                string resourcePrefabPath)
            {
                WormId = wormId;
                StarterId = starterId;
                SharedSegmentId = sharedSegmentId;
                LegacyFolderName = legacyFolderName;
                StarterDefinitionPath = starterDefinitionPath;
                SharedDefinitionPath = sharedDefinitionPath;
                SourcePrefabPath = sourcePrefabPath;
                BodyPrefabPath = bodyPrefabPath;
                ResourcePrefabPath = resourcePrefabPath;
                PrefabFolder = StarterRoot + "/" + starterId + "/Prefabs";
            }

            public string GetFormalPrefabPath(int level)
            {
                return PrefabFolder + "/" + StarterId + "_Lv" + Mathf.Max(1, level) + ".prefab";
            }
        }
    }
}
