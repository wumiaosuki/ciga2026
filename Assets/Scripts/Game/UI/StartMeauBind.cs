using Ciga2026.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// StartMeau 界面组件绑定。
    /// </summary>
    public sealed class StartMeauBind : UIBind
    {
        [Header("Images")]
        public Image bgImage;

        [Header("Buttons")]
        public Button startButton;
        public Button continueButton;
        public Button savesButton;
        public Button exitButton;
    }
}
