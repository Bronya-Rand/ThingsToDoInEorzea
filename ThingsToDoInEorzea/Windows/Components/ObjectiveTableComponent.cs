using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;
using ThingsToDoInEorzea.Utility;

namespace ThingsToDoInEorzea.Windows.Components
{
    /// <summary>
    /// A component responsible for rendering the table of objectives in the main window.
    /// </summary>
    internal sealed class ObjectiveTableComponent
    {
        private readonly Plugin plugin;
        private readonly ObjectiveEditModal editor;
        private readonly ObjectiveDeleteModal deleteModal;

        public ObjectiveTableComponent(Plugin plugin)
        {
            this.plugin = plugin;
            editor = new ObjectiveEditModal(plugin)
            {
                OnSave = (objective) =>
                {
                    plugin.Tracker.AddObjective(objective);
                }
            };
            deleteModal = new ObjectiveDeleteModal(plugin);
        }
        public void Draw(List<TodoObjective> objectives, GameDataServices gameData)
        {
            var loc = gameData.Localization;
            using var objectiveTable = ImRaii.Table("ObjectiveTable", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY |
                ImGuiTableFlags.ScrollX | ImGuiTableFlags.Resizable);
            if (!objectiveTable) return;

            // Setup columns
            ImGui.TableSetupColumn(loc.T("ui.table.col_status"), ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn(loc.T("ui.table.col_objective"), ImGuiTableColumnFlags.WidthStretch, 200);
            ImGui.TableSetupColumn(loc.T("ui.table.col_created"), ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableHeadersRow();

            for (var i = 0; i < objectives.Count; i++)
            {
                var objective = objectives[i];

                using (ImRaii.PushId(objective.Id.ToString()))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    var rowStart = ImGui.GetCursorScreenPos();
                    var rowHeight = Math.Max(ImGui.GetFrameHeight(), 20f * ImGuiHelpers.GlobalScale);

                    // Mouse interactions
                    if (ImGui.IsItemHovered())
                    {
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            HandleOnEditObjective(objective);
                    }
                    else if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        ImGui.OpenPopup($"ObjectiveContextMenu##{objective.Id}");


                    // Context Menu
                    using (var popup = ImRaii.Popup($"ObjectiveContextMenu##{objective.Id}"))
                    {
                        if (popup)
                            DrawObjectiveContextMenu(objective);
                    }

                    ImGui.SetCursorScreenPos(rowStart);

                    DrawStatusColumn(objective, gameData);
                    DrawNameColumn(objective, gameData);
                    DrawCreatedColumn(objective);
                }
            }

            editor.Draw();
            deleteModal.Draw();
        }
        private static void DrawStatusColumn(TodoObjective objective, GameDataServices gameData)
        {
            var loc = gameData.Localization;
            ImGui.TableSetColumnIndex(0);

            string statusText;
            System.Numerics.Vector4 statusColor;

            if (objective.IsComplete)
            {
                statusText = loc.T("ui.table.status_complete");
                statusColor = Constants.GreenOutlineColor;
            }
            else if (objective.IsFailed)
            {
                statusText = loc.T("ui.table.status_failed");
                statusColor = Constants.RedOutlineColor;
            }
            else
            {
                statusText = loc.T("ui.status_in_progress");
                statusColor = Constants.GoldOutlineColor;
            }

            ImTracker.OutlinedText(statusText, ImGuiColors.DalamudWhite, statusColor, 14f);
        }
        private void DrawNameColumn(TodoObjective objective, GameDataServices gameData)
        {
            ImGui.TableSetColumnIndex(1);

            var displayTitle = objective.Title.IsNullOrEmpty() ? $"Objective {objective.Id}" : objective.Title;
            ImGui.Text(displayTitle);

            var description = objective.GetDescription(gameData, plugin.Configuration);
            if (!description.IsNullOrEmpty())
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(description);
        }
        private static void DrawCreatedColumn(TodoObjective objective)
        {
            ImGui.TableSetColumnIndex(2);
            ImGui.Text(objective.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
        }
        private void DrawObjectiveContextMenu(TodoObjective objective)
        {
            var loc = plugin.GameDataServices.Localization;
            if (ImGui.MenuItem(loc.T("ui.edit_objective")))
                HandleOnEditObjective(objective);
            if (ImGui.MenuItem(loc.T("ui.main.delete_objective")))
                deleteModal.Open(objective);
        }
        private void HandleOnEditObjective(TodoObjective objective)
        {
            editor.LoadObjective(objective);
        }
    }
}
