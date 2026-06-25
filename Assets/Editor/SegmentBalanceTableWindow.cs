using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TeamProject01.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TeamProject01.EditorTools
{
    public sealed class SegmentBalanceTableWindow : EditorWindow
    {
        private const string WindowTitle = "세그먼트 밸런스 테이블";
        private const string MenuPath = "JC Tool/Balance/세그먼트 밸런스 테이블";
        private const string DefaultCatalogPath = "Assets/Segments/_Catalog/SegmentCatalog.asset";
        private const float RowHeight = 22f;
        private static readonly Color ChangedCellColor = new Color(1f, 0.82f, 0.32f, 1f);
        private static readonly SegmentAttackMoveType[] MoveTypeValues = (SegmentAttackMoveType[])Enum.GetValues(typeof(SegmentAttackMoveType));
        private static readonly SegmentAttackImpactType[] ImpactTypeValues = (SegmentAttackImpactType[])Enum.GetValues(typeof(SegmentAttackImpactType));

        private SegmentCatalogAsset catalog;
        private readonly List<RowState> rows = new List<RowState>(64);
        private Vector2 scroll;
        private string searchText = string.Empty;
        private bool showDirtyOnly;
        private int selectedMoveType = -1;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            SegmentBalanceTableWindow window = GetWindow<SegmentBalanceTableWindow>(WindowTitle);
            window.minSize = new Vector2(1280f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            if (catalog == null)
            {
                catalog = AssetDatabase.LoadAssetAtPath<SegmentCatalogAsset>(DefaultCatalogPath);
            }

            RefreshRows();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSummary();
            DrawTable();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    catalog = (SegmentCatalogAsset)EditorGUILayout.ObjectField("세그먼트 카탈로그", catalog, typeof(SegmentCatalogAsset), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        RefreshRows();
                    }

                    if (GUILayout.Button("새로고침", GUILayout.Width(90f)))
                    {
                        RefreshRows();
                    }

                    GUI.enabled = CountDirtyRows() > 0;
                    if (GUILayout.Button("변경 전체 적용", GUILayout.Width(115f)))
                    {
                        ApplyAllDirtyRows();
                    }

                    if (GUILayout.Button("전체 리셋", GUILayout.Width(90f)))
                    {
                        ResetAllRows();
                    }

                    GUI.enabled = true;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    searchText = EditorGUILayout.TextField("검색", searchText ?? string.Empty);
                    showDirtyOnly = GUILayout.Toggle(showDirtyOnly, "변경만 보기", EditorStyles.toolbarButton, GUILayout.Width(90f));
                    selectedMoveType = EditorGUILayout.Popup("공격 방식 필터", selectedMoveType + 1, BuildMoveTypeFilterLabels(), GUILayout.Width(260f)) - 1;
                }

                EditorGUILayout.HelpBox(
                    "DPS는 SegmentAttackProfile 원본값 기준 계산입니다. 코어 스탯, 성장 강화 누적값, 지원형 버프, 쿨타임 ±10% 랜덤은 포함하지 않습니다.",
                    MessageType.Info);
            }
        }

        private void DrawSummary()
        {
            int dirty = CountDirtyRows();
            EditorGUILayout.LabelField($"행 {rows.Count}개 / 변경 {dirty}개 / 로그 위치: {GetLogDirectory()}");
        }

        private void DrawTable()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, true, true);
            DrawHeader();

            for (int i = 0; i < rows.Count; i++)
            {
                RowState row = rows[i];
                if (!ShouldShowRow(row))
                {
                    continue;
                }

                DrawRow(row);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                Header("ID", 110f);
                Header("이름", 120f);
                Header("레벨", 42f);
                Header("프로필", 170f);
                Header("공격 방식", 145f);
                Header("피해 방식", 120f);
                Header("피해", 58f);
                Header("쿨타임", 60f);
                Header("사거리", 60f);
                Header("발사 수", 54f);
                Header("순차", 42f);
                Header("발사 간격", 70f);
                Header("속도", 58f);
                Header("관통", 50f);
                Header("폭발 반경", 70f);
                Header("레이저 시간", 78f);
                Header("틱 간격", 62f);
                Header("체인 단계", 70f);
                Header("체인 분기", 70f);
                Header("체인 유지율", 78f);
                Header("톱날 관통", 78f);
                Header("단일 DPS", 76f);
                Header("10마리 DPS", 86f);
                Header("작업", 154f);
            }
        }

        private void DrawRow(RowState row)
        {
            bool hasProfile = row.Profile != null;
            DpsResult dps = hasProfile ? DpsCalculator.Calculate(row.Working) : default;
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));
            if (row.IsDirty)
            {
                EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, Mathf.Max(position.width, 1500f), RowHeight), new Color(1f, 0.72f, 0.15f, 0.11f));
            }

            Label(row.SegmentId, 110f);
            Label(row.DisplayName, 120f);
            Label(row.Level > 0 ? row.Level.ToString(CultureInfo.InvariantCulture) : "-", 42f);

            if (GUILayout.Button(hasProfile ? row.Profile.name : "프로필 없음", EditorStyles.linkLabel, GUILayout.Width(170f)))
            {
                if (hasProfile)
                {
                    Selection.activeObject = row.Profile;
                    EditorGUIUtility.PingObject(row.Profile);
                }
            }

            GUI.enabled = hasProfile;
            row.Working.MoveType = MoveTypeCell(row.Working.MoveType, row.Original.MoveType, 145f);
            row.Working.ImpactType = ImpactTypeCell(row.Working.ImpactType, row.Original.ImpactType, 120f);
            row.Working.BaseDamage = FloatCell(row.Working.BaseDamage, row.Original.BaseDamage, 58f);
            row.Working.Cooldown = FloatCell(row.Working.Cooldown, row.Original.Cooldown, 60f);
            row.Working.SearchRange = FloatCell(row.Working.SearchRange, row.Original.SearchRange, 60f);
            row.Working.ProjectileCount = IntCell(row.Working.ProjectileCount, row.Original.ProjectileCount, 54f);
            row.Working.FireProjectilesSequentially = BoolCell(row.Working.FireProjectilesSequentially, row.Original.FireProjectilesSequentially, 42f);
            row.Working.ProjectileFireDelay = FloatCell(row.Working.ProjectileFireDelay, row.Original.ProjectileFireDelay, 70f);
            row.Working.ProjectileSpeed = FloatCell(row.Working.ProjectileSpeed, row.Original.ProjectileSpeed, 58f);
            row.Working.PierceCount = IntCell(row.Working.PierceCount, row.Original.PierceCount, 50f);
            row.Working.ExplosionRadius = FloatCell(row.Working.ExplosionRadius, row.Original.ExplosionRadius, 70f);
            row.Working.LaserDuration = FloatCell(row.Working.LaserDuration, row.Original.LaserDuration, 78f);
            row.Working.LaserTickInterval = FloatCell(row.Working.LaserTickInterval, row.Original.LaserTickInterval, 62f);
            row.Working.MaxChainDepth = IntCell(row.Working.MaxChainDepth, row.Original.MaxChainDepth, 70f);
            row.Working.ChainBranchCount = IntCell(row.Working.ChainBranchCount, row.Original.ChainBranchCount, 70f);
            row.Working.ChainDamageFalloff = FloatCell(row.Working.ChainDamageFalloff, row.Original.ChainDamageFalloff, 78f);
            row.Working.SawPierceDamageRatio = FloatCell(row.Working.SawPierceDamageRatio, row.Original.SawPierceDamageRatio, 78f);
            GUI.enabled = true;

            Label(hasProfile ? FormatFloat(dps.SingleDps) : "-", 76f);
            Label(hasProfile ? FormatFloat(dps.Cluster10Dps) : "-", 86f);

            GUI.enabled = hasProfile && row.IsDirty;
            if (GUILayout.Button("적용", GUILayout.Width(48f)))
            {
                ApplyRow(row);
            }

            if (GUILayout.Button("리셋", GUILayout.Width(48f)))
            {
                row.ResetWorking();
            }

            GUI.enabled = hasProfile;
            if (GUILayout.Button("Ping", GUILayout.Width(48f)))
            {
                Selection.activeObject = row.Profile;
                EditorGUIUtility.PingObject(row.Profile);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshRows()
        {
            rows.Clear();
            if (catalog == null || catalog.Segments == null)
            {
                return;
            }

            for (int i = 0; i < catalog.Segments.Length; i++)
            {
                SegmentDefinition definition = catalog.Segments[i];
                if (definition == null)
                {
                    continue;
                }

                if (definition.Levels == null || definition.Levels.Length == 0)
                {
                    rows.Add(RowState.CreateMissing(definition, 0));
                    continue;
                }

                for (int levelIndex = 0; levelIndex < definition.Levels.Length; levelIndex++)
                {
                    SegmentLevelDefinition level = definition.Levels[levelIndex];
                    rows.Add(RowState.Create(definition, level));
                }
            }
        }

        private bool ShouldShowRow(RowState row)
        {
            if (showDirtyOnly && !row.IsDirty)
            {
                return false;
            }

            if (selectedMoveType >= 0)
            {
                if (row.Profile == null || (int)row.Working.MoveType != selectedMoveType)
                {
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string query = searchText.Trim();
            return Contains(row.SegmentId, query)
                || Contains(row.DisplayName, query)
                || (row.Profile != null && Contains(row.Profile.name, query));
        }

        private void ApplyRow(RowState row)
        {
            if (row.Profile == null || !row.IsDirty)
            {
                return;
            }

            BalanceValues before = row.Original.Clone();
            DpsResult beforeDps = DpsCalculator.Calculate(before);
            DpsResult afterDps = DpsCalculator.Calculate(row.Working);

            Undo.RecordObject(row.Profile, "Apply Segment Balance Row");
            row.Working.ApplyTo(row.Profile);
            EditorUtility.SetDirty(row.Profile);
            AssetDatabase.SaveAssets();

            WriteApplyLog("Apply Row", new List<RowLogEntry>
            {
                new RowLogEntry(row, before, row.Working.Clone(), beforeDps, afterDps)
            });

            row.Original = row.Working.Clone();
        }

        private void ApplyAllDirtyRows()
        {
            List<RowState> dirtyRows = new List<RowState>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Profile != null && rows[i].IsDirty)
                {
                    dirtyRows.Add(rows[i]);
                }
            }

            if (dirtyRows.Count == 0)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("변경 전체 적용", $"{dirtyRows.Count}개 행의 변경값을 실제 에셋에 적용할까요?", "적용", "취소"))
            {
                return;
            }

            List<RowLogEntry> logEntries = new List<RowLogEntry>(dirtyRows.Count);
            for (int i = 0; i < dirtyRows.Count; i++)
            {
                RowState row = dirtyRows[i];
                BalanceValues before = row.Original.Clone();
                DpsResult beforeDps = DpsCalculator.Calculate(before);
                DpsResult afterDps = DpsCalculator.Calculate(row.Working);

                Undo.RecordObject(row.Profile, "Apply Segment Balance Rows");
                row.Working.ApplyTo(row.Profile);
                EditorUtility.SetDirty(row.Profile);
                logEntries.Add(new RowLogEntry(row, before, row.Working.Clone(), beforeDps, afterDps));
                row.Original = row.Working.Clone();
            }

            AssetDatabase.SaveAssets();
            WriteApplyLog("Apply All", logEntries);
        }

        private void ResetAllRows()
        {
            if (CountDirtyRows() == 0)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("전체 리셋", "적용하지 않은 모든 변경값을 원본값으로 되돌릴까요?", "리셋", "취소"))
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].ResetWorking();
            }
        }

        private int CountDirtyRows()
        {
            int count = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].IsDirty)
                {
                    count++;
                }
            }

            return count;
        }

        private void WriteApplyLog(string action, List<RowLogEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            string directory = GetLogDirectory();
            Directory.CreateDirectory(directory);

            DateTime now = DateTime.Now;
            string fileName = $"SegmentBalance_{now:yyyy-MM-dd_HH-mm-ss-fff}.log";
            string path = Path.Combine(directory, fileName);

            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine($"[{now:yyyy-MM-dd HH:mm:ss}]");
            builder.AppendLine($"Action: {action}");
            builder.AppendLine($"ChangedRows: {entries.Count}");
            builder.AppendLine();

            for (int i = 0; i < entries.Count; i++)
            {
                RowLogEntry entry = entries[i];
                builder.AppendLine($"Segment: {entry.Row.SegmentId}");
                builder.AppendLine($"DisplayName: {entry.Row.DisplayName}");
                builder.AppendLine($"Level: {entry.Row.Level}");
                builder.AppendLine($"Profile: {AssetDatabase.GetAssetPath(entry.Row.Profile)}");
                AppendDiffs(builder, entry.Before, entry.After);
                builder.AppendLine($"SingleDPS: {FormatFloat(entry.BeforeDps.SingleDps)} -> {FormatFloat(entry.AfterDps.SingleDps)}");
                builder.AppendLine($"Cluster10DPS: {FormatFloat(entry.BeforeDps.Cluster10Dps)} -> {FormatFloat(entry.AfterDps.Cluster10Dps)}");
                builder.AppendLine();
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            Debug.Log($"[SegmentBalanceTable] Balance log written: {path}");
        }

        private static void AppendDiffs(StringBuilder builder, BalanceValues before, BalanceValues after)
        {
            AppendDiff(builder, "공격 방식", TranslateMoveType(before.MoveType), TranslateMoveType(after.MoveType));
            AppendDiff(builder, "피해 방식", TranslateImpactType(before.ImpactType), TranslateImpactType(after.ImpactType));
            AppendDiff(builder, "기본 피해", before.BaseDamage, after.BaseDamage);
            AppendDiff(builder, "쿨타임", before.Cooldown, after.Cooldown);
            AppendDiff(builder, "사거리", before.SearchRange, after.SearchRange);
            AppendDiff(builder, "발사 수", before.ProjectileCount, after.ProjectileCount);
            AppendDiff(builder, "순차 발사", before.FireProjectilesSequentially, after.FireProjectilesSequentially);
            AppendDiff(builder, "발사 간격", before.ProjectileFireDelay, after.ProjectileFireDelay);
            AppendDiff(builder, "투사체 속도", before.ProjectileSpeed, after.ProjectileSpeed);
            AppendDiff(builder, "관통 수", before.PierceCount, after.PierceCount);
            AppendDiff(builder, "폭발 반경", before.ExplosionRadius, after.ExplosionRadius);
            AppendDiff(builder, "레이저 지속 시간", before.LaserDuration, after.LaserDuration);
            AppendDiff(builder, "레이저 틱 간격", before.LaserTickInterval, after.LaserTickInterval);
            AppendDiff(builder, "체인 단계", before.MaxChainDepth, after.MaxChainDepth);
            AppendDiff(builder, "체인 분기", before.ChainBranchCount, after.ChainBranchCount);
            AppendDiff(builder, "체인 피해 유지율", before.ChainDamageFalloff, after.ChainDamageFalloff);
            AppendDiff(builder, "톱날 관통 피해 비율", before.SawPierceDamageRatio, after.SawPierceDamageRatio);
        }

        private static void AppendDiff<T>(StringBuilder builder, string label, T before, T after)
        {
            if (!EqualityComparer<T>.Default.Equals(before, after))
            {
                builder.AppendLine($"{label}: {before} -> {after}");
            }
        }

        private static void AppendDiff(StringBuilder builder, string label, float before, float after)
        {
            if (!Approximately(before, after))
            {
                builder.AppendLine($"{label}: {FormatFloat(before)} -> {FormatFloat(after)}");
            }
        }

        private static string GetLogDirectory()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string workspaceRoot = Directory.GetParent(projectRoot) != null ? Directory.GetParent(projectRoot).FullName : projectRoot;
            return Path.Combine(workspaceRoot, "OZCodingProject 개인파일", "Logs", "SegmentBalance");
        }

        private static string[] BuildMoveTypeFilterLabels()
        {
            string[] labels = new string[MoveTypeValues.Length + 1];
            labels[0] = "전체";
            for (int i = 0; i < MoveTypeValues.Length; i++)
            {
                labels[i + 1] = TranslateMoveType(MoveTypeValues[i]);
            }

            return labels;
        }

        private static void Header(string text, float width)
        {
            GUILayout.Label(text, EditorStyles.boldLabel, GUILayout.Width(width));
        }

        private static void Label(string text, float width)
        {
            GUILayout.Label(text, GUILayout.Width(width));
        }

        private static float FloatCell(float value, float original, float width)
        {
            Color previous = GUI.backgroundColor;
            if (!Approximately(value, original))
            {
                GUI.backgroundColor = ChangedCellColor;
            }

            float result = EditorGUILayout.FloatField(value, GUILayout.Width(width));
            GUI.backgroundColor = previous;
            return result;
        }

        private static int IntCell(int value, int original, float width)
        {
            Color previous = GUI.backgroundColor;
            if (value != original)
            {
                GUI.backgroundColor = ChangedCellColor;
            }

            int result = EditorGUILayout.IntField(value, GUILayout.Width(width));
            GUI.backgroundColor = previous;
            return result;
        }

        private static bool BoolCell(bool value, bool original, float width)
        {
            Color previous = GUI.backgroundColor;
            if (value != original)
            {
                GUI.backgroundColor = ChangedCellColor;
            }

            bool result = EditorGUILayout.Toggle(value, GUILayout.Width(width));
            GUI.backgroundColor = previous;
            return result;
        }

        private static SegmentAttackMoveType MoveTypeCell(SegmentAttackMoveType value, SegmentAttackMoveType original, float width)
        {
            Color previous = GUI.backgroundColor;
            if (value != original)
            {
                GUI.backgroundColor = ChangedCellColor;
            }

            int index = Mathf.Max(0, Array.IndexOf(MoveTypeValues, value));
            int resultIndex = EditorGUILayout.Popup(index, BuildMoveTypeLabels(), GUILayout.Width(width));
            GUI.backgroundColor = previous;
            return MoveTypeValues[Mathf.Clamp(resultIndex, 0, MoveTypeValues.Length - 1)];
        }

        private static SegmentAttackImpactType ImpactTypeCell(SegmentAttackImpactType value, SegmentAttackImpactType original, float width)
        {
            Color previous = GUI.backgroundColor;
            if (value != original)
            {
                GUI.backgroundColor = ChangedCellColor;
            }

            int index = Mathf.Max(0, Array.IndexOf(ImpactTypeValues, value));
            int resultIndex = EditorGUILayout.Popup(index, BuildImpactTypeLabels(), GUILayout.Width(width));
            GUI.backgroundColor = previous;
            return ImpactTypeValues[Mathf.Clamp(resultIndex, 0, ImpactTypeValues.Length - 1)];
        }

        private static string[] BuildMoveTypeLabels()
        {
            string[] labels = new string[MoveTypeValues.Length];
            for (int i = 0; i < MoveTypeValues.Length; i++)
            {
                labels[i] = TranslateMoveType(MoveTypeValues[i]);
            }

            return labels;
        }

        private static string[] BuildImpactTypeLabels()
        {
            string[] labels = new string[ImpactTypeValues.Length];
            for (int i = 0; i < ImpactTypeValues.Length; i++)
            {
                labels[i] = TranslateImpactType(ImpactTypeValues[i]);
            }

            return labels;
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.0001f;
        }

        private static bool Contains(string source, string query)
        {
            return !string.IsNullOrEmpty(source)
                && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string TranslateMoveType(SegmentAttackMoveType moveType)
        {
            switch (moveType)
            {
                case SegmentAttackMoveType.StraightProjectile:
                    return "직선 투사체";
                case SegmentAttackMoveType.PiercingProjectile:
                    return "관통 투사체";
                case SegmentAttackMoveType.ArcProjectile:
                    return "곡사 투사체";
                case SegmentAttackMoveType.HomingProjectile:
                    return "추적 투사체";
                case SegmentAttackMoveType.Laser:
                    return "레이저";
                case SegmentAttackMoveType.ChainLightning:
                    return "체인 번개";
                case SegmentAttackMoveType.SawBounceProjectile:
                    return "톱날 연쇄";
                case SegmentAttackMoveType.ExpandingFlameSphere:
                    return "확장 화염구";
                default:
                    return moveType.ToString();
            }
        }

        private static string TranslateImpactType(SegmentAttackImpactType impactType)
        {
            switch (impactType)
            {
                case SegmentAttackImpactType.DirectDamage:
                    return "직접 피해";
                case SegmentAttackImpactType.PierceDamage:
                    return "관통 피해";
                case SegmentAttackImpactType.ExplosionArea:
                    return "폭발 범위";
                case SegmentAttackImpactType.ContinuousDamage:
                    return "지속 피해";
                case SegmentAttackImpactType.ChainDamage:
                    return "체인 피해";
                default:
                    return impactType.ToString();
            }
        }

        private sealed class RowState
        {
            public SegmentDefinition Definition;
            public SegmentAttackProfile Profile;
            public string SegmentId;
            public string DisplayName;
            public int Level;
            public BalanceValues Original;
            public BalanceValues Working;

            public bool IsDirty => Profile != null && !Working.EqualsTo(Original);

            public static RowState Create(SegmentDefinition definition, SegmentLevelDefinition level)
            {
                SegmentAttackProfile profile = level.AttackProfile;
                BalanceValues values = profile != null ? BalanceValues.FromProfile(profile) : new BalanceValues();
                return new RowState
                {
                    Definition = definition,
                    Profile = profile,
                    SegmentId = definition != null ? definition.NormalizedId : string.Empty,
                    DisplayName = definition != null ? definition.DisplayName : string.Empty,
                    Level = Mathf.Max(0, level.Level),
                    Original = values.Clone(),
                    Working = values.Clone()
                };
            }

            public static RowState CreateMissing(SegmentDefinition definition, int level)
            {
                return new RowState
                {
                    Definition = definition,
                    Profile = null,
                    SegmentId = definition != null ? definition.NormalizedId : string.Empty,
                    DisplayName = definition != null ? definition.DisplayName : string.Empty,
                    Level = level,
                    Original = new BalanceValues(),
                    Working = new BalanceValues()
                };
            }

            public void ResetWorking()
            {
                Working = Original.Clone();
            }
        }

        private sealed class RowLogEntry
        {
            public readonly RowState Row;
            public readonly BalanceValues Before;
            public readonly BalanceValues After;
            public readonly DpsResult BeforeDps;
            public readonly DpsResult AfterDps;

            public RowLogEntry(RowState row, BalanceValues before, BalanceValues after, DpsResult beforeDps, DpsResult afterDps)
            {
                Row = row;
                Before = before;
                After = after;
                BeforeDps = beforeDps;
                AfterDps = afterDps;
            }
        }

        private sealed class BalanceValues
        {
            public SegmentAttackMoveType MoveType;
            public SegmentAttackImpactType ImpactType;
            public float SearchRange;
            public float BaseDamage;
            public float Cooldown;
            public int ProjectileCount;
            public bool FireProjectilesSequentially;
            public float ProjectileFireDelay;
            public float ProjectileSpeed;
            public int PierceCount;
            public float ExplosionRadius;
            public float LaserDuration;
            public float LaserTickInterval;
            public float ChainRange;
            public int MaxChainDepth;
            public int ChainBranchCount;
            public float ChainDamageFalloff;
            public float SawPierceDamageRatio;
            public float ProjectileLifetime;

            public static BalanceValues FromProfile(SegmentAttackProfile profile)
            {
                return new BalanceValues
                {
                    MoveType = profile.MoveType,
                    ImpactType = profile.ImpactType,
                    SearchRange = profile.SearchRange,
                    BaseDamage = profile.BaseDamage,
                    Cooldown = profile.Cooldown,
                    ProjectileCount = profile.ProjectileCount,
                    FireProjectilesSequentially = profile.FireProjectilesSequentially,
                    ProjectileFireDelay = profile.ProjectileFireDelay,
                    ProjectileSpeed = profile.ProjectileSpeed,
                    PierceCount = profile.PierceCount,
                    ExplosionRadius = profile.ExplosionRadius,
                    LaserDuration = profile.LaserDuration,
                    LaserTickInterval = profile.LaserTickInterval,
                    ChainRange = profile.ChainRange,
                    MaxChainDepth = profile.MaxChainDepth,
                    ChainBranchCount = profile.ChainBranchCount,
                    ChainDamageFalloff = profile.ChainDamageFalloff,
                    SawPierceDamageRatio = profile.SawPierceDamageRatio,
                    ProjectileLifetime = profile.ProjectileLifetime
                };
            }

            public BalanceValues Clone()
            {
                return (BalanceValues)MemberwiseClone();
            }

            public void ApplyTo(SegmentAttackProfile profile)
            {
                profile.MoveType = MoveType;
                profile.ImpactType = ImpactType;
                profile.SearchRange = Mathf.Max(0.1f, SearchRange);
                profile.BaseDamage = Mathf.Max(0f, BaseDamage);
                profile.Cooldown = Mathf.Max(0.05f, Cooldown);
                profile.ProjectileCount = Mathf.Max(1, ProjectileCount);
                profile.FireProjectilesSequentially = FireProjectilesSequentially;
                profile.ProjectileFireDelay = Mathf.Max(0f, ProjectileFireDelay);
                profile.ProjectileSpeed = Mathf.Max(0.1f, ProjectileSpeed);
                profile.PierceCount = Mathf.Max(0, PierceCount);
                profile.ExplosionRadius = Mathf.Max(0.1f, ExplosionRadius);
                profile.LaserDuration = Mathf.Max(0.05f, LaserDuration);
                profile.LaserTickInterval = Mathf.Max(0.02f, LaserTickInterval);
                profile.ChainRange = Mathf.Max(0.1f, ChainRange);
                profile.MaxChainDepth = Mathf.Max(0, MaxChainDepth);
                profile.ChainBranchCount = Mathf.Max(1, ChainBranchCount);
                profile.ChainDamageFalloff = Mathf.Clamp01(ChainDamageFalloff);
                profile.SawPierceDamageRatio = Mathf.Clamp01(SawPierceDamageRatio);
            }

            public bool EqualsTo(BalanceValues other)
            {
                if (other == null)
                {
                    return false;
                }

                return MoveType == other.MoveType
                    && ImpactType == other.ImpactType
                    && Approximately(SearchRange, other.SearchRange)
                    && Approximately(BaseDamage, other.BaseDamage)
                    && Approximately(Cooldown, other.Cooldown)
                    && ProjectileCount == other.ProjectileCount
                    && FireProjectilesSequentially == other.FireProjectilesSequentially
                    && Approximately(ProjectileFireDelay, other.ProjectileFireDelay)
                    && Approximately(ProjectileSpeed, other.ProjectileSpeed)
                    && PierceCount == other.PierceCount
                    && Approximately(ExplosionRadius, other.ExplosionRadius)
                    && Approximately(LaserDuration, other.LaserDuration)
                    && Approximately(LaserTickInterval, other.LaserTickInterval)
                    && Approximately(ChainRange, other.ChainRange)
                    && MaxChainDepth == other.MaxChainDepth
                    && ChainBranchCount == other.ChainBranchCount
                    && Approximately(ChainDamageFalloff, other.ChainDamageFalloff)
                    && Approximately(SawPierceDamageRatio, other.SawPierceDamageRatio);
            }
        }

        private readonly struct DpsResult
        {
            public readonly float SingleDps;
            public readonly float Cluster10Dps;

            public DpsResult(float singleDps, float cluster10Dps)
            {
                SingleDps = singleDps;
                Cluster10Dps = cluster10Dps;
            }
        }

        private static class DpsCalculator
        {
            public static DpsResult Calculate(BalanceValues values)
            {
                float cycleTime = GetCycleTime(values);
                float singleDamage = EstimateSingleCastDamage(values);
                float clusterDamage = EstimateClusterCastDamage(values, 10);
                return new DpsResult(singleDamage / cycleTime, clusterDamage / cycleTime);
            }

            private static float GetCycleTime(BalanceValues values)
            {
                int projectileCount = Mathf.Max(1, values.ProjectileCount);
                float cooldown = Mathf.Max(0.05f, values.Cooldown);
                if (values.FireProjectilesSequentially && projectileCount > 1)
                {
                    cooldown += Mathf.Max(0f, values.ProjectileFireDelay) * (projectileCount - 1);
                }

                return Mathf.Max(0.05f, cooldown);
            }

            private static float EstimateSingleCastDamage(BalanceValues values)
            {
                int projectileCount = Mathf.Max(1, values.ProjectileCount);
                float damage = Mathf.Max(0f, values.BaseDamage);

                switch (values.MoveType)
                {
                    case SegmentAttackMoveType.Laser:
                        return damage * GetTickCount(values.LaserDuration, values.LaserTickInterval);
                    case SegmentAttackMoveType.ExpandingFlameSphere:
                        return damage * projectileCount * GetTickCount(values.ProjectileLifetime, values.LaserTickInterval);
                    case SegmentAttackMoveType.ChainLightning:
                        return damage;
                    default:
                        return damage * projectileCount;
                }
            }

            private static float EstimateClusterCastDamage(BalanceValues values, int targetCount)
            {
                int clampedTargets = Mathf.Max(1, targetCount);
                int projectileCount = Mathf.Max(1, values.ProjectileCount);
                float damage = Mathf.Max(0f, values.BaseDamage);

                if (values.MoveType == SegmentAttackMoveType.ChainLightning)
                {
                    return EstimateChainDamage(damage, values.MaxChainDepth, values.ChainBranchCount, values.ChainDamageFalloff, clampedTargets);
                }

                if (values.MoveType == SegmentAttackMoveType.Laser)
                {
                    return EstimateSingleCastDamage(values);
                }

                if (values.MoveType == SegmentAttackMoveType.ExpandingFlameSphere)
                {
                    return damage * projectileCount * GetTickCount(values.ProjectileLifetime, values.LaserTickInterval) * clampedTargets;
                }

                if (values.MoveType == SegmentAttackMoveType.SawBounceProjectile)
                {
                    int fullDamageHits = Mathf.Min(clampedTargets, projectileCount * Mathf.Max(1, 1 + values.MaxChainDepth));
                    int remainingTargets = Mathf.Max(0, clampedTargets - fullDamageHits);
                    float pierceDamage = damage * Mathf.Clamp01(values.SawPierceDamageRatio) * remainingTargets;
                    return damage * fullDamageHits + pierceDamage;
                }

                if (values.ImpactType == SegmentAttackImpactType.ExplosionArea)
                {
                    return damage * projectileCount * clampedTargets;
                }

                if (values.MoveType == SegmentAttackMoveType.PiercingProjectile || values.ImpactType == SegmentAttackImpactType.PierceDamage)
                {
                    return damage * Mathf.Min(clampedTargets, projectileCount * Mathf.Max(1, values.PierceCount));
                }

                return damage * Mathf.Min(clampedTargets, projectileCount);
            }

            private static float EstimateChainDamage(float baseDamage, int maxDepth, int branchCount, float falloff, int targetCount)
            {
                int remainingTargets = Mathf.Max(0, targetCount);
                if (remainingTargets <= 0)
                {
                    return 0f;
                }

                float total = baseDamage;
                remainingTargets--;

                int branches = Mathf.Max(1, branchCount);
                float clampedFalloff = Mathf.Clamp01(falloff);
                int nodesAtDepth = 1;
                for (int depth = 1; depth <= Mathf.Max(0, maxDepth) && remainingTargets > 0; depth++)
                {
                    nodesAtDepth *= branches;
                    int hits = Mathf.Min(remainingTargets, nodesAtDepth);
                    total += baseDamage * Mathf.Pow(clampedFalloff, depth) * hits;
                    remainingTargets -= hits;
                }

                return total;
            }

            private static int GetTickCount(float duration, float interval)
            {
                float safeDuration = Mathf.Max(0.05f, duration);
                float safeInterval = Mathf.Max(0.02f, interval);
                return Mathf.Max(1, Mathf.CeilToInt(safeDuration / safeInterval));
            }
        }
    }
}
