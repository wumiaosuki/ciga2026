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
        private const string GradePenaltyConfigPath = "Assets/ScriptableObjects/Gameplay/Configs/DefaultGradePenaltyConfig.asset";
        private const string LevelFolderPath = "Assets/ScriptableObjects/Gameplay/Levels";

        private readonly string[] tabNames = { "关卡", "基础配置" };

        private LevelSequenceConfig levelSequenceConfig;
        private WordLibrary wordLibrary;
        private GameplaySessionConfig sessionConfig;
        private GradePenaltyConfig gradePenaltyConfig;

        private SerializedObject levelSequenceSerializedObject;
        private SerializedObject wordLibrarySerializedObject;
        private SerializedObject sessionConfigSerializedObject;
        private SerializedObject gradePenaltySerializedObject;
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
            return levelSequenceConfig != null && wordLibrary != null && sessionConfig != null && gradePenaltyConfig != null;
        }

        private void DrawMissingAssetsHelp()
        {
            EditorGUILayout.HelpBox("缺少一个或多个默认配置资产。点击“自动导入”会按默认路径和类型重新查找。", MessageType.Warning);

            levelSequenceConfig = (LevelSequenceConfig)EditorGUILayout.ObjectField("关卡流程", levelSequenceConfig, typeof(LevelSequenceConfig), false);
            wordLibrary = (WordLibrary)EditorGUILayout.ObjectField("词库", wordLibrary, typeof(WordLibrary), false);
            sessionConfig = (GameplaySessionConfig)EditorGUILayout.ObjectField("玩法配置", sessionConfig, typeof(GameplaySessionConfig), false);
            gradePenaltyConfig = (GradePenaltyConfig)EditorGUILayout.ObjectField("扣分配置", gradePenaltyConfig, typeof(GradePenaltyConfig), false);

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
            var levelsProperty = levelSequenceSerializedObject.FindProperty("levels");
            selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, Mathf.Max(0, levelsProperty.arraySize - 1));

            levelListScroll = EditorGUILayout.BeginScrollView(levelListScroll);
            for (var i = 0; i < levelsProperty.arraySize; i++)
            {
                var levelProperty = levelsProperty.GetArrayElementAtIndex(i);
                var level = levelProperty.objectReferenceValue as InformationDefinition;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var label = level != null ? $"{i + 1}. {GetLevelDisplayName(level)}" : $"{i + 1}. <空关卡>";
                    var isSelected = selectedLevelIndex == i;
                    if (GUILayout.Toggle(isSelected, label, "Button"))
                    {
                        SelectLevel(i);
                    }

                    if (GUILayout.Button("↑", GUILayout.Width(24f)) && i > 0)
                    {
                        levelsProperty.MoveArrayElement(i, i - 1);
                        selectedLevelIndex = i - 1;
                    }

                    if (GUILayout.Button("↓", GUILayout.Width(24f)) && i < levelsProperty.arraySize - 1)
                    {
                        levelsProperty.MoveArrayElement(i, i + 1);
                        selectedLevelIndex = i + 1;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(levelProperty, GUIContent.none);

                    if (GUILayout.Button("移除", GUILayout.Width(48f)))
                    {
                        levelsProperty.DeleteArrayElementAtIndex(i);
                        selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, Mathf.Max(0, levelsProperty.arraySize - 1));
                        break;
                    }
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
            DrawAvailableWordsEditor();

            EditorGUILayout.Space(8f);
            var answersProperty = selectedLevelSerializedObject.FindProperty("answerCombinations");
            EditorGUILayout.PropertyField(answersProperty, new GUIContent("评分档答案"), true);

            EditorGUILayout.EndScrollView();

            if (selectedLevelSerializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(selectedLevelSerializedObject.targetObject);
            }
        }

        private void DrawAvailableWordsEditor()
        {
            var wordsProperty = selectedLevelSerializedObject.FindProperty("availableWordIds");
            EditorGUILayout.LabelField("本关可用词语", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加空词 ID", GUILayout.Width(100f)))
                {
                    wordsProperty.InsertArrayElementAtIndex(wordsProperty.arraySize);
                    wordsProperty.GetArrayElementAtIndex(wordsProperty.arraySize - 1).stringValue = string.Empty;
                }

                if (GUILayout.Button("清空", GUILayout.Width(56f)) && EditorUtility.DisplayDialog("清空词语", "确认清空当前关卡的可用词语？", "清空", "取消"))
                {
                    wordsProperty.ClearArray();
                }
            }

            for (var i = 0; i < wordsProperty.arraySize; i++)
            {
                var wordProperty = wordsProperty.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    wordProperty.stringValue = EditorGUILayout.TextField(wordProperty.stringValue);
                    EditorGUILayout.LabelField(GetWordDisplayText(wordProperty.stringValue), GUILayout.Width(84f));

                    if (GUILayout.Button("删", GUILayout.Width(32f)))
                    {
                        wordsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
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
                gradePenaltyConfig = (GradePenaltyConfig)EditorGUILayout.ObjectField("评分扣分配置", gradePenaltyConfig, typeof(GradePenaltyConfig), false);
            }

            RebuildSerializedObjects();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawConfigObjectEditor("玩法会话配置", sessionConfigSerializedObject);
                DrawConfigObjectEditor("评分扣分配置", gradePenaltySerializedObject);
            }

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

            for (var i = 0; i < levelSequenceConfig.Levels.Count; i++)
            {
                var level = levelSequenceConfig.Levels[i];
                if (level == null)
                {
                    issues.Add($"第 {i + 1} 关为空。");
                    continue;
                }

                foreach (var wordId in level.AvailableWordIds)
                {
                    if (!wordIds.Contains(wordId))
                    {
                        issues.Add($"第 {i + 1} 关可用词不存在：{wordId}");
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
            gradePenaltyConfig = LoadOrFindAsset<GradePenaltyConfig>(GradePenaltyConfigPath);
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
            ApplySerializedObject(gradePenaltySerializedObject);
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
            gradePenaltySerializedObject = gradePenaltyConfig != null ? new SerializedObject(gradePenaltyConfig) : null;
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
            var wordsProperty = selectedLevelSerializedObject.FindProperty("availableWordIds");
            for (var i = 0; i < wordsProperty.arraySize; i++)
            {
                if (wordsProperty.GetArrayElementAtIndex(i).stringValue == wordId)
                {
                    statusMessage = $"当前关卡已包含词语：{wordId}";
                    return;
                }
            }

            wordsProperty.InsertArrayElementAtIndex(wordsProperty.arraySize);
            wordsProperty.GetArrayElementAtIndex(wordsProperty.arraySize - 1).stringValue = wordId;
            selectedLevelSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedLevelSerializedObject.targetObject);
            statusMessage = $"已加入词语：{wordId}";
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

            var assetName = AssetDatabase.GenerateUniqueAssetPath($"{LevelFolderPath}/NewInformation.asset");
            var level = CreateInstance<InformationDefinition>();
            var serializedLevel = new SerializedObject(level);
            serializedLevel.FindProperty("id").stringValue = System.IO.Path.GetFileNameWithoutExtension(assetName);
            serializedLevel.FindProperty("informationText").stringValue = "信息提供者：";
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(level, assetName);
            AssetDatabase.SaveAssets();

            levelSequenceSerializedObject.Update();
            var levelsProperty = levelSequenceSerializedObject.FindProperty("levels");
            levelsProperty.InsertArrayElementAtIndex(levelsProperty.arraySize);
            levelsProperty.GetArrayElementAtIndex(levelsProperty.arraySize - 1).objectReferenceValue = level;
            selectedLevelIndex = levelsProperty.arraySize - 1;
            levelSequenceSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelSequenceConfig);

            RefreshSelectedLevelSerializedObject();
            statusMessage = $"已创建关卡：{assetName}";
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

        private string GetWordDisplayText(string wordId)
        {
            if (wordLibrary == null || string.IsNullOrWhiteSpace(wordId))
            {
                return string.Empty;
            }

            return wordLibrary.TryGetWord(wordId, out var word) ? word.Text : "<缺失>";
        }
    }
}
