using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Utility;

namespace ThingsToDoInEorzea.Windows.Components;

public sealed class ObjectiveDeleteModal(Plugin plugin)
{
    private bool shouldOpenModal;
    private bool isOpen;

    #region Editing State

    private TodoObjective? objectiveToDelete;

    #endregion

    public Action<TodoObjective>? OnDelete { get; set; }

    public void Open(TodoObjective objective)
    {
        this.objectiveToDelete = objective;
        isOpen = true;
        shouldOpenModal = true;
    }

    public void Draw()
    {
        if (!isOpen) return;

        var loc = plugin.GameDataServices.Localization;
        var modalTitle = loc.T("ui.delete.modal_title");
        var modalId = $"{modalTitle}##TObjectiveDeleteModal";

        if (shouldOpenModal)
        {
            ImGui.OpenPopup(modalId);
            shouldOpenModal = false;
        }

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        using var modal = ImRaii.PopupModal(modalId, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize);
        if (modal)
            DrawContent();
    }

    private void DrawContent()
    {
        if (objectiveToDelete == null) return;

        DrawPrompt();
        ImTracker.SpacedSeparator();
        DrawFooter();
    }

    private void DrawPrompt()
    {
        var loc = plugin.GameDataServices.Localization;
        var title = objectiveToDelete!.Title.IsNullOrEmpty() ? "Untitled Objective" : objectiveToDelete.Title;
        ImTracker.CenteredText(loc.T("ui.delete.prompt", title));
        ImTracker.CenteredTextDisabled(loc.T("ui.delete.warning"));
    }

    private void DrawFooter()
    {
        var loc = plugin.GameDataServices.Localization;
        var buttonWidth = 120f;
        var spacing = 10f;
        var totalWidth = (buttonWidth * 2) + spacing;
        var windowWidth = ImGui.GetContentRegionAvail().X;
        var padding = (windowWidth - totalWidth) / 2;

        if (padding > 0)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding);

        if (ImGui.Button(loc.T("ui.delete.btn_delete"), new Vector2(buttonWidth, 0)))
        {
            if (objectiveToDelete != null)
            {
                OnDelete?.Invoke(objectiveToDelete);
                plugin.Tracker.RemoveObjective(objectiveToDelete.Id);
                objectiveToDelete = null;
            }

            isOpen = false;
        }

        ImGui.SameLine(0, spacing);

        if (ImGui.Button(loc.T("ui.btn_cancel"), new Vector2(buttonWidth, 0)))
        {
            objectiveToDelete = null;
            isOpen = false;
        }
    }
}
