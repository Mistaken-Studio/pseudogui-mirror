using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;

namespace Mistaken.PseudoGUI;

internal sealed class Plugin
{
    public static Plugin Instance { get; private set; }

    [PluginConfig]
    public Config Config = null!;

    [PluginPriority(LoadPriority.High)]
    [PluginEntryPoint("PseudoGUI", "1.0.0", "Custom Hint based GUI implementation", "Mistaken Devs")]
    public void Load()
    {
        Instance = this;
        EventManager.RegisterEvents(this);
    }

    [PluginUnload]
    public void Unload()
    {
        EventManager.UnregisterEvents(this);
    }

    [PluginEvent(ServerEventType.WaitingForPlayers)]
    private void OnWaitingForPlayers()
        => Server.Instance.GameObject.AddComponent<PseudoGUIHandler>();
}