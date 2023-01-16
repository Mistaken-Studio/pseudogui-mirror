using PluginAPI.Core;

namespace Mistaken.PseudoGUI;

/// <summary>
/// PseudoGUI Extensions.
/// </summary>
public static class PseudoGUIExtensions
{
    /// <summary>
    /// Sets GUI Element.
    /// </summary>
    /// <param name="player">Player.</param>
    /// <param name="key">key.</param>
    /// <param name="type">position.</param>
    /// <param name="content">content.</param>
    /// <param name="duration">duration.</param>
    public static void SetGUI(this Player player, string key, PseudoGUIPosition type, string content, float duration)
        => PseudoGUIHandler.Set(player, key, type, content, duration);

    /// <summary>
    /// Sets GUI Element.
    /// </summary>
    /// <param name="player">Player.</param>
    /// <param name="key">key.</param>
    /// <param name="type">position.</param>
    /// <param name="content">content.</param>
    public static void SetGUI(this Player player, string key, PseudoGUIPosition type, string content)
        => PseudoGUIHandler.Set(player, key, type, content);
}