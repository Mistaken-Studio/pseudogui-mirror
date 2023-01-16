using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MEC;
using PluginAPI.Core;
using UnityEngine;

namespace Mistaken.PseudoGUI;

/// <summary>
/// PseudGUIHandler component.
/// </summary>
// ReSharper disable InconsistentNaming
[PublicAPI]
public sealed class PseudoGUIHandler : MonoBehaviour
{
    /// <summary>
    /// Stops updating GUI.
    /// </summary>
    /// <param name="p">player to ignore.</param>
    public static void Ignore(Player p)
    {
        lock (ToIgnoreLock)
            ToIgnore.Add(p);

        lock (ToUpdateLock)
            ToUpdate.Remove(p);
    }

    /// <summary>
    /// Starts updating GUI.
    /// </summary>
    /// <param name="p">player to stop ignoring.</param>
    public static void StopIgnore(Player p)
    {
        lock (ToIgnoreLock)
            ToIgnore.Remove(p);

        lock (ToUpdateLock)
            ToUpdate.Add(p);
    }

    internal static void Set(Player player, string key, PseudoGUIPosition type, string content, float duration)
    {
        Set(player, key, type, content);
        Timing.CallDelayed(duration, () => Set(player, key, type, null));
    }

    internal static void Set(Player player, string key, PseudoGUIPosition type, string content)
    {
        if (player == null)
        {
            Log.Warning("Tried to set GUI for null player");
            Log.Warning(Environment.StackTrace);
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            if (!CustomInfo.TryGetValue(player, out Dictionary<string, (string, PseudoGUIPosition)> value) || !value.ContainsKey(key))
                return;

            value.Remove(key);
        }
        else
        {
            if (!CustomInfo.ContainsKey(player))
                CustomInfo[player] = new();
            else if (CustomInfo[player].TryGetValue(key, out (string Conetent, PseudoGUIPosition Type) value) && value.Conetent == content)
                return;

            CustomInfo[player][key] = (content, type);
        }

        lock (ToUpdateLock)
            ToUpdate.Add(player);
    }

    private static readonly Dictionary<Player, Dictionary<string, (string Content, PseudoGUIPosition Type)>> CustomInfo = new();
    private static readonly object ToUpdateLock = new();
    private static readonly HashSet<Player> ToUpdate = new();
    private static readonly object ToIgnoreLock = new();
    private static readonly HashSet<Player> ToIgnore = new();

    private static bool IsConnected(Player player) => player.GameObject != null && player.Connection is not null;

    private readonly ConcurrentDictionary<Player, string> constructedStrings = new();
    private int frames;
    private bool active = true;

    private void Start()
    {
        this.active = true;
        _ = Task.Run(UpdateTask);
    }

    private void OnDestroy()
    {
        this.active = false;
        CustomInfo.Clear();

        lock (ToUpdateLock)
            ToUpdate.Clear();

        lock (ToIgnoreLock)
            ToIgnore.Clear();
    }

    private void FixedUpdate()
    {
        if (this.constructedStrings.Count == 0)
            return;

        foreach (var item in this.constructedStrings.Keys.ToArray())
        {
            try
            {
                if (item == null)
                    continue;

                if (!IsConnected(item))
                {
                    this.constructedStrings.TryRemove(item, out _);
                }
                else
                {
                    lock (ToIgnoreLock)
                    {
                        if (ToIgnore.Contains(item))
                            continue;
                    }

                    this.UpdateGui(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }
        }
    }

    private async Task UpdateTask()
    {
        while (this.active)
        {
            try
            {
                await Task.Delay(100);

                this.frames += 1;

                // 10s
                if (this.frames > 99)
                {
                    foreach (var item in Player.GetPlayers<Player>())
                    {
                        try
                        {
                            lock (ToIgnoreLock)
                            {
                                if (ToIgnore.Contains(item))
                                    continue;
                            }

                            this.ConstructString(item);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                            Log.Error(ex.StackTrace);
                        }
                    }

                    lock (ToUpdateLock)
                        ToUpdate.Clear();

                    this.frames = 0;
                    continue;
                }

                Player[] toUpdate;
                lock (ToUpdateLock)
                {
                    toUpdate = ToUpdate.ToArray();
                    ToUpdate.Clear();
                }

                foreach (var item in toUpdate)
                {
                    if (!IsConnected(item))
                        continue;

                    lock (ToIgnoreLock)
                    {
                        if (ToIgnore.Contains(item))
                            continue;
                    }

                    this.ConstructString(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }

    private void ConstructString(Player player)
    {
        if (!CustomInfo.ContainsKey(player))
            CustomInfo[player] = new();

        var topContent = string.Empty;
        var topLines = 0;

        var middleContent = string.Empty;
        var middleLines = 0;

        var bottomContent = string.Empty;
        var bottomLines = 0;

        foreach (var item in CustomInfo[player].Values)
        {
            var lines = item.Content.Split(new[] { "<br>" }, StringSplitOptions.None).Length;
            switch (item.Type)
            {
                case PseudoGUIPosition.TOP: // 18
                    if (topContent.Length > 0)
                    {
                        topContent += "<br>";
                        topLines++;
                    }

                    topContent += item.Content;
                    topLines += lines;
                    break;
                case PseudoGUIPosition.MIDDLE:
                    if (middleContent.Length > 0)
                    {
                        middleContent += "<br>";
                        middleLines++;
                    }

                    middleContent += item.Content;
                    middleLines += lines;
                    break;
                case PseudoGUIPosition.BOTTOM: // 15
                    if (bottomContent.Length > 0)
                    {
                        bottomContent = "<br>" + bottomContent;
                        bottomLines++;
                    }

                    bottomContent = item.Content + bottomContent;
                    bottomLines += lines;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var toWrite = string.Empty;
        toWrite += topContent;
        var linesToAddTop = 18 - topLines - ((middleLines - (middleLines % 2)) / 2);
        for (var i = 0; i < linesToAddTop; i++)
            toWrite += "<br>";
        toWrite += middleContent;
        var linesToAddBottom = 15 - bottomLines - ((middleLines - (middleLines % 2)) / 2);
        for (var i = 0; i < linesToAddBottom; i++)
            toWrite += "<br>";
        toWrite += bottomContent;

        this.constructedStrings[player] = $"<size=75%><color=#FFFF>{toWrite}</color><br><br><br><br><br><br><br><br><br><br></size>";
    }

    private void UpdateGui(Player player)
    {
        if (!this.constructedStrings.TryGetValue(player, out var text))
            return;

        try
        {
            if (IsConnected(player))
                player.ReceiveHint(text, 7200);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }

        this.constructedStrings.TryRemove(player, out _);
    }
}
