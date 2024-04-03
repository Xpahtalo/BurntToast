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