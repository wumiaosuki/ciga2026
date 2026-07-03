using System.Collections.Generic;
using Ciga2026.Game;
using Ciga2026.Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// Game 界面交互控制器，负责词语拖拽提交、信息展示和容忍度反馈。
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

        private readonly List<string> submittedWordIds = new();
        private BroadcastGameplaySession session;
        private int currentLevelIndex;
        private float remainingLevelTime;
        private float currentLevelDuration = 5f;
        private int displayedTimerCentiseconds = -1;
        private bool isTimerRunning;
        private bool isResolvingSubmission;

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
                ResolveSubmission(allowEmptyAnswer: true, isTimeout: true);
            }
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
            RefreshStaticTexts();
            RefreshToleranceView();
            PopulateWordLibrary();
            ResetLevelTimer();

            if (bind != null && bind.submitButton != null)
            {
                bind.submitButton.interactable = informationDefinition != null && !session.IsGameOver;
            }
        }

        /// <summary>
        /// 从第一关开始执行关卡流程。
        /// </summary>
        public void StartLevelSequence()
        {
            session = new BroadcastGameplaySession(sessionConfig);
            currentLevelIndex = 0;
            HideSettlement();

            if (levelSequenceConfig != null && levelSequenceConfig.TryGetLevel(currentLevelIndex, out var firstLevel))
            {
                StartRound(firstLevel);
                return;
            }

            StartRound(informationDefinition);
        }

        private void OnSubmitClicked()
        {
            ResolveSubmission(allowEmptyAnswer: false, isTimeout: false);
        }

        private void ResolveSubmission(bool allowEmptyAnswer, bool isTimeout)
        {
            if (informationDefinition == null)
            {
                SetFeedback("未配置本局信息。");
                return;
            }

            submittedWordIds.Clear();
            CollectSubmittedWordIds(submittedWordIds);

            if (!allowEmptyAnswer && submittedWordIds.Count == 0)
            {
                SetFeedback("请先把词语拖到输入框中。");
                return;
            }

            if (isResolvingSubmission)
            {
                return;
            }

            isResolvingSubmission = true;
            isTimerRunning = false;

            var result = session.SubmitAnswer(informationDefinition, submittedWordIds);
            RefreshToleranceView();
            RefreshTimerView();
            SetFeedback(BuildFeedbackText(result, isTimeout));

            if (result.IsGameOver)
            {
                SetSubmitInteractable(false);
                ShowSettlement(
                    isVictory: false,
                    title: "播报失败",
                    message: $"容忍度归零，最终停在第 {currentLevelIndex + 1} 关。");
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

            if (levelSequenceConfig.TryGetLevel(currentLevelIndex, out var nextLevel))
            {
                StartRound(nextLevel);
                SetFeedback($"{BuildRoundSummaryText(result, isTimeout)}进入第 {currentLevelIndex + 1} 关。");
                return;
            }

            ClearWordItems();
            isTimerRunning = false;
            remainingLevelTime = 0f;
            RefreshTimerView();

            if (bind != null && bind.informationText != null)
            {
                bind.informationText.text = "全部信息播报完成。";
            }

            SetFeedback($"全部关卡完成，最终容忍度 {session.CurrentTolerance}/{session.InitialTolerance}。");
            SetSubmitInteractable(false);
            ShowSettlement(
                isVictory: true,
                title: "播报完成",
                message: $"全部信息播报完成，最终容忍度 {session.CurrentTolerance}/{session.InitialTolerance}。");
        }

        private void OnSettlementRestartClicked()
        {
            StartLevelSequence();
        }

        private void OnSettlementExitClicked()
        {
            if (GameStateManager.TryGetInstance(out var gameStateManager))
            {
                gameStateManager.ExitGame();
            }
        }

        private void ResetLevelTimer()
        {
            currentLevelDuration = GetLevelDuration();
            remainingLevelTime = currentLevelDuration;
            displayedTimerCentiseconds = -1;
            isTimerRunning = informationDefinition != null && session != null && !session.IsGameOver;
            RefreshTimerView();
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

        private void PopulateWordLibrary()
        {
            if (bind == null || bind.wordItemPrefab == null || bind.wordLibraryDropZone == null || informationDefinition == null)
            {
                return;
            }

            var availableWordIds = informationDefinition.AvailableWordIds;
            for (var i = 0; i < availableWordIds.Count; i++)
            {
                var wordId = availableWordIds[i];
                var wordText = wordLibrary != null ? wordLibrary.GetDisplayText(wordId) : wordId;
                var item = Instantiate(bind.wordItemPrefab, bind.wordLibraryDropZone.ContentRoot);
                item.gameObject.SetActive(true);
                item.Initialize(wordId, wordText, bind.wordLibraryDropZone);
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

        private void ClearWordItems()
        {
            if (bind == null)
            {
                return;
            }

            ClearChildren(bind.wordLibraryContent);
            ClearChildren(bind.inputContent);
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
                bind.informationText.text = informationDefinition != null ? informationDefinition.InformationText : "未配置本局信息";
            }

            SetFeedback(informationDefinition != null ? "拖动词语组成句子后提交。" : "请先配置 InformationDefinition。");
        }

        private void RefreshToleranceView()
        {
            if (bind == null || session == null)
            {
                return;
            }

            if (bind.toleranceFillImage != null)
            {
                SetProgressImage(bind.toleranceFillImage, (float)session.CurrentTolerance / session.InitialTolerance);
            }

            if (bind.toleranceText != null)
            {
                bind.toleranceText.text = $"{session.CurrentTolerance}/{session.InitialTolerance}";
            }
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

        private void HideSettlement()
        {
            if (bind != null && bind.settlementPanel != null)
            {
                bind.settlementPanel.SetActive(false);
            }
        }

        private void ShowSettlement(bool isVictory, string title, string message)
        {
            isTimerRunning = false;
            SetSubmitInteractable(false);

            if (bind == null || bind.settlementPanel == null)
            {
                return;
            }

            bind.settlementPanel.SetActive(true);

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

        private static string BuildFeedbackText(SentenceEvaluationResult result, bool isTimeout)
        {
            if (isTimeout)
            {
                return result.IsGameOver
                    ? $"时间耗尽，扣除 {result.TolerancePenalty}，容忍度归零。"
                    : $"时间耗尽，扣除 {result.TolerancePenalty}，剩余 {result.RemainingTolerance}。";
            }

            if (!result.IsMatched)
            {
                return result.IsGameOver
                    ? $"未通过，扣除 {result.TolerancePenalty}，容忍度归零。"
                    : $"未通过，扣除 {result.TolerancePenalty}，剩余 {result.RemainingTolerance}。";
            }

            return result.IsGameOver
                ? $"评分 {result.Grade}，扣除 {result.TolerancePenalty}，容忍度归零。"
                : $"评分 {result.Grade}，扣除 {result.TolerancePenalty}，剩余 {result.RemainingTolerance}。";
        }

        private static string BuildRoundSummaryText(SentenceEvaluationResult result, bool isTimeout)
        {
            if (isTimeout)
            {
                return $"上一关时间耗尽，扣除 {result.TolerancePenalty}。";
            }

            return result.IsMatched
                ? $"上一关评分 {result.Grade}，扣除 {result.TolerancePenalty}。"
                : $"上一关未通过，扣除 {result.TolerancePenalty}。";
        }
    }
}
