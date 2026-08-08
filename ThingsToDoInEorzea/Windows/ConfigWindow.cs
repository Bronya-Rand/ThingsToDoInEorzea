using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ThingsToDoInEorzea.Utility;

namespace ThingsToDoInEorzea.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("Things To Do In Eorzea Configuration###TrackerConfiguration")
    {
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 150),
            MaximumSize = new Vector2(450, 550)
        };

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() => GC.SuppressFinalize(this);

    public override void Draw()
    {
        var loc = plugin.GameDataServices.Localization;

        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f), loc.T("ui.config.window_settings"));
        ImGui.Separator();
        ImGui.Spacing();

        DrawObjectiveWindowTextAlignmentField();

        ImGui.Spacing();

        var languageOverride = configuration.LanguageOverride;
        var previewText = languageOverride?.ToString() ?? "Game Default";

        ImGui.Text(loc.T("ui.config.language_override"));
        using (var languageOverrideCombo = ImRaii.Combo("###LanguageOverride", previewText))
        {
            if (languageOverrideCombo)
            {
                if (ImGui.Selectable("Game Default", languageOverride == null))
                {
                    configuration.LanguageOverride = null;
                    configuration.Save();
                }

                foreach (var lang in Enum.GetValues<ClientLanguage>())
                {
                    var langName = lang.ToString();
                    if (ImGui.Selectable(langName, languageOverride == lang))
                    {
                        configuration.LanguageOverride = lang;
                        configuration.Save();

                        foreach (var obj in plugin.Tracker.Objectives)
                            obj.InvalidateCachedDescription();
                    }
                }
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(loc.T("ui.config.language_override_tooltip"));

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f), loc.T("ui.config.modifier_settings"));
        ImGui.Separator();
        ImGui.Spacing();

        var hideUnusedJobs = configuration.HideUnusedJobs;
        if (ImGui.Checkbox($"{loc.T("ui.config.hide_unused_jobs")}###HideUnusedJobs", ref hideUnusedJobs))
        {
            configuration.HideUnusedJobs = hideUnusedJobs;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(loc.T("ui.config.hide_unused_jobs_tooltip"));
    }

    private void DrawObjectiveWindowTextAlignmentField()
    {
        var loc = plugin.GameDataServices.Localization;
        var textAlignment = configuration.ObjectiveWindowTextAlignment;
        var textPreview = textAlignment.GetName();

        ImGui.Text(loc.T("ui.config.text_alignment"));
        using (var textAlignmentCombo = ImRaii.Combo("###ObjectiveWindowTextAlignment", textPreview))
        {
            if (textAlignmentCombo)
            {
                foreach (var mode in Enum.GetValues<ObjectiveWindowTextAlign>())
                {
                    var modeName = mode.GetName();
                    if (ImGui.Selectable(modeName, textAlignment == mode))
                    {
                        configuration.ObjectiveWindowTextAlignment = mode;
                        configuration.Save();
                    }
                }
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(loc.T("ui.config.text_alignment_tooltip"));
    }
}
