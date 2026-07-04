using System.Collections.Generic;
using Ciga2026.Game.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Ciga2026.Game.Editor
{
    /// <summary>
    /// 面向策划的核心玩法配置窗口，用于集中编辑关卡、词库和基础数值配置。
    /// </summary>
    public sealed class GameplayConfigEditorWindow : EditorWindow
    {
        private const string LevelSequencePath = "Assets/ScriptableObjects/Gameplay/Levels/DefaultLevelSequenceConfig.asset";
        private const string WordLibraryPath = "Assets/ScriptableObjects/Gameplay/Words/DefaultWordLibrary.asset";
        private const string SessionConfigPath = "Assets/ScriptableObjects/Gameplay/Configs/DefaultGameplaySessionConfig.asset";
        private const string LevelFolderPath = "Assets/ScriptableObjects/Gameplay/Levels";
        private const string DayFolderPath = "Assets/ScriptableObjects/Gameplay/Days";

        private readonly string[] tabNames = { "关卡", "基础配置" };

        private LevelSequenceConfig levelSequenceConfig;
        private WordLibrary wordLibrary;
        private GameplaySessionConfig sessionConfig;

        private SerializedObject levelSequenceSerializedObject;
        private SerializedObject wordLibrarySerializedObject;
        private SerializedObject sessionConfigSerializedObject;
        private SerializedObject selectedLevelSerializedObject;

        private Vector2 levelListScroll;
        private Vector2 levelEditorScroll;
        private Vector2 wordLibraryScroll;
        private Vector2 configScroll;
        private int selectedTab;
        private int selectedLevelIndex;
        private string statusMessage;

        /// <summary>
        /// 打开关卡编辑器窗口。
        /// </summary>
        [MenuItem("Tools/Ciga2026/关卡编辑器")]
        public static void OpenWindow()
        {
            var window = GetWindow<GameplayConfigEditorWindow>("关卡编辑器");
            window.minSize = new Vector2(980f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            AutoImportAssets();
        }

        private void OnGUI()
        {
            DrawTopToolbar();
            DrawTabBar();

            EditorGUILayout.Space(4f);

            if (!HasRequiredAssets())
            {
                DrawMissingAssetsHelp();
                return;
            }

            switch (selectedTab)
            {
                case 0:
                    DrawLevelsTab();
                    break;
                case 1:
                    DrawBaseConfigTab();
                    break;
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(3f);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }

        private void DrawTopToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    SaveAll();
                }

                if (GUILayout.Button("自动导入", EditorStyles.toolbarButton, GUILayout.Width(92f)))
                {
                    AutoImportAssets();
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(levelSequenceConfig == null))
                {
                    if (GUILayout.Button("定位流程 SO", EditorStyles.toolbarButton, GUILayout.Width(96f)))
                    {
                        Selection.activeObject = levelSequenceConfig;
                        EditorGUIUtility.PingObject(levelSequenceConfig);
                    }
                }
            }
        }

        private void DrawTabBar()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30f));
        }

        private bool HasRequiredAssets()
        {
            return levelSequenceConfig != null && wordLibrary != null && sessionConfig != null;
        }

        private void DrawMissingAssetsHelp()
        {
            EditorGUILayout.HelpBox("缺少一个或多个默认配置资产。点击“自动导入”会按默认路径和类型重新查找。", MessageType.Warning);

            levelSequenceConfig = (LevelSequenceConfig)EditorGUILayout.ObjectField("关卡流程", levelSequenceConfig, typeof(LevelSequenceConfig), false);
            wordLibrary = (WordLibrary)EditorGUILayout.ObjectField("词库", wordLibrary, typeof(WordLibrary), false);
            sessionConfig = (GameplaySessionConfig)EditorGUILayout.ObjectField("玩法配置", sessionConfig, typeof(GameplaySessionConfig), false);

            RebuildSerializedObjects();
        }

        private void DrawLevelsTab()
        {
            var availableWidth = Mathf.Max(900f, position.width - 18f);
            var spacing = 8f;
            var leftWidth = Mathf.Floor(availableWidth * 0.2f);
            var middleWidth = Mathf.Floor(availableWidth * 0.5f);
            var rightWidth = availableWidth - leftWidth - middleWidth - spacing * 2f;

            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(leftWidth), GUILayout.ExpandHeight(true)))
                {
                    DrawLevelListPanel();
                }

                GUILayout.Space(spacing);

                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(middleWidth), GUILayout.ExpandHeight(true)))
                {
                    DrawSelectedLevelPanel();
                }

                GUILayout.Space(spacing);

                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(rightWidth), GUILayout.ExpandHeight(true)))
                {
                    DrawWordLibraryPanel();
                }
            }
        }

        private void DrawLevelListPanel()
        {
            EditorGUILayout.LabelField("关卡列表", EditorStyles.boldLabel);

            levelSequenceConfig = (LevelSequenceConfig)EditorGUILayout.ObjectField(levelSequenceConfig, typeof(LevelSequenceConfig), false);
            if (levelSequenceConfig != null && (levelSequenceSerializedObject == null || levelSequenceSerializedObject.targetObject != levelSequenceConfig))
            {
                levelSequenceSerializedObject = new SerializedObject(levelSequenceConfig);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新建关卡"))
                {
                    CreateAndAppendLevel();
                }

                if (GUILayout.Button("刷新"))
                {
                    AutoImportAssets();
                }
            }

            if (levelSequenceSerializedObject == null)
            {
                return;
            }

            levelSequenceSerializedObject.Update();
            var daysProperty = levelSequenceSerializedObject.FindProperty("days");
            selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, Mathf.Max(0, levelSequenceConfig.Count - 1));

            levelListScroll = EditorGUILayout.BeginScrollView(levelListScroll);
            EditorGUILayout.PropertyField(daysProperty, new GUIContent("天数 SO"), true);
            EditorGUILayout.Space(6f);

            for (var i = 0; i < levelSequenceConfig.Count; i++)
            {
                if (!levelSequenceConfig.TryGetLevel(i, out var level, out var dayIndex, out var dayLevelIndex, out _))
                {
                    continue;
                }

                var label = $"Day {dayIndex + 1}-{dayLevelIndex + 1}. {GetLevelDisplayName(level)}";
                if (GUILayout.Toggle(selectedLevelIndex == i, label, "Button"))
                {
                    SelectLevel(i);
                }
            }
            EditorGUILayout.EndScrollView();

            levelSequenceSerializedObject.ApplyModifiedProperties();
            RefreshSelectedLevelSerializedObject();
        }

        private void DrawSelectedLevelPanel()
        {
            EditorGUILayout.LabelField("关卡设定", EditorStyles.boldLabel);
            RefreshSelectedLevelSerializedObject();

            if (selectedLevelSerializedObject == null)
            {
                EditorGUILayout.HelpBox("请在左侧选择一个关卡。", MessageType.Info);
                return;
            }

            selectedLevelSerializedObject.Update();
            levelEditorScroll = EditorGUILayout.BeginScrollView(levelEditorScroll);

            DrawProperty("id", "关卡 ID");
            DrawProperty("informationText", "信息文本");

            EditorGUILayout.Space(8f);
            DrawProperty("relatedWordCount", "关联词数量");
            DrawProperty("choiceSets", "互斥词组");

            EditorGUILayout.EndScrollView();

            if (selectedLevelSerializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(selectedLevelSerializedObject.targetObject);
            }
        }

        private void DrawProperty(string propertyName, string label)
        {
            var property = selectedLevelSerializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
            }
        }

        private void DrawWordLibraryPanel()
        {
            EditorGUILayout.LabelField("词库", EditorStyles.boldLabel);

            wordLibrary = (WordLibrary)EditorGUILayout.ObjectField(wordLibrary, typeof(WordLibrary), false);
            if (wordLibrary != null && (wordLibrarySerializedObject == null || wordLibrarySerializedObject.targetObject != wordLibrary))
            {
                wordLibrarySerializedObject = new SerializedObject(wordLibrary);
            }

            if (wordLibrarySerializedObject == null)
            {
                return;
            }

            wordLibrarySerializedObject.Update();
            wordLibraryScroll = EditorGUILayout.BeginScrollView(wordLibraryScroll);

            EditorGUILayout.PropertyField(wordLibrarySerializedObject.FindProperty("wordGroups"), new GUIContent("词语分组"), true);
            wordLibrarySerializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("快速加入当前关卡", EditorStyles.boldLabel);
            DrawWordQuickAddList();

            EditorGUILayout.EndScrollView();
        }

        private void DrawWordQuickAddList()
        {
            if (wordLibrary == null)
            {
                return;
            }

            foreach (var group in wordLibrary.WordGroups)
            {
                if (group == null)
                {
                    continue;
                }

                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(group.Title) ? "未命名分组" : group.Title, EditorStyles.miniBoldLabel);
                foreach (var word in group.Words)
                {
                    if (word == null || string.IsNullOrWhiteSpace(word.Id))
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{word.Text}  ({word.Id})");

                        using (new EditorGUI.DisabledScope(selectedLevelSerializedObject == null))
                        {
                            if (GUILayout.Button("加入", GUILayout.Width(48f)))
                            {
                                AddWordIdToSelectedLevel(word.Id);
                            }
                        }
                    }
                }
            }
        }

        private void DrawBaseConfigTab()
        {
            configScroll = EditorGUILayout.BeginScrollView(configScroll);
            EditorGUILayout.LabelField("基础配置", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                levelSequenceConfig = (LevelSequenceConfig)EditorGUILayout.ObjectField("默认关卡流程", levelSequenceConfig, typeof(LevelSequenceConfig), false);
                wordLibrary = (WordLibrary)EditorGUILayout.ObjectField("默认词库", wordLibrary, typeof(WordLibrary), false);
                sessionConfig = (GameplaySessionConfig)EditorGUILayout.ObjectField("玩法会话配置", sessionConfig, typeof(GameplaySessionConfig), false);
            }

            RebuildSerializedObjects();

            DrawConfigObjectEditor("玩法会话配置", sessionConfigSerializedObject);

            EditorGUILayout.Space(8f);
            DrawValidationSummary();

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigObjectEditor(string title, SerializedObject serializedObject)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.MinWidth(320f)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                if (serializedObject == null)
                {
                    EditorGUILayout.HelpBox("未绑定配置资产。", MessageType.Warning);
                    return;
                }

                serializedObject.Update();
                var property = serializedObject.GetIterator();
                var enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyPath == "m_Script")
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.PropertyField(property, true);
                        }
                        continue;
                    }

                    EditorGUILayout.PropertyField(property, true);
                }

                if (serializedObject.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(serializedObject.targetObject);
                }
            }
        }

        private void DrawValidationSummary()
        {
            EditorGUILayout.LabelField("配置检查", EditorStyles.boldLabel);

            if (levelSequenceConfig == null || wordLibrary == null)
            {
                EditorGUILayout.HelpBox("缺少关卡流程或词库，无法检查。", MessageType.Warning);
                return;
            }

            var issues = new List<string>();
            var wordIds = new HashSet<string>();
            var duplicateWordIds = new HashSet<string>();

            foreach (var word in wordLibrary.Words)
            {
                if (word == null || string.IsNullOrWhiteSpace(word.Id))
                {
                    continue;
                }

                if (!wordIds.Add(word.Id))
                {
                    duplicateWordIds.Add(word.Id);
                }
            }

            foreach (var duplicateId in duplicateWordIds)
            {
                issues.Add($"词库存在重复 ID：{duplicateId}");
            }

            for (var i = 0; i < levelSequenceConfig.Count; i++)
            {
                if (!levelSequenceConfig.TryGetLevel(i, out var level))
                {
                    issues.Add($"第 {i + 1} 关为空。");
                    continue;
                }

                if (level == null)
                {
                    issues.Add($"第 {i + 1} 关为空。");
                    continue;
                }

                foreach (var choiceSet in level.ChoiceSets)
                {
                    if (choiceSet == null)
                    {
                        continue;
                    }

                    foreach (var choice in choiceSet.Choices)
                    {
                        if (choice == null || string.IsNullOrWhiteSpace(choice.WordId))
                        {
                            continue;
                        }

                        if (!wordIds.Contains(choice.WordId))
                        {
                            issues.Add($"第 {i + 1} 关词组选项不存在：{choice.WordId}");
                        }
                    }
                }
            }

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox($"检查通过。关卡数：{levelSequenceConfig.Count}，词语数：{wordIds.Count}。", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);
        }

        private void AutoImportAssets()
        {
            levelSequenceConfig = LoadOrFindAsset<LevelSequenceConfig>(LevelSequencePath);
            wordLibrary = LoadOrFindAsset<WordLibrary>(WordLibraryPath);
            sessionConfig = LoadOrFindAsset<GameplaySessionConfig>(SessionConfigPath);
            selectedLevelIndex = 0;
            RebuildSerializedObjects();
            RefreshSelectedLevelSerializedObject();
            statusMessage = "已自动导入默认配置资产。";
            Repaint();
        }

        private void SaveAll()
        {
            ApplySerializedObject(levelSequenceSerializedObject);
            ApplySerializedObject(wordLibrarySerializedObject);
            ApplySerializedObject(sessionConfigSerializedObject);
            ApplySerializedObject(selectedLevelSerializedObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            statusMessage = "配置已保存。";
        }

        private void ApplySerializedObject(SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);
        }

        private void RebuildSerializedObjects()
        {
            levelSequenceSerializedObject = levelSequenceConfig != null ? new SerializedObject(levelSequenceConfig) : null;
            wordLibrarySerializedObject = wordLibrary != null ? new SerializedObject(wordLibrary) : null;
            sessionConfigSerializedObject = sessionConfig != null ? new SerializedObject(sessionConfig) : null;
            RefreshSelectedLevelSerializedObject();
        }

        private void SelectLevel(int index)
        {
            selectedLevelIndex = index;
            RefreshSelectedLevelSerializedObject();
        }

        private void RefreshSelectedLevelSerializedObject()
        {
            var selectedLevel = GetSelectedLevel();
            selectedLevelSerializedObject = selectedLevel != null ? new SerializedObject(selectedLevel) : null;
        }

        private InformationDefinition GetSelectedLevel()
        {
            if (levelSequenceConfig == null || levelSequenceConfig.Count == 0)
            {
                return null;
            }

            selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, levelSequenceConfig.Count - 1);
            return levelSequenceConfig.TryGetLevel(selectedLevelIndex, out var level) ? level : null;
        }

        private void AddWordIdToSelectedLevel(string wordId)
        {
            if (selectedLevelSerializedObject == null || string.IsNullOrWhiteSpace(wordId))
            {
                return;
            }

            selectedLevelSerializedObject.Update();
            var choiceSetsProperty = selectedLevelSerializedObject.FindProperty("choiceSets");
            for (var i = 0; i < choiceSetsProperty.arraySize; i++)
            {
                var choicesProperty = choiceSetsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("choices");
                if (choicesProperty == null)
                {
                    continue;
                }

                for (var j = 0; j < choicesProperty.arraySize; j++)
                {
                    if (choicesProperty.GetArrayElementAtIndex(j).FindPropertyRelative("wordId").stringValue == wordId)
                    {
                        statusMessage = $"当前关卡已包含词语：{wordId}";
                        return;
                    }
                }
            }

            choiceSetsProperty.InsertArrayElementAtIndex(choiceSetsProperty.arraySize);
            var choiceSetProperty = choiceSetsProperty.GetArrayElementAtIndex(choiceSetsProperty.arraySize - 1);
            choiceSetProperty.FindPropertyRelative("title").stringValue = wordId;

            var newChoicesProperty = choiceSetProperty.FindPropertyRelative("choices");
            newChoicesProperty.ClearArray();
            newChoicesProperty.InsertArrayElementAtIndex(0);
            var choiceProperty = newChoicesProperty.GetArrayElementAtIndex(0);
            choiceProperty.FindPropertyRelative("wordId").stringValue = wordId;
            choiceProperty.FindPropertyRelative("tolerancePenalty").intValue = 0;
            choiceProperty.FindPropertyRelative("note").stringValue = string.Empty;

            selectedLevelSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedLevelSerializedObject.targetObject);
            statusMessage = $"已加入单选词组：{wordId}";
        }

        private void CreateAndAppendLevel()
        {
            if (levelSequenceSerializedObject == null)
            {
                statusMessage = "缺少关卡流程配置，无法创建关卡。";
                return;
            }

            EnsureFolder("Assets/ScriptableObjects/Gameplay");
            EnsureFolder(LevelFolderPath);
            EnsureFolder(DayFolderPath);

            var assetName = AssetDatabase.GenerateUniqueAssetPath($"{LevelFolderPath}/NewInformation.asset");
            var level = CreateInstance<InformationDefinition>();
            var serializedLevel = new SerializedObject(level);
            serializedLevel.FindProperty("id").stringValue = System.IO.Path.GetFileNameWithoutExtension(assetName);
            serializedLevel.FindProperty("informationText").stringValue = "信息提供者：";
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(level, assetName);
            AssetDatabase.SaveAssets();

            var day = GetOrCreateFirstDay();
            var daySerializedObject = new SerializedObject(day);
            daySerializedObject.Update();
            var levelsProperty = daySerializedObject.FindProperty("levels");
            levelsProperty.InsertArrayElementAtIndex(levelsProperty.arraySize);
            levelsProperty.GetArrayElementAtIndex(levelsProperty.arraySize - 1).objectReferenceValue = level;
            selectedLevelIndex = levelSequenceConfig.Count;
            daySerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(day);

            RefreshSelectedLevelSerializedObject();
            statusMessage = $"已创建关卡：{assetName}";
        }

        private DayDefinition GetOrCreateFirstDay()
        {
            if (levelSequenceConfig.Days.Count > 0 && levelSequenceConfig.Days[0] != null)
            {
                return levelSequenceConfig.Days[0];
            }

            var dayAssetName = AssetDatabase.GenerateUniqueAssetPath($"{DayFolderPath}/Day_001.asset");
            var day = CreateInstance<DayDefinition>();
            var serializedDay = new SerializedObject(day);
            serializedDay.FindProperty("title").stringValue = "Day 1";
            serializedDay.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(day, dayAssetName);
            AssetDatabase.SaveAssets();

            levelSequenceSerializedObject.Update();
            var daysProperty = levelSequenceSerializedObject.FindProperty("days");
            daysProperty.InsertArrayElementAtIndex(daysProperty.arraySize);
            daysProperty.GetArrayElementAtIndex(daysProperty.arraySize - 1).objectReferenceValue = day;
            levelSequenceSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelSequenceConfig);
            return day;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static TAsset LoadOrFindAsset<TAsset>(string path) where TAsset : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            if (asset != null)
            {
                return asset;
            }

            var guids = AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}");
            if (guids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<TAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static string GetLevelDisplayName(InformationDefinition level)
        {
            if (level == null)
            {
                return "<空>";
            }

            return string.IsNullOrWhiteSpace(level.Id) ? level.name : level.Id;
        }
    }
}
