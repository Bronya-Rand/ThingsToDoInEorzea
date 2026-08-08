using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ThingsToDoInEorzea.Conditions.Modifiers;
using ThingsToDoInEorzea.Conditions.Primary;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Utility;
using ThingsToDoInEorzea.Windows.Components;

namespace ThingsToDoInEorzea.Windows;

/// <summary>
/// Represents the main window for displaying objectives in the plugin.
/// </summary>
public class ObjectiveWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private int currentPage;
    private const int ObjectivesPerPage = 5;

    private bool editMode;
    private readonly ObjectiveDeleteModal deleteModal;

    // Icons to use for each primary condition type.
    private static readonly Dictionary<Type, FontAwesomeIcon> PrimaryIcons = new()
    {
        [typeof(DutyClearCondition)] = FontAwesomeIcon.DoorOpen,
        [typeof(FateClearCondition)] = FontAwesomeIcon.Star,
        [typeof(ItemCountCondition)] = FontAwesomeIcon.Box,
        [typeof(CustomTextCondition)] = FontAwesomeIcon.StickyNote,
    };

    private const FontAwesomeIcon DefaultQuestIcon = FontAwesomeIcon.AddressCard; // Stand-in for quest icon
    private const int MaxWindowWidth = 400;
    private const float TimerHeight = 2f;

    public ObjectiveWindow(Plugin plugin)
        : base("Objective HUD##ObjectiveWindow",
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.AlwaysAutoResize)
    {
        IsOpen = true; // Open/visible by default
        ForceMainWindow = true;

        var maxWindowSize = MaxWindowWidth * AtkUnitBase.GetGlobalUIScale() * ImGuiHelpers.GlobalScale;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(maxWindowSize, 100),
            MaximumSize = new Vector2(maxWindowSize, float.MaxValue)
        };
        this.plugin = plugin;
        deleteModal = new ObjectiveDeleteModal(plugin);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    public override void Draw()
    {
        if (editMode)
            Flags &= ~(ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove);
        else
            Flags |= ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove;

        if (!DalamudServices.ClientState.IsLoggedIn) return;

        // Display background color if in edit mode
        DrawObjectiveList();
        deleteModal.Draw();
    }
    private void DrawObjectiveList()
    {
        var objectives = plugin.Tracker.Objectives;
        if (objectives.Count == 0)
        {
            ImGui.TextDisabled("No objectives to display.");
        }
        else
        {

            var pageCount = (int)Math.Ceiling((float)objectives.Count / (float)ObjectivesPerPage);
            currentPage = Math.Clamp(currentPage, 0, pageCount - 1);
            var pageObjectives = objectives.Skip(currentPage * ObjectivesPerPage).Take(ObjectivesPerPage).ToList();

            var align = plugin.Configuration.ObjectiveWindowTextAlignment;
            var wrapWidth = ImGui.GetContentRegionAvail().X;

            for (var i = 0; i < pageObjectives.Count; i++)
            {
                var objective = pageObjectives[i];
                DrawObjective(objective, wrapWidth, align);
                ImGui.Spacing();
            }
            if (pageCount > 1)
                DrawPageControls(pageCount);
        }
        DrawEditModeFooter();
    }
    private void DrawObjective(TodoObjective objective, float wrapWidth, ObjectiveWindowTextAlign align)
    {
        var isComplete = objective.IsComplete;
        var isFailed = objective.IsFailed;

        var titleOutlineColor = isComplete
            ? Constants.GreenOutlineColor
            : isFailed
                ? Constants.RedOutlineColor
                : Constants.GoldOutlineColor;

        var displayTitle = objective.Title.IsNullOrEmpty() ? "Untitled Objective" : objective.Title;

        var titleRowCursor = ImGui.GetCursorPos();
        var buttonWidth = isComplete ? ImGui.GetFrameHeight() : 0f;
        var availableWidth = isComplete ? wrapWidth - buttonWidth - 4f : wrapWidth;

        var icon = PrimaryIcons.GetValueOrDefault(objective.Primary.GetType(), DefaultQuestIcon);
        var iconString = icon.ToIconString();

        float iconWidth;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            iconWidth = ImGui.CalcTextSize(iconString).X;
        }

        var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
        var font = ImGui.GetFont();
        var scale = ImGuiHelpers.GlobalScale * AtkUnitBase.GetGlobalUIScale();
        var effectiveTitleFontSize = Constants.TitleTextSize * scale;

        var removeButtonOnLeft = isComplete && align == ObjectiveWindowTextAlign.Right;
        var maxTitleWrapWidth = Math.Max(50f, availableWidth - iconWidth - itemSpacing);

        var titleLines = ImTracker.WrapText(displayTitle, font, effectiveTitleFontSize, maxTitleWrapWidth);
        var maxLineWidth = 0f;
        foreach (var line in titleLines)
        {
            var w = ImTracker.MeasureWidth(line, font, effectiveTitleFontSize);
            if (w > maxLineWidth) maxLineWidth = w;
        }

        var totalUnitWidth = maxLineWidth + iconWidth + itemSpacing;
        var baseX = titleRowCursor.X + (removeButtonOnLeft ? buttonWidth + 4f : 0f);

        switch (align)
        {
            case ObjectiveWindowTextAlign.Left:
                {
                    DrawIcon(iconString, new Vector2(baseX, titleRowCursor.Y), Constants.TitleTextSize);
                    ImGui.SetCursorPos(new Vector2(baseX + iconWidth + itemSpacing, titleRowCursor.Y));
                    ImTracker.OutlinedText(
                        displayTitle, ImGuiColors.DalamudWhite, titleOutlineColor,
                        Constants.TitleTextSize, wrapWidth: maxTitleWrapWidth, align: ObjectiveWindowTextAlign.Left);
                    break;
                }

            case ObjectiveWindowTextAlign.Center:
                {
                    var startX = Math.Max(0f, (availableWidth - totalUnitWidth) / 2f);
                    DrawIcon(iconString, new Vector2(baseX + startX, titleRowCursor.Y), Constants.TitleTextSize);
                    ImGui.SetCursorPos(new Vector2(baseX + startX + iconWidth + itemSpacing, titleRowCursor.Y));
                    ImTracker.OutlinedText(
                        displayTitle, ImGuiColors.DalamudWhite, titleOutlineColor,
                        Constants.TitleTextSize, wrapWidth: maxLineWidth, align: ObjectiveWindowTextAlign.Center);
                    break;
                }

            case ObjectiveWindowTextAlign.Right:
                {
                    if (isComplete)
                        using (ImRaii.Disabled(editMode))
                            DrawRemoveButton(objective, titleRowCursor.X, editMode);

                    var iconX = baseX + availableWidth - iconWidth;
                    DrawIcon(iconString, new Vector2(iconX, titleRowCursor.Y), Constants.TitleTextSize);
                    ImGui.SetCursorPos(new Vector2(baseX, titleRowCursor.Y));
                    ImTracker.OutlinedText(
                        displayTitle, ImGuiColors.DalamudWhite, titleOutlineColor,
                        Constants.TitleTextSize, wrapWidth: maxTitleWrapWidth, align: ObjectiveWindowTextAlign.Right);
                    break;
                }
        }

        // Display the remove button on the far right if complete and not on left
        if (isComplete && !removeButtonOnLeft)
        {
            using (ImRaii.Disabled(editMode))
                DrawRemoveButton(objective, titleRowCursor.X + wrapWidth - buttonWidth, editMode);
        }

        var objectiveSpacing = Constants.ObjectiveSpacing * ImGuiHelpers.GlobalScale * AtkUnitBase.GetGlobalUIScale();
        ImTracker.Spacing(new Vector2(0, objectiveSpacing));

        var objectiveDesc = objective.GetDescription(plugin.GameDataServices, plugin.Configuration);
        ImTracker.OutlinedText(
            objectiveDesc, ImGuiColors.DalamudWhite, Constants.BlueOutlineColor,
            Constants.ObjectiveTextSize, wrapWidth: wrapWidth, align: align);

        DrawObjectiveTimerBar(objective);
    }

    private static void DrawIcon(string iconString, Vector2 cursorPos, float targetFontSize)
    {
        var scale = ImGuiHelpers.GlobalScale * AtkUnitBase.GetGlobalUIScale();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            iconSize = ImGui.CalcTextSize(iconString);
        }

        // Vertically center the icon relative to the title text font size
        var yOffset = Math.Max(0f, ((targetFontSize * scale) - iconSize.Y) / 2f) + (1.5f * scale);

        var restoreCursor = ImGui.GetCursorPos();
        ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + yOffset));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextColored(ImGuiColors.DalamudWhite, iconString);
        }
        ImGui.SetCursorPos(restoreCursor);
    }

    private void DrawRemoveButton(TodoObjective objective, float posX, bool disabled)
    {
        var restoreCursor = ImGui.GetCursorPos();
        ImGui.SetCursorPosX(posX);
        if (ImTracker.DrawClickableIcon(
            $"###RemoveObjective_{objective.Id}",
            FontAwesomeIcon.Times,
            "Remove Objective",
            (disabled ? ImGuiColors.DalamudGrey3 : ImGuiColors.DalamudGrey),
            (disabled ? ImGuiColors.DalamudGrey3 : ImGuiColors.DalamudRed)
            ))
        {
            deleteModal.Open(objective);
        }
        ImGui.SetCursorPos(restoreCursor);
    }
    private static void DrawObjectiveTimerBar(TodoObjective objective)
    {
        var timerModifier = objective.Modifiers.OfType<TimerCondition>().FirstOrDefault();
        if (timerModifier == null || !timerModifier.IsRunning)
            return;

        var fraction = (float)timerModifier.Progress!;
        var color = GetTimerColor(fraction, timerModifier.IsExpired);

        using (ImRaii.PushColor(ImGuiCol.PlotHistogram, color))
        {
            ImGui.ProgressBar(fraction, new Vector2(-1, TimerHeight * ImGuiHelpers.GlobalScale), string.Empty);
        }
    }
    private static Vector4 GetTimerColor(float progress, bool isExpired)
    {
        if (isExpired)
            return ImGuiColors.DalamudRed;
        if (progress >= 0.75f)
            return ImGuiColors.DalamudOrange;
        return Constants.GreenOutlineColor;
    }
    private void DrawPageControls(int pageCount)
    {
        ImTracker.SpacedSeparator();

        var pageText = $"Page {currentPage + 1}/{pageCount}";
        var textWidth = ImGui.CalcTextSize(pageText).X;

        float buttonWidth;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            buttonWidth = ImGui.CalcTextSize(FontAwesomeIcon.ChevronLeft.ToIconString()).X + (ImGui.GetStyle().FramePadding.X * 2f);
        }

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var totalWidth = (buttonWidth * 2f) + (spacing * 2f) + textWidth;
        var availWidth = ImGui.GetContentRegionAvail().X;
        var startX = Math.Max(0f, (availWidth - totalWidth) / 2f);

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + startX);

        using (ImRaii.Disabled(currentPage == 0))
        {
            if (ImGuiComponents.IconButton("###PrevPage", FontAwesomeIcon.ChevronLeft))
                currentPage--;
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(pageText);

        ImGui.SameLine();
        using (ImRaii.Disabled(currentPage == pageCount - 1))
        {
            if (ImGuiComponents.IconButton("###NextPage", FontAwesomeIcon.ChevronRight))
                currentPage++;
        }
    }
    private void DrawEditModeFooter()
    {
        if (!DalamudServices.ClientState.IsLoggedIn) return;

        var iconText = FontAwesomeIcon.Edit.ToIconString();
        float iconWidth;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            iconWidth = ImGui.CalcTextSize(iconText).X;
        }

        ImTracker.SpacedSeparator();

        if (editMode)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Edit Mode: Drag to reposition.");
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - iconWidth + ImGui.GetCursorPosX());
        }
        else
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - iconWidth);
        }

        // Display a small, transparent "Edit" button in the bottom right corner to toggle edit mode
        if (ImTracker.DrawClickableIcon(
            "###ToggleEditMode",
            FontAwesomeIcon.Edit,
            editMode ? "Lock HUD Position" : "Unlock HUD Position",
            activeColor: new Vector4(1.0f, 1.0f, 1.0f, 0.35f),
            hoverColor: ImGuiColors.DalamudYellow))
        {
            editMode = !editMode;
        }
    }
}
