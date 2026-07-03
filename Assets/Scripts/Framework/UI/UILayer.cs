namespace Ciga2026.Framework.UI
{
    /// <summary>
    /// UI 默认层级。
    /// 数值越大，默认显示越靠上；实际遮挡关系仍以 Transform sibling 顺序为准。
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Normal = 100,
        Popup = 200,
        Overlay = 300,
        System = 400
    }
}
