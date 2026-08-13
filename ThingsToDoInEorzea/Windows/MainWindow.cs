using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Windows.Components;

namespace ThingsToDoInEorzea.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly ObjectiveEditModal editModal;
    private readonly ObjectiveDeleteModal deleteModal;
    private Guid? selectedObjectiveId;

    public MainWindow(Plugin plugin)
        : base("Things To Do In Eorzea##MainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(550, 350),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.plugin = plugin;

        editModal = new ObjectiveEditModal(plugin)
        {
            OnSave = (objective) =>
            {
                plugin.Tracker.AddObjective(objective);
            }
        };
        deleteModal = new ObjectiveDeleteModal(plugin);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    public override void Draw()
    {
        // 1. Toolbar / Action Bar
        DrawToolbar();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // 2. Objectives Table View Section
        DrawObjectivesTableSection();

        // 3. Render Modal Dialogs if open
        editModal.Draw();
        deleteModal.Draw();
    }

    private void DrawToolbar()
    {
        var loc = plugin.GameDataServices.Localization;
        using var group = ImRaii.Group();

        if (ImGui.Button(loc.T("ui.main.add_objective")))
        {
            editModal.LoadObjective(null);
        }

        ImGui.SameLine();
        if (ImGui.Button(loc.T("ui.main.settings")))
        {
            plugin.ToggleConfigUi();
        }
    }

    private void DrawObjectivesTableSection()
    {
        var loc = plugin.GameDataServices.Localization;
        var avail = ImGui.GetContentRegionAvail();
        using var child = ImRaii.Child("ObjectivesTableChild", avail);
        if (!child) return;

        var objectives = plugin.Tracker.Objectives;
        if (objectives.Count == 0)
        {
            ImGui.TextDisabled(loc.T("ui.main.no_objectives"));
            return;
        }

        using var table = ImRaii.Table("ObjectivesTable", 3,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn(loc.T("ui.main.col_name"), ImGuiTableColumnFlags.WidthStretch, 200f);
        ImGui.TableSetupColumn(loc.T("ui.col_created"), ImGuiTableColumnFlags.WidthFixed, 140f);
        ImGui.TableSetupColumn(loc.T("ui.main.col_status"), ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableHeadersRow();

        foreach (var objective in objectives)
        {
            using (ImRaii.PushId(objective.Id.ToString()))
            {
                ImGui.TableNextRow();

                var isSelected = selectedObjectiveId == objective.Id;
                var rowHeight = Math.Max(ImGui.GetFrameHeight(), 22f * ImGuiHelpers.GlobalScale);

                // Column 0: Name & Selectable Overlay
                ImGui.TableSetColumnIndex(0);
                var rowStartPos = ImGui.GetCursorScreenPos();

                var rowClicked = ImGui.Selectable($"##Row_{objective.Id}", isSelected,
                    ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.AllowDoubleClick,
                    new Vector2(0, rowHeight));

                ImGui.SetItemAllowOverlap();

                // Mouse interaction handling (Double-click to edit, Right-click for context menu)
                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        editModal.LoadObjective(objective);
                    }
                    else if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                    {
                        selectedObjectiveId = objective.Id;
                        ImGui.OpenPopup($"ObjectiveContextMenu##{objective.Id}");
                    }
                }

                if (rowClicked)
                {
                    selectedObjectiveId = objective.Id;
                }

                // Draw Context Menu
                using (var popup = ImRaii.Popup($"ObjectiveContextMenu##{objective.Id}"))
                {
                    if (popup)
                    {
                        if (ImGui.MenuItem(loc.T("ui.edit_objective")))
                        {
                            editModal.LoadObjective(objective);
                        }

                        if (ImGui.MenuItem(loc.T("ui.main.delete_objective")))
                        {
                            deleteModal.Open(objective);
                        }
                    }
                }

                // Reset cursor screen pos so text renders over selectable row
                ImGui.SetCursorScreenPos(rowStartPos);

                // 1. Name Column
                var objectiveDesc = objective.GetDescription(plugin.GameDataServices, plugin.Configuration);
                var nameText = objective.Recurrence switch
                {
                    ResetInterval.Daily => $"[D] {objective.Title}",
                    ResetInterval.Weekly => $"[W] {objective.Title}",
                    _ => objective.Title,
                };
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(nameText);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(objectiveDesc);

                // 2. Created Date Column
                ImGui.TableSetColumnIndex(1);
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(objective.CreatedAt.ToLocalTime().ToString("g"));

                // 3. Status / Progress Column
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding();
                if (objective.IsComplete)
                {
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.2f, 1.0f), loc.T("ui.main.status_completed"));
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.9f, 0.7f, 0.1f, 1.0f), loc.T("ui.status_in_progress"));
                }
                ImGui.SameLine();
                ImGui.TextDisabled($"({objective.Primary.Current}/{objective.Primary.Target})");
            }
        }
    }
}
