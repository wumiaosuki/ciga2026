using Ciga2026.Game.Gameplay;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ciga2026.Game.Editor
{
    /// <summary>
    /// InformationDefinition 的专用 Inspector，避开默认 IMGUI 文本框中文显示间距异常的问题。
    /// </summary>
    [CustomEditor(typeof(InformationDefinition))]
    public sealed class InformationDefinitionEditor : UnityEditor.Editor
    {
        private const float MinimumInformationTextHeight = 88f;

        /// <summary>
        /// 使用 UI Toolkit 绘制 Inspector。
        /// </summary>
        /// <returns>Inspector 根元素。</returns>
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.style.paddingLeft = 2f;
            root.style.paddingRight = 2f;

            DrawScriptField(root);
            AddSectionTitle(root, "信息基础内容");

            root.Add(new PropertyField(serializedObject.FindProperty("id")));
            root.Add(CreateInformationTextField());

            AddSectionTitle(root, "本轮词组选项");
            root.Add(new PropertyField(serializedObject.FindProperty("relatedWordCount")));
            root.Add(new PropertyField(serializedObject.FindProperty("choiceSets")));

            root.Bind(serializedObject);
            return root;
        }

        private void DrawScriptField(VisualElement root)
        {
            var scriptField = new ObjectField("Script")
            {
                objectType = typeof(MonoScript),
                value = MonoScript.FromScriptableObject((InformationDefinition)target)
            };
            scriptField.SetEnabled(false);
            root.Add(scriptField);
        }

        private static void AddSectionTitle(VisualElement root, string title)
        {
            var label = new Label(title);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 8f;
            label.style.marginBottom = 4f;
            root.Add(label);
        }

        private static TextField CreateInformationTextField()
        {
            var textField = new TextField("Information Text")
            {
                bindingPath = "informationText",
                multiline = true
            };

            textField.style.minHeight = MinimumInformationTextHeight;
            textField.style.whiteSpace = WhiteSpace.Normal;
            textField.style.flexGrow = 1f;

            return textField;
        }
    }
}
