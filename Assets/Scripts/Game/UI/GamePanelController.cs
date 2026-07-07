using System.Collections;
using System.Collections.Generic;
using Ciga2026.Framework.UI;
using Ciga2026.Game;
using Ciga2026.Game.Audio;
using Ciga2026.Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// Game 界面交互控制器，负责词语点击提交、信息展示和容忍度反馈。
    /// </summary>
    [RequireComponent(typeof(GamePanelBind))]
    public sealed class GamePanelController : MonoBehaviour
    {
        [Header("绑定")]
        [Tooltip("Game 界面组件绑定。为空时会自动从当前对象获取。")]
        [SerializeField]
        private GamePanelBind bind;

        [Header("本局配置")]
        [Tooltip("全局词库，用于把词语 ID 转成玩家看到的文本。")]
        [SerializeField]
        private WordLibrary wordLibrary;

        [Tooltip("当前测试使用的信息配置。后续接流程时可以由外部切换。")]
        [SerializeField]
        private InformationDefinition informationDefinition;

        [Tooltip("多关卡流程配置。配置后会优先按列表顺序执行多个信息 SO。")]
        [SerializeField]
        private LevelSequenceConfig levelSequenceConfig;

        [Tooltip("玩法会话配置，包含初始容忍度和评分扣分表。")]
        [SerializeField]
        private GameplaySessionConfig sessionConfig;

        [Header("输入反馈")]
        [Tooltip("已选词之间的间隔。每个词本身会用 TMP 下划线标签标出。")]
        [SerializeField]
        private string selectedWordSeparator = "    ";

        [Tooltip("错误词在 InputText 中的富文本颜色。")]
        [SerializeField]
        private string wrongWordColor = "#FF3B30";

        [Tooltip("选择错误词时 InputText 抖动时长。")]
        [SerializeField]
        [Min(0f)]
        private float wrongWordShakeDuration = 0.1f;

        [Tooltip("选择错误词时 InputText 抖动强度，单位为 UI 像素。")]
        [SerializeField]
        [Min(0f)]
        private float wrongWordShakeStrength = 10f;

        [Header("信息面板自适应")]
        [Tooltip("InfoPanel 的最小高度，避免短文本时面板被压得过低。")]
        [SerializeField]
        [Min(1f)]
        private float informationPanelMinHeight = 120f;

        [Tooltip("InfoPanel 的最大高度，避免长文本盖住过多界面。")]
        [SerializeField]
        [Min(1f)]
        private float informationPanelMaxHeight = 520f;

        [Tooltip("信息文本上下留白。实际会和 prefab 当前留白取较大值。")]
        [SerializeField]
        [Min(0f)]
        private float informationPanelVerticalPadding = 78f;

        [Header("电视图片")]
        [Tooltip("TVShow 图片在 Resources 下的文件夹路径。例如图片在 Assets/Resources/Textures/11.jpg，则这里填 Textures。")]
        [SerializeField]
        private string tvShowResourceFolder = "Textures";

        [Header("失败弹窗")]
        [Tooltip("失败弹窗 prefab 在 Resources 下的路径，不包含扩展名。")]
        [SerializeField]
        private string failurePanelResourcePath = "Prefab/FailiuePanel";

        [Header("角色动画")]
        [Tooltip("CharacterStage 下的 Spine 动画控制器，用于根据选词和天数进度切换角色动画。")]
        [SerializeField]
        private GameCharacterSpineAnimator characterAnimator;

        [Header("天数流程")]
        [Tooltip("进入新的一天时触发。参数为从 1 开始的天数编号。暂时不做效果时可以不绑定。")]
        [SerializeField]
        private UnityEvent<int> dayStarted;

        [Header("评分展示")]
        [Tooltip("不同评分档位对应的图片。")]
        [SerializeField]
        private List<GradeSpriteEntry> gradeSprites = new();

        [Header("容忍度头像")]
        [Tooltip("按容忍度百分比分档切换头像。匹配时会优先使用最低百分比最高的条目。")]
        [SerializeField]
        private List<ToleranceHeadSpriteEntry> toleranceHeadSprites = new();

        [Tooltip("评分图片淡入时间。")]
        [SerializeField]
        [Min(0f)]
        private float gradeFadeInDuration = 0.18f;

        [Tooltip("评分图片停留时间。")]
        [SerializeField]
        [Min(0f)]
        private float gradeVisibleDuration = 0.65f;

        [Tooltip("评分图片淡出时间。")]
        [SerializeField]
        [Min(0f)]
        private float gradeFadeOutDuration = 0.35f;

        private readonly List<string> submittedWordIds = new();
        private readonly List<string> selectedWordIds = new();
        private readonly List<string> selectedWordOutputSegments = new();
        private readonly List<WordChoiceSet> remainingChoiceSets = new();
        private readonly List<WordChoiceSet> visibleChoiceSets = new();
        private readonly Dictionary<DraggableWordItem, WordChoiceEntry> wordEntriesByItem = new();
        private readonly Dictionary<DraggableWordItem, WordChoiceSet> choiceSetsByItem = new();
        private BroadcastGameplaySession session;
        private int currentLevelIndex;
        private int currentDayIndex = -1;
        private float remainingLevelTime;
        private float currentLevelDuration = 5f;
        private int displayedTimerCentiseconds = -1;
        private bool isTimerRunning;
        private bool isResolvingSubmission;
        private Coroutine gradeDisplayCoroutine;
        private Coroutine inputTextShakeCoroutine;
        private Vector2 inputTextDefaultAnchoredPosition;
        private bool hasInputTextDefaultAnchoredPosition;
        private float initialInformationPanelHeight = -1f;
        private SettlementMode settlementMode;
        private InformationDefinition pendingDayLevel;
        private int pendingDayIndex = -1;
        private SentenceEvaluationResult pendingDayResult;
        private bool pendingDayWasTimeout;
        private const string FailurePanelId = "FailiuePanel";

        private enum SettlementMode
        {
            None,
            FinalSettlement,
            DayTransition
        }

        [System.Serializable]
        private sealed class GradeSpriteEntry
        {
            [Tooltip("评分档位。")]
            public AnswerGrade grade;

            [Tooltip("该评分档位对应的图片。")]
            public Sprite sprite;
        }

        [System.Serializable]
        private sealed class ToleranceHeadSpriteEntry
        {
            [Tooltip("该头像生效的最低容忍度百分比，1 表示 100%，0.5 表示 50%。")]
            [Range(0f, 1f)]
            public float minTolerancePercent;

            [Tooltip("容忍度到达该档位时显示的头像图片。")]
            public Sprite sprite;
        }

        private void Awake()
        {
            bind = bind != null ? bind : GetComponent<GamePanelBind>();
        }

        private void OnEnable()
        {
            if (bind != null && bind.submitButton != null)
            {
                bind.submitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (bind != null && bind.gmButton != null)
            {
                bind.gmButton.onClick.AddListener(OnGmButtonClicked);
            }

            if (bind != null && bind.gameExitButton != null)
            {
                bind.gameExitButton.onClick.AddListener(OnGameExitClicked);
            }

            if (bind != null && bind.settlementRestartButton != null)
            {
                bind.settlementRestartButton.onClick.AddListener(OnSettlementRestartClicked);
            }

            if (bind != null && bind.settlementExitButton != null)
            {
                bind.settlementExitButton.onClick.AddListener(OnSettlementExitClicked);
            }

            StartLevelSequence();
        }

        private void OnDisable()
        {
            if (bind != null && bind.submitButton != null)
            {
                bind.submitButton.onClick.RemoveListener(OnSubmitClicked);
            }

            if (bind != null && bind.gmButton != null)
            {
                bind.gmButton.onClick.RemoveListener(OnGmButtonClicked);
            }

            if (bind != null && bind.gameExitButton != null)
            {
                bind.gameExitButton.onClick.RemoveListener(OnGameExitClicked);
            }

            if (bind != null && bind.settlementRestartButton != null)
            {
                bind.settlementRestartButton.onClick.RemoveListener(OnSettlementRestartClicked);
            }

            if (bind != null && bind.settlementExitButton != null)
            {
                bind.settlementExitButton.onClick.RemoveListener(OnSettlementExitClicked);
            }

            isTimerRunning = false;
        }

        private void Update()
        {
            if (!isTimerRunning || session == null || session.IsGameOver)
            {
                return;
            }

            remainingLevelTime = Mathf.Max(0f, remainingLevelTime - Time.deltaTime);
            RefreshTimerView();

            if (remainingLevelTime <= 0f)
            {
                HandleSelectionTimerExpired();
            }
        }

        private void OnWordItemClicked(DraggableWordItem item)
        {
            if (!isTimerRunning || session == null || session.IsGameOver || item == null || bind == null || item.CurrentZone != bind.wordLibraryDropZone)
            {
                return;
            }

            SelectWordItem(item);
            characterAnimator?.PlayPlayerSelectAnimation();
        }

        private void SelectWordItem(DraggableWordItem item)
        {
            if (!wordEntriesByItem.TryGetValue(item, out var entry) || !choiceSetsByItem.TryGetValue(item, out var choiceSet))
            {
                return;
            }

            var isCorrectChoiceInOrder = IsCorrectChoiceInCurrentOrder(choiceSet, entry);
            var effectivePenalty = GetEffectiveSelectionPenalty(choiceSet, entry);

            session.ApplyWordSelectionPenalty(effectivePenalty);
            GameAudioPlayer.PlayRandomVoice(sessionConfig);
            GameAudioPlayer.PlayPenaltyWarningIfNeeded(sessionConfig, effectivePenalty);
            AppendSelectedWord(entry, isCorrectChoiceInOrder);
            wordEntriesByItem.Remove(item);
            choiceSetsByItem.Remove(item);
            RemoveChoiceSetFromLibrary(choiceSet, item);
            DestroyWordItem(item.gameObject);
            visibleChoiceSets.Remove(choiceSet);
            RefillVisibleChoiceSets();
            RefreshToleranceView();
            RefreshSubmitInteractable();
            ShakeInputTextIfWrong(isCorrectChoiceInOrder);

            if (session.IsGameOver)
            {
                SetFeedback($"选择“{GetWordText(entry)}”，扣除 {effectivePenalty}，容忍度归零。");
                SetSubmitInteractable(false);
                ShowFailurePanel();
                return;
            }

            RefreshSelectionTimerAfterChoice();
            SetFeedback($"选择“{GetWordText(entry)}”，扣除 {effectivePenalty}，本关累计扣除 {session.CurrentRoundPenalty}。");
        }

        /// <summary>
        /// 切换并开始一条新的信息流程。
        /// </summary>
        /// <param name="information">本轮信息配置。</param>
        public void StartRound(InformationDefinition information)
        {
            informationDefinition = information;

            if (session == null)
            {
                session = new BroadcastGameplaySession(sessionConfig);
            }

            ClearWordItems();
            ClearChoiceRuntimeState();
            ClearSelectedWords();
            session.BeginRound();
            RefreshStaticTexts();
            RefreshTvShowForCurrentInformation();
            RefreshToleranceView();
            PrepareChoiceQueue();
            RefillVisibleChoiceSets();
            ResetSelectionTimer();
            characterAnimator?.ResetForLevel();
            RefreshCharacterDayAnimation();
            RefreshSubmitInteractable();
        }

        /// <summary>
        /// 从第一关开始执行关卡流程。
        /// </summary>
        public void StartLevelSequence()
        {
            sessionConfig?.ReloadRuntimeConfig();
            GameAudioPlayer.PlayGameBgm(sessionConfig);
            session = new BroadcastGameplaySession(sessionConfig);
            currentLevelIndex = 0;
            currentDayIndex = -1;
            ClearPendingDayTransition();
            HideSettlement();
            HideGradeImage();
            ReleaseFailurePanel();

            if (TryGetLevel(currentLevelIndex, out var firstLevel, out var firstDayIndex, out _, out var isFirstLevelOfDay))
            {
                NotifyDayIfNeeded(firstDayIndex, isFirstLevelOfDay);
                StartRound(firstLevel);
                return;
            }

            StartRound(informationDefinition);
        }

        private void OnSubmitClicked()
        {
            GameAudioPlayer.PlayUiClick(sessionConfig);
            ResolveSubmission(allowEmptyAnswer: false, isTimeout: false);
        }

        private void OnGmButtonClicked()
        {
            GameAudioPlayer.PlayUiClick(sessionConfig);
            ClearWordItems();
            HideTvShowImage();
            isTimerRunning = false;
            remainingLevelTime = 0f;
            RefreshTimerView();
            SetSubmitInteractable(false);
            SetFeedback("GM：直接进入胜利结算。");
            ShowSettlement(
                isVictory: true,
                title: "播报完成",
                message: session != null
                    ? $"GM 测试胜利，当前容忍度 {session.CurrentTolerance}/{session.MaxTolerance}。"
                    : "GM 测试胜利。");
        }

        private void OnGameExitClicked()
        {
            GameAudioPlayer.PlayUiClick(sessionConfig);
            ReturnToMainMenuFromGame();
        }

        private void ResolveSubmission(bool allowEmptyAnswer, bool isTimeout)
        {
            if (informationDefinition == null)
            {
                SetFeedback("未配置本局信息。");
                return;
            }

            submittedWordIds.Clear();
            submittedWordIds.AddRange(selectedWordIds);

            if (!allowEmptyAnswer && submittedWordIds.Count == 0)
            {
                SetFeedback("请先点击词语加入输入框。");
                return;
            }

            if (!allowEmptyAnswer && HasDrawableWords())
            {
                SetFeedback("还有可选择的词语，全部选择完成后才能提交。");
                return;
            }

            if (isResolvingSubmission)
            {
                return;
            }

            isResolvingSubmission = true;
            isTimerRunning = false;

            var result = session.SubmitAnswer(informationDefinition, submittedWordIds);
            HideTvShowImage();
            RefreshToleranceView();
            RefreshTimerView();
            SetFeedback(BuildFeedbackText(result, isTimeout));
            TryShowGradeImage(result);

            if (result.IsGameOver)
            {
                SetSubmitInteractable(false);
                ShowFailurePanel();
                isResolvingSubmission = false;
                return;
            }

            TryAdvanceLevel(result, isTimeout);
            isResolvingSubmission = false;
        }

        private void TryAdvanceLevel(SentenceEvaluationResult result, bool isTimeout)
        {
            if (levelSequenceConfig == null)
            {
                return;
            }

            currentLevelIndex++;

            if (TryGetLevel(currentLevelIndex, out var nextLevel, out var dayIndex, out _, out var isFirstLevelOfDay))
            {
                if (!result.IsGameOver && !session.IsGameOver && isFirstLevelOfDay && currentDayIndex >= 0 && dayIndex != currentDayIndex)
                {
                    ShowDayTransitionPanel(nextLevel, dayIndex, result, isTimeout);
                    return;
                }

                NotifyDayIfNeeded(dayIndex, isFirstLevelOfDay);
                StartRound(nextLevel);
                SetFeedback($"{BuildRoundSummaryText(result, isTimeout)}进入第 {currentLevelIndex + 1} 关。");
                return;
            }

            ClearWordItems();
            HideTvShowImage();
            isTimerRunning = false;
            remainingLevelTime = 0f;
            RefreshTimerView();

            if (bind != null && bind.informationText != null)
            {
                bind.informationText.text = "全部信息播报完成。";
                RefreshInformationPanelHeight();
            }

            SetFeedback($"全部关卡完成，最终容忍度 {session.CurrentTolerance}/{session.MaxTolerance}。");
            SetSubmitInteractable(false);
            ShowSettlement(
                isVictory: true,
                title: "播报完成",
                message: $"全部信息播报完成，最终容忍度 {session.CurrentTolerance}/{session.MaxTolerance}。");
        }

        private void OnSettlementRestartClicked()
        {
            GameAudioPlayer.PlayUiClick(sessionConfig);

            if (settlementMode == SettlementMode.DayTransition)
            {
                ContinuePendingDay();
                return;
            }

            StartLevelSequence();
        }

        private void OnSettlementExitClicked()
        {
            GameAudioPlayer.PlayUiClick(sessionConfig);

            if (settlementMode == SettlementMode.DayTransition)
            {
                ReturnToMainMenuFromGame();
                return;
            }

            if (GameStateManager.TryGetInstance(out var gameStateManager))
            {
                gameStateManager.ExitGame();
            }
        }

        private void ShowDayTransitionPanel(InformationDefinition nextLevel, int nextDayIndex, SentenceEvaluationResult result, bool isTimeout)
        {
            pendingDayLevel = nextLevel;
            pendingDayIndex = nextDayIndex;
            pendingDayResult = result;
            pendingDayWasTimeout = isTimeout;
            settlementMode = SettlementMode.DayTransition;
            isTimerRunning = false;
            SetSubmitInteractable(false);
            HideGradeImage();
            HideTvShowImage();

            if (bind == null || bind.settlementPanel == null)
            {
                ContinuePendingDay();
                return;
            }

            bind.settlementPanel.SetActive(true);
            SetSettlementButtonLabels("继续下一天", "返回主菜单");
            SetSettlementTextVisible(false);
        }

        private void ContinuePendingDay()
        {
            if (pendingDayLevel == null)
            {
                HideSettlement();
                return;
            }

            var nextLevel = pendingDayLevel;
            var nextDayIndex = pendingDayIndex;
            var previousResult = pendingDayResult;
            var previousWasTimeout = pendingDayWasTimeout;
            ClearPendingDayTransition();
            HideSettlement();
            NotifyDayIfNeeded(nextDayIndex, isFirstLevelOfDay: true);
            StartRound(nextLevel);
            SetFeedback($"{BuildRoundSummaryText(previousResult, previousWasTimeout)}进入第 {currentLevelIndex + 1} 关。");
        }

        private void ReturnToMainMenuFromGame()
        {
            ClearPendingDayTransition();
            HideSettlement();
            ClearWordItems();
            HideTvShowImage();
            isTimerRunning = false;

            if (UIManager.TryGetInstance(out var uiManager))
            {
                uiManager.ReleasePanel(FailurePanelId);
                uiManager.ReleasePanel(UIResourceType.Game.ToString());
                uiManager.ShowPanel(UIResourceType.StartMeau, true);
            }

            if (GameStateManager.TryGetInstance(out var gameStateManager))
            {
                gameStateManager.GoToMainMenu();
            }
        }

        private void ClearPendingDayTransition()
        {
            pendingDayLevel = null;
            pendingDayIndex = -1;
            pendingDayResult = default;
            pendingDayWasTimeout = false;
        }

        private void ShowFailurePanel()
        {
            isTimerRunning = false;
            SetSubmitInteractable(false);
            HideGradeImage();
            HideSettlement();
            GameAudioPlayer.PlayFailure(sessionConfig);

            if (!UIManager.TryGetInstance(out var uiManager))
            {
                return;
            }

            var failurePanelPrefab = Resources.Load<GameObject>(failurePanelResourcePath);
            if (failurePanelPrefab == null)
            {
                Debug.LogWarning($"GamePanelController.ShowFailurePanel failed: Resources path '{failurePanelResourcePath}' not found.");
                return;
            }

            uiManager.OpenPanel(failurePanelPrefab, panelId: FailurePanelId, layer: UILayer.Popup, reuseExisting: true);
        }

        private static void ReleaseFailurePanel()
        {
            if (UIManager.TryGetInstance(out var uiManager))
            {
                uiManager.ReleasePanel(FailurePanelId);
            }
        }

        private void ResetSelectionTimer()
        {
            currentLevelDuration = GetLevelDuration();
            remainingLevelTime = currentLevelDuration;
            displayedTimerCentiseconds = -1;
            isTimerRunning = informationDefinition != null && session != null && !session.IsGameOver && HasDrawableWords();
            RefreshTimerView();
        }

        private void RefreshSelectionTimerAfterChoice()
        {
            if (HasDrawableWords())
            {
                ResetSelectionTimer();
                return;
            }

            currentLevelDuration = GetLevelDuration();
            remainingLevelTime = currentLevelDuration;
            displayedTimerCentiseconds = -1;
            isTimerRunning = informationDefinition != null && session != null && !session.IsGameOver;
            RefreshTimerView();
        }

        private void HandleSelectionTimerExpired()
        {
            if (session == null || session.IsGameOver)
            {
                return;
            }

            if (!HasDrawableWords())
            {
                ResolveSubmission(allowEmptyAnswer: true, isTimeout: true);
                return;
            }

            var penalty = sessionConfig != null ? sessionConfig.SelectionTimeoutPenalty : 10;
            session.ApplySelectionTimeoutPenalty(penalty);
            GameAudioPlayer.PlayPenaltyWarningIfNeeded(sessionConfig, penalty);
            RefreshToleranceView();

            if (session.IsGameOver)
            {
                SetFeedback($"选词超时，扣除 {penalty}，容忍度归零。");
                SetSubmitInteractable(false);
                ShowFailurePanel();
                return;
            }

            ResetSelectionTimer();
            SetFeedback($"选词超时，扣除 {penalty} 容忍度，评分扣分 {session.CurrentRoundPenalty}。");
        }

        private float GetLevelDuration()
        {
            const float defaultLevelDuration = 5f;

            if (sessionConfig == null)
            {
                return defaultLevelDuration;
            }

            return sessionConfig.GetLevelDuration(currentLevelIndex, GetLevelCount());
        }

        private int GetLevelCount()
        {
            return levelSequenceConfig != null && levelSequenceConfig.Count > 0
                ? levelSequenceConfig.Count
                : 1;
        }

        private void PrepareChoiceQueue()
        {
            remainingChoiceSets.Clear();
            visibleChoiceSets.Clear();

            if (informationDefinition == null)
            {
                return;
            }

            for (var i = 0; i < informationDefinition.ChoiceSets.Count; i++)
            {
                var choiceSet = informationDefinition.ChoiceSets[i];
                if (choiceSet != null && choiceSet.Choices.Count > 0)
                {
                    remainingChoiceSets.Add(choiceSet);
                }
            }
        }

        private void RefillVisibleChoiceSets()
        {
            if (bind == null || bind.wordItemPrefab == null || bind.wordLibraryDropZone == null || informationDefinition == null)
            {
                return;
            }

            while (visibleChoiceSets.Count < informationDefinition.VisibleChoiceSetLimit && remainingChoiceSets.Count > 0)
            {
                var choiceSet = remainingChoiceSets[0];
                remainingChoiceSets.RemoveAt(0);
                var spawnedCount = SpawnChoiceSet(choiceSet);
                if (spawnedCount > 0)
                {
                    visibleChoiceSets.Add(choiceSet);
                }
            }
        }

        private int SpawnChoiceSet(WordChoiceSet choiceSet)
        {
            if (choiceSet == null)
            {
                return 0;
            }

            var spawnedCount = 0;
            for (var i = 0; i < choiceSet.Choices.Count; i++)
            {
                var entry = choiceSet.Choices[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.WordId))
                {
                    continue;
                }

                var item = Instantiate(bind.wordItemPrefab, bind.wordLibraryDropZone.ContentRoot);
                item.gameObject.SetActive(true);
                item.Clicked += OnWordItemClicked;
                item.Initialize(entry.WordId, GetWordText(entry), bind.wordLibraryDropZone);
                wordEntriesByItem[item] = entry;
                choiceSetsByItem[item] = choiceSet;
                spawnedCount++;
            }

            return spawnedCount;
        }

        private void RemoveChoiceSetFromLibrary(WordChoiceSet choiceSet, DraggableWordItem selectedItem)
        {
            if (bind == null || bind.wordLibraryContent == null || choiceSet == null)
            {
                return;
            }

            for (var i = bind.wordLibraryContent.childCount - 1; i >= 0; i--)
            {
                var item = bind.wordLibraryContent.GetChild(i).GetComponent<DraggableWordItem>();
                if (item == null || item == selectedItem || !choiceSetsByItem.TryGetValue(item, out var itemChoiceSet) || itemChoiceSet != choiceSet)
                {
                    continue;
                }

                wordEntriesByItem.Remove(item);
                choiceSetsByItem.Remove(item);
                DestroyWordItem(item.gameObject);
            }
        }

        private void CollectSubmittedWordIds(List<string> results)
        {
            if (bind == null || bind.inputContent == null)
            {
                return;
            }

            for (var i = 0; i < bind.inputContent.childCount; i++)
            {
                var item = bind.inputContent.GetChild(i).GetComponent<DraggableWordItem>();
                if (item != null)
                {
                    results.Add(item.WordId);
                }
            }
        }

        private void AppendSelectedWord(WordChoiceEntry entry, bool isCorrectChoice)
        {
            if (entry == null)
            {
                return;
            }

            selectedWordIds.Add(entry.WordId);

            var wordText = $"<u>{EscapeRichText(GetWordText(entry))}</u>";
            if (!isCorrectChoice)
            {
                wordText = $"<color={wrongWordColor}>{wordText}</color>";
            }

            selectedWordOutputSegments.Add(wordText);
            RefreshInputText();
        }

        private void RefreshInputText()
        {
            if (bind == null || bind.inputText == null)
            {
                return;
            }

            bind.inputText.richText = true;
            bind.inputText.text = string.Join(selectedWordSeparator, selectedWordOutputSegments);
        }

        private void ClearSelectedWords()
        {
            selectedWordIds.Clear();
            selectedWordOutputSegments.Clear();
            RefreshInputText();
        }

        private void ShakeInputTextIfWrong(bool isCorrectChoice)
        {
            if (isCorrectChoice || bind == null || bind.inputText == null)
            {
                return;
            }

            var inputTextRect = bind.inputText.rectTransform;
            if (!hasInputTextDefaultAnchoredPosition)
            {
                inputTextDefaultAnchoredPosition = inputTextRect.anchoredPosition;
                hasInputTextDefaultAnchoredPosition = true;
            }

            if (inputTextShakeCoroutine != null)
            {
                StopCoroutine(inputTextShakeCoroutine);
                inputTextRect.anchoredPosition = inputTextDefaultAnchoredPosition;
            }

            inputTextShakeCoroutine = StartCoroutine(ShakeInputText(inputTextRect));
        }

        private int GetEffectiveSelectionPenalty(WordChoiceSet choiceSet, WordChoiceEntry entry)
        {
            if (entry == null || IsCorrectChoiceInCurrentOrder(choiceSet, entry))
            {
                return 0;
            }

            return entry.TolerancePenalty;
        }

        private bool IsCorrectChoiceInCurrentOrder(WordChoiceSet choiceSet, WordChoiceEntry entry)
        {
            if (!IsCorrectChoice(choiceSet, entry) || informationDefinition == null)
            {
                return false;
            }

            var expectedChoiceSetIndex = selectedWordIds.Count;
            return expectedChoiceSetIndex >= 0
                && expectedChoiceSetIndex < informationDefinition.ChoiceSets.Count
                && ReferenceEquals(informationDefinition.ChoiceSets[expectedChoiceSetIndex], choiceSet);
        }

        private static bool IsCorrectChoice(WordChoiceSet choiceSet, WordChoiceEntry entry)
        {
            return choiceSet != null
                && entry != null
                && choiceSet.Choices.Count > 0
                && ReferenceEquals(choiceSet.Choices[0], entry);
        }

        private IEnumerator ShakeInputText(RectTransform target)
        {
            if (target == null || wrongWordShakeDuration <= 0f || wrongWordShakeStrength <= 0f)
            {
                inputTextShakeCoroutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < wrongWordShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / wrongWordShakeDuration);
                var strength = Mathf.Lerp(wrongWordShakeStrength, 0f, normalized);
                target.anchoredPosition = inputTextDefaultAnchoredPosition + Random.insideUnitCircle * strength;
                yield return null;
            }

            target.anchoredPosition = inputTextDefaultAnchoredPosition;
            inputTextShakeCoroutine = null;
        }

        private static string EscapeRichText(string text)
        {
            return string.IsNullOrEmpty(text)
                ? string.Empty
                : text.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private void ClearWordItems()
        {
            if (bind == null)
            {
                return;
            }

            ClearChildren(bind.wordLibraryContent);
            ClearChildren(bind.inputContent);
            ClearChoiceRuntimeState();
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void RefreshStaticTexts()
        {
            if (bind == null)
            {
                return;
            }

            if (bind.informationText != null)
            {
                bind.informationText.richText = false;
                bind.informationText.text = informationDefinition != null ? GetVisibleInformationText(informationDefinition.InformationText) : "未配置本局信息";
                RefreshInformationPanelHeight();
            }

            SetFeedback(informationDefinition != null ? "选择词语会立即影响容忍度，全部选择完成后提交。" : "请先配置 InformationDefinition。");
        }

        private void RefreshTvShowForCurrentInformation()
        {
            if (!TryGetInformationImageKey(informationDefinition != null ? informationDefinition.InformationText : null, out var imageKey))
            {
                HideTvShowImage();
                return;
            }

            var sprite = LoadTvShowSprite(imageKey);
            if (sprite == null)
            {
                Debug.LogWarning($"GamePanelController failed to load TVShow image: Resources/{tvShowResourceFolder}/{imageKey}");
                HideTvShowImage();
                return;
            }

            if (bind == null || bind.tvShowImage == null)
            {
                return;
            }

            bind.tvShowImage.sprite = sprite;
            bind.tvShowImage.preserveAspect = true;
            bind.tvShowImage.gameObject.SetActive(true);
        }

        private void HideTvShowImage()
        {
            if (bind == null || bind.tvShowImage == null)
            {
                return;
            }

            bind.tvShowImage.gameObject.SetActive(false);
        }

        private Sprite LoadTvShowSprite(string imageKey)
        {
            imageKey = SanitizeResourceKey(imageKey);
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }

            var resourcePath = string.IsNullOrWhiteSpace(tvShowResourceFolder)
                ? imageKey
                : $"{tvShowResourceFolder.Trim('/')}/{imageKey}";

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            return texture != null
                ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
        }

        private static bool TryGetInformationImageKey(string informationText, out string imageKey)
        {
            imageKey = null;

            if (string.IsNullOrWhiteSpace(informationText))
            {
                return false;
            }

            var trimmedText = informationText.Trim();
            if (trimmedText.Length <= 1 || trimmedText[0] != '&')
            {
                return false;
            }

            imageKey = trimmedText.Substring(1);
            return true;
        }

        private string GetVisibleInformationText(string informationText)
        {
            return TryGetInformationImageKey(informationText, out _)
                ? "<----------"
                : informationText;
        }

        private static bool IsResourceKeyCharacter(char character)
        {
            return char.IsLetterOrDigit(character) || character == '_' || character == '-' || character == '/';
        }

        private static string SanitizeResourceKey(string imageKey)
        {
            if (string.IsNullOrWhiteSpace(imageKey))
            {
                return string.Empty;
            }

            var trimmedKey = imageKey.Trim();
            for (var i = 0; i < trimmedKey.Length; i++)
            {
                if (!IsResourceKeyCharacter(trimmedKey[i]))
                {
                    return string.Empty;
                }
            }

            return trimmedKey;
        }

        private void RefreshInformationPanelHeight()
        {
            if (bind == null || bind.informationText == null)
            {
                return;
            }

            var panelRect = GetInfoPanelRect();
            var textRect = bind.informationText.rectTransform;
            if (panelRect == null || textRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            bind.informationText.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);

            var textWidth = Mathf.Max(1f, textRect.rect.width);
            var preferredTextHeight = bind.informationText
                .GetPreferredValues(bind.informationText.text, textWidth, Mathf.Infinity)
                .y;

            if (initialInformationPanelHeight < 0f)
            {
                initialInformationPanelHeight = Mathf.Max(1f, panelRect.rect.height);
            }

            var currentFramePadding = Mathf.Max(0f, panelRect.rect.height - textRect.rect.height);
            var verticalPadding = Mathf.Max(informationPanelVerticalPadding, currentFramePadding);
            var minHeight = Mathf.Max(1f, informationPanelMinHeight, initialInformationPanelHeight);
            var maxHeight = Mathf.Max(minHeight, informationPanelMaxHeight);
            var targetHeight = Mathf.Clamp(Mathf.Ceil(preferredTextHeight + verticalPadding), minHeight, maxHeight);

            var bottomBeforeResize = GetWorldBottom(panelRect);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            KeepWorldBottom(panelRect, bottomBeforeResize);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        private static float GetWorldBottom(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return corners[0].y;
        }

        private static void KeepWorldBottom(RectTransform rectTransform, float targetWorldBottom)
        {
            var bottomAfterResize = GetWorldBottom(rectTransform);
            var worldOffset = targetWorldBottom - bottomAfterResize;
            if (Mathf.Approximately(worldOffset, 0f))
            {
                return;
            }

            rectTransform.position += new Vector3(0f, worldOffset, 0f);
        }

        private RectTransform GetInfoPanelRect()
        {
            if (bind == null)
            {
                return null;
            }

            if (bind.infoPanel != null)
            {
                return bind.infoPanel;
            }

            return bind.informationText != null
                ? bind.informationText.transform.parent as RectTransform
                : null;
        }

        private void RefreshToleranceView()
        {
            if (bind == null || session == null)
            {
                return;
            }

            var tolerancePercent = (float)session.CurrentTolerance / session.MaxTolerance;

            if (bind.toleranceFillImage != null)
            {
                SetProgressImage(bind.toleranceFillImage, tolerancePercent);
            }

            if (bind.toleranceText != null)
            {
                bind.toleranceText.text = $"{session.CurrentTolerance}/{session.MaxTolerance}";
            }

            RefreshHeadImage(tolerancePercent);
        }

        private void RefreshTimerView()
        {
            if (bind == null)
            {
                return;
            }

            SetProgressImage(bind.timerFillImage, currentLevelDuration > 0f ? remainingLevelTime / currentLevelDuration : 0f);

            if (bind.timerText != null)
            {
                var centiseconds = Mathf.CeilToInt(Mathf.Max(0f, remainingLevelTime) * 100f);
                if (centiseconds != displayedTimerCentiseconds)
                {
                    displayedTimerCentiseconds = centiseconds;
                    bind.timerText.text = (centiseconds / 100f).ToString("0.00");
                }
            }
        }

        private bool HasDrawableWords()
        {
            return remainingChoiceSets.Count > 0 || visibleChoiceSets.Count > 0;
        }

        private void RefreshSubmitInteractable()
        {
            SetSubmitInteractable(informationDefinition != null && session != null && !session.IsGameOver && !HasDrawableWords());
        }

        private string GetWordText(string wordId)
        {
            return wordLibrary != null ? wordLibrary.GetDisplayText(wordId) : wordId;
        }

        private string GetWordText(WordChoiceEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(entry.DisplayText)
                ? entry.DisplayText
                : GetWordText(entry.WordId);
        }

        private void ClearChoiceRuntimeState()
        {
            remainingChoiceSets.Clear();
            visibleChoiceSets.Clear();
            wordEntriesByItem.Clear();
            choiceSetsByItem.Clear();
        }

        private static void DestroyWordItem(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            item.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(item);
            }
            else
            {
                DestroyImmediate(item);
            }
        }

        private bool TryGetLevel(int index, out InformationDefinition level, out int dayIndex, out int dayLevelIndex, out bool isFirstLevelOfDay)
        {
            if (levelSequenceConfig != null)
            {
                return levelSequenceConfig.TryGetLevel(index, out level, out dayIndex, out dayLevelIndex, out isFirstLevelOfDay);
            }

            level = index == 0 ? informationDefinition : null;
            dayIndex = 0;
            dayLevelIndex = index;
            isFirstLevelOfDay = index == 0;
            return level != null;
        }

        private void NotifyDayIfNeeded(int dayIndex, bool isFirstLevelOfDay)
        {
            if (!isFirstLevelOfDay || dayIndex < 0 || dayIndex == currentDayIndex)
            {
                return;
            }

            currentDayIndex = dayIndex;
            dayStarted?.Invoke(dayIndex + 1);
        }

        private void RefreshCharacterDayAnimation()
        {
            if (characterAnimator == null)
            {
                return;
            }

            var dayStageIndex = GetCurrentDayStageIndex();
            characterAnimator.RefreshNpcByDayStage(dayStageIndex);
        }

        private int GetCurrentDayStageIndex()
        {
            const int npcDayStageCount = 3;

            var dayCount = GetConfiguredDayCount();
            var clampedDayIndex = Mathf.Clamp(currentDayIndex, 0, dayCount - 1);
            var stageIndex = Mathf.FloorToInt((float)clampedDayIndex * npcDayStageCount / dayCount);
            return Mathf.Clamp(stageIndex, 0, npcDayStageCount - 1);
        }

        private int GetConfiguredDayCount()
        {
            if (levelSequenceConfig == null || levelSequenceConfig.Days == null)
            {
                return 1;
            }

            var count = 0;
            for (var i = 0; i < levelSequenceConfig.Days.Count; i++)
            {
                if (levelSequenceConfig.Days[i] != null)
                {
                    count++;
                }
            }

            return Mathf.Max(1, count);
        }

        private static void SetProgressImage(Image image, float normalizedValue)
        {
            if (image == null)
            {
                return;
            }

            var value = Mathf.Clamp01(normalizedValue);
            image.fillAmount = value;

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(value, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void SetFeedback(string message)
        {
            if (bind != null && bind.feedbackText != null)
            {
                bind.feedbackText.text = message;
            }
        }

        private void SetSubmitInteractable(bool interactable)
        {
            if (bind != null && bind.submitButton != null)
            {
                bind.submitButton.interactable = interactable;
            }
        }

        private void TryShowGradeImage(SentenceEvaluationResult result)
        {
            if (!result.IsMatched || !result.Grade.HasValue || bind == null || bind.gradeImage == null)
            {
                return;
            }

            var sprite = FindGradeSprite(result.Grade.Value);
            if (sprite == null)
            {
                return;
            }

            if (gradeDisplayCoroutine != null)
            {
                StopCoroutine(gradeDisplayCoroutine);
                gradeDisplayCoroutine = null;
            }

            bind.gradeImage.sprite = sprite;
            bind.gradeImage.gameObject.SetActive(true);
            SetGradeImageAlpha(0f);
            gradeDisplayCoroutine = StartCoroutine(PlayGradeImage());
        }

        private Sprite FindGradeSprite(AnswerGrade grade)
        {
            for (var i = 0; i < gradeSprites.Count; i++)
            {
                var entry = gradeSprites[i];
                if (entry != null && entry.grade == grade && entry.sprite != null)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private void RefreshHeadImage(float tolerancePercent)
        {
            if (bind == null || bind.headImage == null)
            {
                return;
            }

            var sprite = FindToleranceHeadSprite(Mathf.Clamp01(tolerancePercent));
            if (sprite != null && bind.headImage.sprite != sprite)
            {
                bind.headImage.sprite = sprite;
            }
        }

        private Sprite FindToleranceHeadSprite(float tolerancePercent)
        {
            ToleranceHeadSpriteEntry bestEntry = null;
            var bestMinPercent = float.MinValue;

            for (var i = 0; i < toleranceHeadSprites.Count; i++)
            {
                var entry = toleranceHeadSprites[i];
                if (entry == null || entry.sprite == null)
                {
                    continue;
                }

                var minPercent = Mathf.Clamp01(entry.minTolerancePercent);
                if (tolerancePercent < minPercent || minPercent < bestMinPercent)
                {
                    continue;
                }

                bestEntry = entry;
                bestMinPercent = minPercent;
            }

            return bestEntry != null ? bestEntry.sprite : null;
        }

        private IEnumerator PlayGradeImage()
        {
            yield return FadeGradeImage(0f, 1f, gradeFadeInDuration);

            if (gradeVisibleDuration > 0f)
            {
                yield return new WaitForSeconds(gradeVisibleDuration);
            }

            yield return FadeGradeImage(1f, 0f, gradeFadeOutDuration);
            SetGradeImageAlpha(0f);
            if (bind != null && bind.gradeImage != null)
            {
                bind.gradeImage.gameObject.SetActive(false);
            }

            gradeDisplayCoroutine = null;
        }

        private IEnumerator FadeGradeImage(float fromAlpha, float toAlpha, float duration)
        {
            if (duration <= 0f)
            {
                SetGradeImageAlpha(toAlpha);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetGradeImageAlpha(Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetGradeImageAlpha(toAlpha);
        }

        private void HideGradeImage()
        {
            if (gradeDisplayCoroutine != null)
            {
                StopCoroutine(gradeDisplayCoroutine);
                gradeDisplayCoroutine = null;
            }

            if (bind == null || bind.gradeImage == null)
            {
                return;
            }

            SetGradeImageAlpha(0f);
            bind.gradeImage.gameObject.SetActive(false);
        }

        private void SetGradeImageAlpha(float alpha)
        {
            if (bind == null || bind.gradeImage == null)
            {
                return;
            }

            var color = bind.gradeImage.color;
            color.a = Mathf.Clamp01(alpha);
            bind.gradeImage.color = color;
        }

        private void HideSettlement()
        {
            if (bind != null && bind.settlementPanel != null)
            {
                bind.settlementPanel.SetActive(false);
            }

            settlementMode = SettlementMode.None;
        }

        private void ShowSettlement(bool isVictory, string title, string message)
        {
            settlementMode = SettlementMode.FinalSettlement;
            isTimerRunning = false;
            SetSubmitInteractable(false);
            HideGradeImage();
            if (isVictory)
            {
                GameAudioPlayer.PlayVictory(sessionConfig);
            }

            if (bind == null || bind.settlementPanel == null)
            {
                return;
            }

            bind.settlementPanel.SetActive(true);
            SetSettlementTextVisible(true);
            SetSettlementButtonLabels("重新开始", "退出游戏");

            if (bind.settlementTitleText != null)
            {
                bind.settlementTitleText.text = title;
                bind.settlementTitleText.color = isVictory
                    ? new Color(1f, 0.84f, 0.35f, 1f)
                    : new Color(1f, 0.36f, 0.34f, 1f);
            }

            if (bind.settlementMessageText != null)
            {
                bind.settlementMessageText.text = message;
            }
        }

        private void SetSettlementButtonLabels(string restartButtonText, string exitButtonText)
        {
            SetButtonLabel(bind != null ? bind.settlementRestartButton : null, restartButtonText);
            SetButtonLabel(bind != null ? bind.settlementExitButton : null, exitButtonText);
        }

        private void SetSettlementTextVisible(bool visible)
        {
            if (bind == null)
            {
                return;
            }

            if (bind.settlementTitleText != null)
            {
                bind.settlementTitleText.gameObject.SetActive(visible);
            }

            if (bind.settlementMessageText != null)
            {
                bind.settlementMessageText.gameObject.SetActive(visible);
            }
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var tmpText = button.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (tmpText != null)
            {
                tmpText.text = text;
                return;
            }

            var legacyText = button.GetComponentInChildren<Text>(includeInactive: true);
            if (legacyText != null)
            {
                legacyText.text = text;
            }
        }

        private static string BuildFeedbackText(SentenceEvaluationResult result, bool isTimeout)
        {
            if (isTimeout)
            {
                return result.IsGameOver
                    ? $"时间耗尽，评分 {result.Grade}，评分扣分 {result.TolerancePenalty}，容忍度归零。"
                    : $"时间耗尽，评分 {result.Grade}，评分扣分 {result.TolerancePenalty}{BuildRecoveryText(result)}，剩余 {result.RemainingTolerance}。";
            }

            if (!result.IsMatched)
            {
                return result.IsGameOver
                    ? $"未通过，扣除 {result.TolerancePenalty}，容忍度归零。"
                    : $"未通过，扣除 {result.TolerancePenalty}，剩余 {result.RemainingTolerance}。";
            }

            return result.IsGameOver
                ? $"评分 {result.Grade}，评分扣分 {result.TolerancePenalty}{BuildRecoveryText(result)}，容忍度归零。"
                : $"评分 {result.Grade}，评分扣分 {result.TolerancePenalty}{BuildRecoveryText(result)}，剩余 {result.RemainingTolerance}。";
        }

        private static string BuildRoundSummaryText(SentenceEvaluationResult result, bool isTimeout)
        {
            if (isTimeout)
            {
                return $"上一关时间耗尽，评分 {result.Grade}，评分扣分 {result.TolerancePenalty}。";
            }

            return result.IsMatched
                ? $"上一关评分 {result.Grade}，评分扣分 {result.TolerancePenalty}{BuildRecoveryText(result)}。"
                : $"上一关未通过，扣除 {result.TolerancePenalty}。";
        }

        private static string BuildRecoveryText(SentenceEvaluationResult result)
        {
            if (result.ToleranceRecovery <= 0)
            {
                return string.Empty;
            }

            return result.ConsecutiveAGradeCount >= 2
                ? $"，连续 A x{result.ConsecutiveAGradeCount}，回复 {result.ToleranceRecovery}"
                : $"，回复 {result.ToleranceRecovery}";
        }
    }
}
