using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Configuration;

namespace BurntToast;

[Serializable]
public class Configuration : IPluginConfiguration {
    private BurntToast Plugin { get; set; } = null!;

    public List<Regex> Patterns { get; set; } = new();

    public List<BattleTalkPattern> BattleTalkPatterns { get; set; } = new();

    public int Version { get; set; } = 1;

    internal void AddToastPattern(string message) {
        AddToastPattern(new Regex(message, RegexOptions.Compiled));
    }

    internal void AddToastPattern(Regex regex) {
        Patterns.Add(regex);
        Save();
    }

    internal void AddBattleTalkPattern(string message, bool showMessage) {
        AddBattleTalkPattern(new Regex(message, RegexOptions.Compiled), showMessage);
    }

    internal void AddBattleTalkPattern(Regex regex, bool showMessage) {
        BattleTalkPatterns.Add(new BattleTalkPattern(regex, showMessage));
        Save();
    }

    internal void Initialise(BurntToast plugin) {
        Plugin = plugin;
    }

    internal void Save() {
        Plugin.Interface.SavePluginConfig(this);
    }
}

[Serializable]
public class BattleTalkPattern {
    public BattleTalkPattern(Regex pattern, bool showMessage) {
        Pattern     = pattern;
        ShowMessage = showMessage;
    }

    public Regex Pattern     { get; set; }
    public bool  ShowMessage { get; set; }
}