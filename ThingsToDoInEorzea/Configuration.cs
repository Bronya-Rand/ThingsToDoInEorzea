using System;
using Dalamud.Configuration;
using Dalamud.Game;
using ThingsToDoInEorzea.Utility;

namespace ThingsToDoInEorzea;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    /// <summary>
    /// Whether to hide jobs that a player has not unlocked/played yet.
    /// </summary>
    public bool HideUnusedJobs { get; set; } = true;
    public ObjectiveWindowTextAlign ObjectiveWindowTextAlignment { get; set; } = ObjectiveWindowTextAlign.Right;
    public ClientLanguage? LanguageOverride { get; set; } = ClientLanguage.English;

    public void Save()
    {
        DalamudServices.PluginInterface.SavePluginConfig(this);
    }
}
