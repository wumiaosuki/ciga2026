using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ciga2026.Game.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Ciga2026.Game.Editor
{
    /// <summary>
    /// 从 Resources/Table 下的 CSV 表生成核心玩法 ScriptableObject。
    /// </summary>
    public sealed class GameplayTableImporterWindow : EditorWindow
    {
        private const string TableFolder = "Assets/Resources/Table";
        private const string WordTablePath = TableFolder + "/选词表.csv";
        private const string ConfusionTablePath = TableFolder + "/混淆词.csv";
        private const string NewsTablePath = TableFolder + "/新闻组表.csv";
        private const string LevelTablePath = TableFolder + "/关卡表.csv";
        private const string WordLibraryPath = "Assets/ScriptableObjects/Gameplay/Words/DefaultWordLibrary.asset";
        private const string LevelSequencePath = "Assets/ScriptableObjects/Gameplay/Levels/DefaultLevelSequenceConfig.asset";
        private const string GeneratedRoot = "Assets/ScriptableObjects/Gameplay/GeneratedFromTables";
        private const string GeneratedNewsFolder = GeneratedRoot + "/News";
        private const string GeneratedDaysFolder = GeneratedRoot + "/Days";

        [SerializeField]
        private int defaultConfusionPenalty = 10;

        private Vector2 scrollPosition;
        private string lastImportReport;

        /// <summary>
        /// 打开表格导入器窗口。
        /// </summary>
        [MenuItem("Tools/Ciga2026/表格导入器")]
        public static void OpenWindow()
        {
            var window = GetWindow<GameplayTableImporterWindow>("表格导入器");
            window.minSize = new Vector2(620f, 420f);
            window.Show();
        }

        /// <summary>
        /// 直接从菜单执行一次完整导入。
        /// </summary>
        [MenuItem("Tools/Ciga2026/表格导入器/导入全部")]
        public static void ImportAllFromMenu()
        {
            var report = ImportAll(defaultConfusionPenalty: 10);
            Debug.Log(report);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("核心玩法表格导入", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("从 Assets/Resources/Table 中读取 CSV，生成/更新 WordLibrary、InformationDefinition、DayDefinition 和 LevelSequenceConfig。", MessageType.Info);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("输入表", EditorStyles.boldLabel);
                DrawPathLabel("选词表", WordTablePath);
                DrawPathLabel("混淆词", ConfusionTablePath);
                DrawPathLabel("新闻组表", NewsTablePath);
                DrawPathLabel("关卡表", LevelTablePath);
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("导入设置", EditorStyles.boldLabel);
                defaultConfusionPenalty = EditorGUILayout.IntField("默认混淆词扣分", Mathf.Max(0, defaultConfusionPenalty));
                DrawPathLabel("新闻 SO 输出", GeneratedNewsFolder);
                DrawPathLabel("天 SO 输出", GeneratedDaysFolder);
                DrawPathLabel("词库 SO", WordLibraryPath);
                DrawPathLabel("流程 SO", LevelSequencePath);
            }

            if (GUILayout.Button("导入全部", GUILayout.Height(32f)))
            {
                lastImportReport = ImportAll(defaultConfusionPenalty);
                Debug.Log(lastImportReport);
            }

            if (!string.IsNullOrEmpty(lastImportReport))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("导入报告", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                EditorGUILayout.TextArea(lastImportReport, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawPathLabel(string label, string path)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(100f));
                EditorGUILayout.SelectableLabel(path, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        private static string ImportAll(int defaultConfusionPenalty)
        {
            var report = new ImportReport();

            try
            {
                EnsureFolder("Assets/ScriptableObjects");
                EnsureFolder("Assets/ScriptableObjects/Gameplay");
                EnsureFolder("Assets/ScriptableObjects/Gameplay/Words");
                EnsureFolder("Assets/ScriptableObjects/Gameplay/Levels");
                EnsureFolder(GeneratedRoot);
                EnsureFolder(GeneratedNewsFolder);
                EnsureFolder(GeneratedDaysFolder);

                var wordRows = ReadWordRows(WordTablePath, report);
                var confusionRows = ReadConfusionRows(ConfusionTablePath, report);
                var newsRows = ReadNewsRows(NewsTablePath, report);
                var levelRows = ReadLevelRows(LevelTablePath, report);

                var wordsById = wordRows
                    .GroupBy(row => row.Id)
                    .ToDictionary(group => group.Key, group => group.Last());
                var confusionsByWordId = confusionRows
                    .GroupBy(row => row.SourceWordId)
                    .ToDictionary(group => group.Key, group => group.ToList());

                ValidateTables(wordsById, confusionsByWordId, newsRows, levelRows, report);
                if (report.HasErrors)
                {
                    report.Warning("检测到表格错误，已停止写入 SO。");
                    return report.ToString();
                }

                var wordLibrary = LoadOrCreateAsset<WordLibrary>(WordLibraryPath);
                WriteWordLibrary(wordLibrary, wordRows, confusionsByWordId);

                DeleteGeneratedAssets<InformationDefinition>(GeneratedNewsFolder, report);
                DeleteGeneratedAssets<DayDefinition>(GeneratedDaysFolder, report);

                var newsAssetsById = new Dictionary<int, InformationDefinition>();
                foreach (var newsRow in newsRows)
                {
                    var assetPath = $"{GeneratedNewsFolder}/News_{newsRow.Id}.asset";
                    var asset = LoadOrCreateAsset<InformationDefinition>(assetPath);
                    WriteInformationDefinition(asset, newsRow, wordsById, confusionsByWordId, defaultConfusionPenalty, report);
                    newsAssetsById[newsRow.Id] = asset;
                }

                var orderedLevelRows = OrderLevelRows(levelRows, report);
                var dayAssets = new List<DayDefinition>();
                foreach (var levelRow in orderedLevelRows)
                {
                    var assetPath = $"{GeneratedDaysFolder}/Day_{levelRow.Id}.asset";
                    var day = LoadOrCreateAsset<DayDefinition>(assetPath);
                    WriteDayDefinition(day, levelRow, newsAssetsById, report);
                    dayAssets.Add(day);
                }

                var levelSequence = LoadOrCreateAsset<LevelSequenceConfig>(LevelSequencePath);
                WriteLevelSequence(levelSequence, dayAssets);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                report.Info($"导入完成：标准词 {wordRows.Count}，混淆词 {confusionRows.Count}，新闻 {newsAssetsById.Count}，天/关卡 {dayAssets.Count}。");
            }
            catch (Exception exception)
            {
                report.Error($"导入失败：{exception.Message}\n{exception.StackTrace}");
            }

            return report.ToString();
        }

        private static List<WordRow> ReadWordRows(string path, ImportReport report)
        {
            return ReadCsvRows(path, report, row => new WordRow(
                ParseRequiredInt(row, "id", path, report),
                ParseRequiredInt(row, "news_id", path, report),
                ParseRequiredInt(row, "new_numb", path, report),
                GetValue(row, "word"),
                ParseOptionalInt(row, "coin")));
        }

        private static List<ConfusionRow> ReadConfusionRows(string path, ImportReport report)
        {
            return ReadCsvRows(path, report, row => new ConfusionRow(
                ParseRequiredInt(row, "id", path, report),
                ParseRequiredInt(row, "mis_word", path, report),
                GetValue(row, "word")));
        }

        private static List<NewsRow> ReadNewsRows(string path, ImportReport report)
        {
            return ReadCsvRows(path, report, row => new NewsRow(
                ParseRequiredInt(row, "id", path, report),
                ParseIntList(GetValue(row, "word_group"), path, row.LineNumber, report),
                GetValue(row, "mistake_word"),
                ParseOptionalInt(row, "connect_word")));
        }

        private static List<LevelRow> ReadLevelRows(string path, ImportReport report)
        {
            return ReadCsvRows(path, report, row => new LevelRow(
                ParseRequiredInt(row, "id", path, report),
                ParseOptionalInt(row, "next_level"),
                ParseIntList(GetValue(row, "level_news"), path, row.LineNumber, report)));
        }

        private static List<T> ReadCsvRows<T>(string path, ImportReport report, Func<CsvRow, T> factory)
        {
            if (!File.Exists(path))
            {
                report.Error($"表不存在：{path}");
                return new List<T>();
            }

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 4)
            {
                report.Error($"表行数不足：{path}");
                return new List<T>();
            }

            var keys = ParseCsvLine(lines[1]);
            var results = new List<T>();
            for (var lineIndex = 3; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    continue;
                }

                var values = ParseCsvLine(lines[lineIndex]);
                var row = new CsvRow(path, lineIndex + 1, keys, values);
                results.Add(factory(row));
            }

            return results;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var builder = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];
                if (character == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        builder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (character == ',' && !inQuotes)
                {
                    values.Add(builder.ToString().Trim());
                    builder.Clear();
                    continue;
                }

                builder.Append(character);
            }

            values.Add(builder.ToString().Trim());
            return values;
        }

        private static List<int> ParseIntList(string rawValue, string path, int lineNumber, ImportReport report)
        {
            var normalized = rawValue.Trim().TrimStart('[').TrimEnd(']');
            var values = new List<int>();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return values;
            }

            var tokens = normalized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (int.TryParse(token.Trim(), out var value))
                {
                    values.Add(value);
                    continue;
                }

                report.Warning($"{path}:{lineNumber} 无法解析 ID 列表项：{token}");
            }

            return values;
        }

        private static int ParseRequiredInt(CsvRow row, string key, string path, ImportReport report)
        {
            var rawValue = GetValue(row, key);
            if (int.TryParse(rawValue, out var value))
            {
                return value;
            }

            report.Error($"{path}:{row.LineNumber} 字段 {key} 不是有效整数：{rawValue}");
            return 0;
        }

        private static int ParseOptionalInt(CsvRow row, string key)
        {
            var rawValue = GetValue(row, key);
            return int.TryParse(rawValue, out var value) ? value : 0;
        }

        private static string GetValue(CsvRow row, string key)
        {
            return row.ValuesByKey.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static void ValidateTables(
            IReadOnlyDictionary<int, WordRow> wordsById,
            IReadOnlyDictionary<int, List<ConfusionRow>> confusionsByWordId,
            IReadOnlyList<NewsRow> newsRows,
            IReadOnlyList<LevelRow> levelRows,
            ImportReport report)
        {
            foreach (var sourceWordId in confusionsByWordId.Keys)
            {
                if (!wordsById.ContainsKey(sourceWordId))
                {
                    report.Warning($"混淆词引用了不存在的选词：{sourceWordId}");
                }
            }

            var newsIds = new HashSet<int>(newsRows.Select(row => row.Id));
            foreach (var newsRow in newsRows)
            {
                foreach (var wordId in newsRow.WordIds)
                {
                    if (!wordsById.ContainsKey(wordId))
                    {
                        report.Warning($"新闻 {newsRow.Id} 的标准选词不存在：{wordId}");
                    }
                }
            }

            foreach (var levelRow in levelRows)
            {
                foreach (var newsId in levelRow.NewsIds)
                {
                    if (!newsIds.Contains(newsId))
                    {
                        report.Warning($"关卡 {levelRow.Id} 引用了不存在的新闻：{newsId}");
                    }
                }
            }
        }

        private static void WriteWordLibrary(
            WordLibrary wordLibrary,
            IReadOnlyList<WordRow> wordRows,
            IReadOnlyDictionary<int, List<ConfusionRow>> confusionsByWordId)
        {
            var serializedObject = new SerializedObject(wordLibrary);
            serializedObject.Update();
            var wordGroupsProperty = serializedObject.FindProperty("wordGroups");
            wordGroupsProperty.ClearArray();

            var wordsByNews = wordRows
                .OrderBy(row => row.NewsId)
                .ThenBy(row => row.Sequence)
                .GroupBy(row => row.NewsId);
            foreach (var group in wordsByNews)
            {
                AddWordGroup(
                    wordGroupsProperty,
                    $"新闻 {group.Key}",
                    group.Select(row => new WordLibraryImportWord(
                        row.Id.ToString(),
                        row.Text,
                        confusionsByWordId.TryGetValue(row.Id, out var confusions)
                            ? confusions.OrderBy(confusion => confusion.Id).Select(confusion => confusion.Text)
                            : Enumerable.Empty<string>())));
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wordLibrary);
        }

        private static void AddWordGroup(SerializedProperty wordGroupsProperty, string title, IEnumerable<WordLibraryImportWord> words)
        {
            wordGroupsProperty.InsertArrayElementAtIndex(wordGroupsProperty.arraySize);
            var groupProperty = wordGroupsProperty.GetArrayElementAtIndex(wordGroupsProperty.arraySize - 1);
            groupProperty.FindPropertyRelative("title").stringValue = title;

            var wordsProperty = groupProperty.FindPropertyRelative("words");
            wordsProperty.ClearArray();
            foreach (var word in words)
            {
                wordsProperty.InsertArrayElementAtIndex(wordsProperty.arraySize);
                var wordProperty = wordsProperty.GetArrayElementAtIndex(wordsProperty.arraySize - 1);
                wordProperty.FindPropertyRelative("id").stringValue = word.Id;
                wordProperty.FindPropertyRelative("text").stringValue = word.Text;

                var confusionWordsProperty = wordProperty.FindPropertyRelative("confusionWords");
                confusionWordsProperty.ClearArray();
                foreach (var confusionText in word.ConfusionTexts)
                {
                    confusionWordsProperty.InsertArrayElementAtIndex(confusionWordsProperty.arraySize);
                    confusionWordsProperty.GetArrayElementAtIndex(confusionWordsProperty.arraySize - 1).stringValue = confusionText;
                }
            }
        }

        private static void WriteInformationDefinition(
            InformationDefinition information,
            NewsRow newsRow,
            IReadOnlyDictionary<int, WordRow> wordsById,
            IReadOnlyDictionary<int, List<ConfusionRow>> confusionsByWordId,
            int defaultConfusionPenalty,
            ImportReport report)
        {
            var serializedObject = new SerializedObject(information);
            serializedObject.Update();
            serializedObject.FindProperty("id").stringValue = newsRow.Id.ToString();
            serializedObject.FindProperty("informationText").stringValue = newsRow.MistakeText;
            serializedObject.FindProperty("relatedWordCount").intValue = Mathf.Max(1, newsRow.RelatedWordCount);

            var choiceSetsProperty = serializedObject.FindProperty("choiceSets");
            choiceSetsProperty.ClearArray();

            for (var i = 0; i < newsRow.WordIds.Count; i++)
            {
                var wordId = newsRow.WordIds[i];
                if (!wordsById.TryGetValue(wordId, out var wordRow))
                {
                    continue;
                }

                choiceSetsProperty.InsertArrayElementAtIndex(choiceSetsProperty.arraySize);
                var choiceSetProperty = choiceSetsProperty.GetArrayElementAtIndex(choiceSetsProperty.arraySize - 1);
                choiceSetProperty.FindPropertyRelative("title").stringValue = $"{wordRow.Sequence}. {wordRow.Text}";
                var choicesProperty = choiceSetProperty.FindPropertyRelative("choices");
                choicesProperty.ClearArray();
                AddChoiceEntry(choicesProperty, wordRow.Id.ToString(), wordRow.Text, Mathf.Max(0, wordRow.ExtraScore), "标准选词");

                if (!confusionsByWordId.TryGetValue(wordId, out var confusions) || confusions.Count == 0)
                {
                    report.Warning($"新闻 {newsRow.Id} 的选词 {wordId} 没有配置混淆词。");
                    continue;
                }

                foreach (var confusion in confusions.OrderBy(row => row.Id))
                {
                    AddChoiceEntry(choicesProperty, confusion.Id.ToString(), confusion.Text, defaultConfusionPenalty, $"混淆词，所属选词 {wordId}");
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(information);
        }

        private static void AddChoiceEntry(SerializedProperty choicesProperty, string wordId, string displayText, int penalty, string note)
        {
            choicesProperty.InsertArrayElementAtIndex(choicesProperty.arraySize);
            var choiceProperty = choicesProperty.GetArrayElementAtIndex(choicesProperty.arraySize - 1);
            choiceProperty.FindPropertyRelative("wordId").stringValue = wordId;
            choiceProperty.FindPropertyRelative("displayText").stringValue = displayText;
            choiceProperty.FindPropertyRelative("tolerancePenalty").intValue = Mathf.Max(0, penalty);
            choiceProperty.FindPropertyRelative("note").stringValue = note;
        }

        private static void WriteDayDefinition(DayDefinition day, LevelRow levelRow, IReadOnlyDictionary<int, InformationDefinition> newsAssetsById, ImportReport report)
        {
            var serializedObject = new SerializedObject(day);
            serializedObject.Update();
            serializedObject.FindProperty("title").stringValue = $"关卡 {levelRow.Id}";
            var levelsProperty = serializedObject.FindProperty("levels");
            levelsProperty.ClearArray();

            foreach (var newsId in levelRow.NewsIds)
            {
                if (!newsAssetsById.TryGetValue(newsId, out var information))
                {
                    report.Warning($"关卡 {levelRow.Id} 跳过不存在的新闻：{newsId}");
                    continue;
                }

                levelsProperty.InsertArrayElementAtIndex(levelsProperty.arraySize);
                levelsProperty.GetArrayElementAtIndex(levelsProperty.arraySize - 1).objectReferenceValue = information;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(day);
        }

        private static void WriteLevelSequence(LevelSequenceConfig levelSequence, IReadOnlyList<DayDefinition> days)
        {
            var serializedObject = new SerializedObject(levelSequence);
            serializedObject.Update();
            var daysProperty = serializedObject.FindProperty("days");
            daysProperty.ClearArray();
            foreach (var day in days)
            {
                daysProperty.InsertArrayElementAtIndex(daysProperty.arraySize);
                daysProperty.GetArrayElementAtIndex(daysProperty.arraySize - 1).objectReferenceValue = day;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelSequence);
        }

        private static List<LevelRow> OrderLevelRows(IReadOnlyList<LevelRow> levelRows, ImportReport report)
        {
            if (levelRows.Count == 0)
            {
                return new List<LevelRow>();
            }

            var rowsById = levelRows.ToDictionary(row => row.Id, row => row);
            var pointedIds = new HashSet<int>(levelRows.Where(row => row.NextLevelId != 0).Select(row => row.NextLevelId));
            var start = levelRows.FirstOrDefault(row => !pointedIds.Contains(row.Id));
            if (start.Id == 0)
            {
                start = levelRows[0];
            }

            var ordered = new List<LevelRow>();
            var visited = new HashSet<int>();
            var currentId = start.Id;

            while (rowsById.TryGetValue(currentId, out var current) && visited.Add(current.Id))
            {
                ordered.Add(current);
                if (current.NextLevelId == 0)
                {
                    break;
                }

                if (!rowsById.ContainsKey(current.NextLevelId))
                {
                    report.Warning($"关卡 {ordered[^1].Id} 的 next_level 不存在：{ordered[^1].NextLevelId}");
                    break;
                }

                currentId = current.NextLevelId;
            }

            foreach (var row in levelRows)
            {
                if (visited.Add(row.Id))
                {
                    ordered.Add(row);
                }
            }

            return ordered;
        }

        private static TAsset LoadOrCreateAsset<TAsset>(string path) where TAsset : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(Path.GetDirectoryName(path)?.Replace("\\", "/"));
            asset = CreateInstance<TAsset>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void DeleteGeneratedAssets<TAsset>(string folder, ImportReport report)
            where TAsset : ScriptableObject
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var typeName = typeof(TAsset).Name;
            var guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { folder });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (AssetDatabase.DeleteAsset(assetPath))
                {
                    report.Info($"删除旧生成资产：{assetPath}");
                }
                else
                {
                    report.Warning($"删除旧生成资产失败：{assetPath}");
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private readonly struct WordLibraryImportWord
        {
            public WordLibraryImportWord(string id, string text, IEnumerable<string> confusionTexts)
            {
                Id = id;
                Text = text;
                ConfusionTexts = confusionTexts != null ? confusionTexts.ToList() : new List<string>();
            }

            public string Id { get; }
            public string Text { get; }
            public IReadOnlyList<string> ConfusionTexts { get; }
        }

        private readonly struct WordRow
        {
            public WordRow(int id, int newsId, int sequence, string text, int extraScore)
            {
                Id = id;
                NewsId = newsId;
                Sequence = sequence;
                Text = text;
                ExtraScore = extraScore;
            }

            public int Id { get; }
            public int NewsId { get; }
            public int Sequence { get; }
            public string Text { get; }
            public int ExtraScore { get; }
        }

        private readonly struct ConfusionRow
        {
            public ConfusionRow(int id, int sourceWordId, string text)
            {
                Id = id;
                SourceWordId = sourceWordId;
                Text = text;
            }

            public int Id { get; }
            public int SourceWordId { get; }
            public string Text { get; }
        }

        private readonly struct NewsRow
        {
            public NewsRow(int id, List<int> wordIds, string mistakeText, int relatedWordCount)
            {
                Id = id;
                WordIds = wordIds;
                MistakeText = mistakeText;
                RelatedWordCount = relatedWordCount;
            }

            public int Id { get; }
            public List<int> WordIds { get; }
            public string MistakeText { get; }
            public int RelatedWordCount { get; }
        }

        private readonly struct LevelRow
        {
            public LevelRow(int id, int nextLevelId, List<int> newsIds)
            {
                Id = id;
                NextLevelId = nextLevelId;
                NewsIds = newsIds;
            }

            public int Id { get; }
            public int NextLevelId { get; }
            public List<int> NewsIds { get; }
        }

        private sealed class CsvRow
        {
            public CsvRow(string path, int lineNumber, IReadOnlyList<string> keys, IReadOnlyList<string> values)
            {
                Path = path;
                LineNumber = lineNumber;
                ValuesByKey = new Dictionary<string, string>();
                for (var i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    ValuesByKey[key] = i < values.Count ? values[i] : string.Empty;
                }
            }

            public string Path { get; }
            public int LineNumber { get; }
            public Dictionary<string, string> ValuesByKey { get; }
        }

        private sealed class ImportReport
        {
            private readonly StringBuilder builder = new();
            private int warningCount;
            private int errorCount;

            public void Info(string message)
            {
                builder.AppendLine("[Info] " + message);
            }

            public void Warning(string message)
            {
                warningCount++;
                builder.AppendLine("[Warning] " + message);
            }

            public void Error(string message)
            {
                errorCount++;
                builder.AppendLine("[Error] " + message);
            }

            public bool HasErrors => errorCount > 0;

            public override string ToString()
            {
                return $"导入报告：{warningCount} warning，{errorCount} error\n" + builder;
            }
        }
    }
}
