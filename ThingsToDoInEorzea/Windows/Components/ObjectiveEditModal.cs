using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ThingsToDoInEorzea.Conditions.Modifiers;
using ThingsToDoInEorzea.Conditions.Primary;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Utility;

namespace ThingsToDoInEorzea.Windows.Components;

public sealed class ObjectiveEditModal(Plugin plugin)
{

    private bool shouldOpenModal;
    private bool isOpen;

    #region Editing State

    private TodoObjective? editingObjective;
    private string editingTitle = string.Empty;
    private IPrimaryCondition? editingPrimaryCondition;
    private ResetInterval editingResetInterval;
    private List<IModifierCondition> editingModifiers = [];
    private IPrimaryCondition? oldPrimaryCondition;

    private Type? temporarySelectedModifer;

    #endregion

    #region Cached Search Entries

    private List<SearchPickerEntry>? cachedItemEntries;
    private List<SearchPickerEntry>? cachedDutyEntries;
    private List<SearchPickerEntry>? cachedFateEntries;
    private List<SearchPickerEntry>? cachedTerritoryEntries;

    #endregion

    public Action<TodoObjective>? OnSave { get; set; }

    public void LoadObjective(TodoObjective? objective)
    {
        this.editingObjective = objective;
        isOpen = true;

        if (objective != null)
        {
            editingTitle = objective.Title;
            oldPrimaryCondition = ClonePrimary(objective.Primary);
            editingPrimaryCondition = ClonePrimary(objective.Primary);
            editingResetInterval = objective.Recurrence;
            editingModifiers = [.. objective.Modifiers.Select(CloneModifier)];
        }
        else
        {
            editingTitle = string.Empty;
            oldPrimaryCondition = null;
            editingPrimaryCondition = null;
            editingResetInterval = ResetInterval.None;
            editingModifiers = [];
        }

        cachedItemEntries = null;
        cachedDutyEntries = null;
        cachedFateEntries = null;
        cachedTerritoryEntries = null;
        shouldOpenModal = true;
    }

    private static IPrimaryCondition? ClonePrimary(IPrimaryCondition? primary)
    {
        switch (primary)
        {
            case ItemCountCondition item:
            {
                var draftItem = new ItemCountCondition(item.ItemId, item.Target, item.HighQualityOnly)
                {
                    BaselineCount = item.BaselineCount
                };
                draftItem.Enable();
                return draftItem;
            }
            case DutyClearCondition duty:
                return new DutyClearCondition
                {
                    ContentFinderConditionId = duty.ContentFinderConditionId,
                    Target = duty.Target,
                    CompletedDuties = duty.Current
                };
            case CustomTextCondition custom:
                return new CustomTextCondition
                {
                    Text = custom.Text
                };
            case FateClearCondition fate:
                return new FateClearCondition(fate.MatchMode, fate.Target, fate.FateRowId, fate.TerritoryTypeRowId)
                {
                    PersistentCount = fate.Current
                };
            default:
                return null;
        }
    }

    private static IModifierCondition CloneModifier(IModifierCondition mod) => mod switch
    {
        JobCondition job => new JobCondition { ClassJobId = job.ClassJobId },
        TimerCondition timer => new TimerCondition { MaxDuration = timer.MaxDuration },
        WipeCondition wipe => new WipeCondition { MaxAllowedWipes = wipe.MaxAllowedWipes },
        DutySettingsCondition duty => new DutySettingsCondition
        {
            RequireLevelSync = duty.RequireLevelSync,
            RequireMinItemLevel = duty.RequireMinItemLevel,
            RequireUnrestrictedParty = duty.RequireUnrestrictedParty,
            RequireSilenceEcho = duty.RequireSilenceEcho,
        },
        DeathCondition death => new DeathCondition { MaxDeaths = death.MaxDeaths },
        _ => throw new NotImplementedException($"Cloning not implemented for {mod.GetType().Name}")
    };

    public void Draw()
    {
        if (!isOpen) return;

        var loc = plugin.GameDataServices.Localization;
        var isEditing = editingObjective != null;
        var titleText = isEditing ? loc.T("ui.edit.modal_title_edit") : loc.T("ui.edit.modal_title_add");
        var modalId = $"{titleText}##TObjectiveEditorModal";

        if (shouldOpenModal)
        {
            ImGui.OpenPopup(modalId);
            shouldOpenModal = false;
        }

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSizeConstraints(new Vector2(350, 200), new Vector2(float.MaxValue, float.MaxValue));

        using var modal = ImRaii.PopupModal(modalId, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize);
        if (modal)
            DrawContent();

    }

    private void DrawContent()
    {
        // Objective Title
        DrawTitleField();
        ImTracker.SpacedSeparator();

        DrawPrimarySection();

        // Recurrence Section (only for non-custom text objectives)
        if (editingPrimaryCondition != null && editingPrimaryCondition is not CustomTextCondition)
            DrawReccurenceSection();

        DrawModifierSection();
        ImTracker.SpacedSeparator();

        DrawFooter();
    }

    private void DrawTitleField()
    {
        var loc = plugin.GameDataServices.Localization;
        ImGui.Text(loc.T("ui.edit.label_objective_name"));
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("###ObjectiveTitle", ref editingTitle, 256);
    }

    private void DrawPrimarySection()
    {
        var loc = plugin.GameDataServices.Localization;
        ImGui.Text(loc.T("ui.edit.label_objective_type"));
        ImGui.SetNextItemWidth(-1);

        var previewText = editingPrimaryCondition == null
                              ? loc.T("ui.edit.select_primary_type")
                              : ObjectiveConditionDisplay.GetPrimaryName(editingPrimaryCondition.GetType(), loc);

        using (var combo = ImRaii.Combo($"###PrimaryObjective", previewText))
        {
            if (combo)
            {
                foreach (var primaryType in ObjectiveConditionTypes.PrimaryTypes)
                {
                    var displayName = ObjectiveConditionDisplay.GetPrimaryName(primaryType, loc);
                    if (ImGui.Selectable(displayName, editingPrimaryCondition?.GetType() == primaryType))
                    {
                        if (editingPrimaryCondition?.GetType() != primaryType)
                        {
                            editingPrimaryCondition?.Dispose();
                            editingPrimaryCondition = ObjectiveConditionTypes.Create(primaryType);

                            if (editingPrimaryCondition is CustomTextCondition)
                                editingResetInterval = ResetInterval.None;

                            // Reset modifiers when changing primary condition
                            foreach (var mod in editingModifiers)
                                mod.Dispose();
                            editingModifiers.Clear();
                        }
                    }
                }
            }
        }

        if (editingPrimaryCondition != null)
        {
            ImTracker.SpacedSeparator();

            switch (editingPrimaryCondition)
            {
                case DutyClearCondition dutyClear:
                    DrawDutyClearField(dutyClear);
                    break;
                case ItemCountCondition itemCount:
                    DrawItemCountField(itemCount);
                    break;
                case CustomTextCondition customText:
                    DrawCustomTextField(customText);
                    break;
                case FateClearCondition fateClear:
                    DrawFateClearField(fateClear);
                    break;
            }
        }
    }

    private void DrawReccurenceSection()
    {
        var loc = plugin.GameDataServices.Localization;
        ImGui.Text(loc.T("ui.edit.label_recurrence"));
        ImGui.SetNextItemWidth(-1);
        var currentInterval = editingResetInterval;
        var recurrencePreview = currentInterval.ToFriendlyString();
        using var combo = ImRaii.Combo($"###RecurrenceDropdown", recurrencePreview);
        if (combo)
        {
            foreach (var rec in Enum.GetValues<ResetInterval>())
            {
                var recName = rec.ToFriendlyString();
                if (ImGui.Selectable(recName, currentInterval == rec))
                    editingResetInterval = rec;
            }
        }
    }

    private void DrawItemCountField(ItemCountCondition itemCond)
    {
        var loc = plugin.GameDataServices.Localization;
        var itemId = itemCond.ItemId;
        var target = itemCond.Target;
        var hqOnly = itemCond.HighQualityOnly;

        var stackSize = (int)plugin.GameDataServices.ItemBrowserService.GetItemStackSize(itemId);
        var onFreeTrial = DalamudServices.Condition[ConditionFlag.OnFreeTrial];

        cachedItemEntries ??=
        [
            .. plugin.GameDataServices.ItemBrowserService.GetAll()
                     .Select(i => new SearchPickerEntry(i.RowId, i.Name, i.SingularName))
        ];
        plugin.GameDataServices.ItemBrowserService.TryGet(itemId, out var selectedItem);

        ImGui.Text(loc.T("ui.edit.label_item"));
        if (EntitySearchDropdown.Draw("###ItemDropdown", ref itemId, cachedItemEntries, loc,
                                      placeholder: loc.T("ui.edit.select_item")))
            itemCond.ItemId = itemId;

        if (selectedItem != null)
        {
            ImGui.Text(loc.T("ui.target_count"));
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt("###ItemTargetCount", ref target))
            {
                // Clamp the target count to the max gil limit for free trial players
                if (selectedItem.RowId == 1 && onFreeTrial && target > Constants.MaxGilOnFreeTrial)
                {
                    target = Constants.MaxGilOnFreeTrial;
                }
                else
                {
                    if (target > stackSize)
                        target = stackSize;
                }

                itemCond.Target = Math.Max(1, target);
            }

            var minItemCount = 1;
            if (stackSize == 0)
                minItemCount = 0;
            var targetText = selectedItem.RowId == 1 && onFreeTrial
                                 ? $"{loc.T("ui.edit.label_min")} {minItemCount} - {loc.T("ui.edit.label_max")} {Constants.MaxGilOnFreeTrial} (Free Trial Limit)"
                                 : $"{loc.T("ui.edit.label_min")} {minItemCount} - {loc.T("ui.edit.label_max")} {stackSize}";
            if (selectedItem.RowId != 0)
                ImTracker.CenteredTextDisabled(targetText);

            using (ImRaii.Disabled(!selectedItem.CanBeHq))
            {
                if (ImGui.Checkbox($"{loc.T("ui.edit.hq_only")}###ItemHqOnly", ref hqOnly))
                    itemCond.HighQualityOnly = hqOnly;
            }

            if (!selectedItem.CanBeHq && ImTracker.IsItemHoveredWhenDisabled())
                ImGui.SetTooltip(loc.T("ui.edit.cannot_be_hq"));
        }
    }

    private void DrawDutyClearField(DutyClearCondition dutyCond)
    {
        var loc = plugin.GameDataServices.Localization;
        var dutyId = dutyCond.ContentFinderConditionId;
        var target = dutyCond.Target;
        var textInfo = CultureInfo.CurrentCulture.TextInfo;

        cachedDutyEntries ??=
        [
            .. plugin.GameDataServices.DutyBrowserService.GetAll()
                     .Select(d => new SearchPickerEntry(d.RowId, textInfo.ToTitleCase(d.Name), d.ShortCode))
        ];

        ImGui.Text(loc.T("ui.edit.label_duty"));
        if (EntitySearchDropdown.Draw("###DutyDropdown", ref dutyId, cachedDutyEntries, loc,
                                      placeholder: loc.T("ui.edit.select_duty")))
            dutyCond.ContentFinderConditionId = dutyId;

        ImGui.Text(loc.T("ui.target_count"));
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputInt("###DutyTargetClears", ref target))
            dutyCond.Target = Math.Max(1, target);
    }

    private void DrawCustomTextField(CustomTextCondition customCond)
    {
        var loc = plugin.GameDataServices.Localization;
        var text = customCond.Text;
        ImGui.Text(loc.T("ui.edit.label_custom_text"));
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextMultiline("###CustomText", ref text, 512, new Vector2(-1, 100 * ImGuiHelpers.GlobalScale)))
            customCond.Text = text;
    }

    private void DrawFateClearField(FateClearCondition fateCond)
    {
        var loc = plugin.GameDataServices.Localization;
        var target = fateCond.Target;
        var matchMode = fateCond.MatchMode;

        var matchModePreview = matchMode.GetName(loc);
        ImGui.Text(loc.T("ui.edit.label_match_mode"));
        ImGui.SetNextItemWidth(-1);
        using (var matchModeCombo = ImRaii.Combo("###FATEMatchMode", matchModePreview, ImGuiComboFlags.HeightLargest))
        {
            if (matchModeCombo)
            {
                foreach (var mode in Enum.GetValues<FateMatchMode>())
                {
                    var modeName = mode.GetName(loc);
                    if (ImGui.Selectable(modeName, matchMode == mode))
                        fateCond.MatchMode = mode;
                }
            }
        }

        switch (fateCond.MatchMode)
        {
            case FateMatchMode.SpecificFate:
            {
                var fateId = fateCond.FateRowId ?? 0;
                cachedFateEntries ??=
                [
                    .. plugin.GameDataServices.FateBrowserService.GetAll()
                             .Select(f => new SearchPickerEntry(f.RowId, f.Name))
                ];

                ImGui.Text(loc.T("ui.edit.label_fate"));
                if (EntitySearchDropdown.Draw("###FATEDropdown", ref fateId, cachedFateEntries, loc,
                                              placeholder: loc.T("ui.edit.select_fate")))
                    fateCond.FateRowId = fateId;
                break;
            }

            case FateMatchMode.AnyInRegion:
            {
                var territoryId = fateCond.TerritoryTypeRowId ?? 0;
                cachedTerritoryEntries ??=
                [
                    .. plugin.GameDataServices.TerritoryBrowserService.GetAll()
                             .Select(t => new SearchPickerEntry(t.RowId, t.Name))
                ];

                ImGui.Text(loc.T("ui.edit.label_territory"));
                if (EntitySearchDropdown.Draw("###TerritoryDropdown", ref territoryId, cachedTerritoryEntries, loc,
                                              placeholder: loc.T("ui.edit.select_territory")))
                    fateCond.TerritoryTypeRowId = territoryId;
                break;
            }
        }

        ImGui.Text(loc.T("ui.target_count"));
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputInt("###FATETargetClears", ref target))
            fateCond.Target = Math.Max(1, target);
    }

    private void DrawModifierSection()
    {
        var loc = plugin.GameDataServices.Localization;
        ImTracker.SpacedSeparator();
        ImGui.Text(loc.T("ui.edit.modifier_section"));

        var modifiers = editingPrimaryCondition?.AllowedModifierTypes;
        if (modifiers == null)
        {
            ImGui.TextDisabled(loc.T("ui.edit.no_primary_selected"));
            return;
        }

        var modTypes = ObjectiveConditionTypes.ModifierTypes
                                              .Where(modifiers.Contains)
                                              .Where(t => editingModifiers.All(m => m.GetType() != t)) // Hide already added modifiers
                                              .ToList();

        // Remove WipeCondition for Trials/Raids/Ultimates
        if (editingPrimaryCondition is DutyClearCondition dutyCond)
        {
            var removeWipeMod =
                plugin.GameDataServices.DutyBrowserService.IsTrialRaidUltimate(dutyCond.ContentFinderConditionId);
            if (removeWipeMod) modTypes.Remove(typeof(WipeCondition));
        }

        var previewText = temporarySelectedModifer != null
                              ? ObjectiveConditionDisplay.GetModifierName(temporarySelectedModifer, loc)
                              : loc.T("ui.edit.select_modifier");

        using (ImRaii.Disabled(modTypes.Count == 0))
        {
            float iconButtonWidth;
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                iconButtonWidth = ImGui.CalcTextSize(FontAwesomeIcon.Plus.ToIconString()).X +
                                  (ImGui.GetStyle().FramePadding.X * 2f);
            }

            var spacing = ImGui.GetStyle().ItemSpacing.X;
            ImGui.SetNextItemWidth(Math.Max(50f, ImGui.GetContentRegionAvail().X - iconButtonWidth - spacing));

            using (var combo = ImRaii.Combo("###ModifierObjectives", previewText))
            {
                if (combo)
                {
                    foreach (var modType in modTypes)
                    {
                        var displayName = ObjectiveConditionDisplay.GetModifierName(modType, loc);
                        if (ImGui.Selectable(displayName, temporarySelectedModifer == modType))
                        {
                            temporarySelectedModifer = modType;
                        }
                    }
                }
            }

            ImGui.SameLine();
            using (ImRaii.Disabled(temporarySelectedModifer == null))
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
                {
                    var newMod = ObjectiveConditionTypes.CreateModifier(temporarySelectedModifer!);
                    editingModifiers.Add(newMod);
                    temporarySelectedModifer = null;
                }
            }

            ImGui.Spacing();
        }

        if (modTypes.Count == 0 && ImTracker.IsItemHoveredWhenDisabled())
        {
            ImGui.SetTooltip(loc.T("ui.edit.no_modifiers_available"));
        }

        for (var i = editingModifiers.Count - 1; i >= 0; i--)
        {
            var modifier = editingModifiers[i];
            var modifierName = ObjectiveConditionDisplay.GetModifierName(modifier.GetType(), loc);

            using var modifierIdScope = ImRaii.PushId($"Modifier_{i}");
            var headerOpen = ImGui.CollapsingHeader(
                $"{modifierName}###ModifierHeader_{i}",
                ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.AllowItemOverlap);

            DrawModifierHeaderButtons(i, modifier);

            if (headerOpen)
            {
                using var indent = ImRaii.PushIndent();
                ImGui.Spacing();
                DrawModifierFields(modifier);
                ImGui.Spacing();
            }
        }
    }

    private void DrawModifierHeaderButtons(int index, IModifierCondition modifier)
    {
        var loc = plugin.GameDataServices.Localization;
        var buttonSize = 20.0f * ImGuiHelpers.GlobalScale;
        var style = ImGui.GetStyle();

        var totalWidth = buttonSize + style.WindowPadding.X;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - totalWidth + ImGui.GetCursorPosX());

        using (ImRaii.PushId("ModifierHeaderRemove"))
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                modifier.Dispose();
                editingModifiers.RemoveAt(index);
            }
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(loc.T("ui.edit.remove_modifier_tooltip"));
    }

    private void DrawModifierFields(IModifierCondition modifier)
    {
        var loc = plugin.GameDataServices.Localization;
        if (modifier is JobCondition jobMod)
        {
            var jobId = jobMod.ClassJobId;
            IEnumerable<SearchPickerEntry> jobEntries;

            if (editingPrimaryCondition is DutyClearCondition dutyCond && dutyCond.ContentFinderConditionId > 0)
            {
                var dutyClassCategoryId =
                    plugin.GameDataServices.DutyBrowserService.GetAcceptClassJobCategoryId(
                        dutyCond.ContentFinderConditionId);
                var allowedJobIds =
                    plugin.GameDataServices.JobBrowserService.GetAllowedJobIdsForCategory(dutyClassCategoryId);
                var filteredAllowedJobIds =
                    plugin.GameDataServices.JobBrowserService.GetFilteredAllowedJobIds(allowedJobIds);

                jobEntries = plugin.GameDataServices.JobBrowserService.GetAll()
                                   .Where(j => filteredAllowedJobIds.Contains(j.RowId))
                                   .Select(j => new SearchPickerEntry(j.RowId, j.Name));
            }
            else
            {
                var allJobIds = plugin.GameDataServices.JobBrowserService.GetAll().Select(j => j.RowId);
                var filteredAllowedJobIds =
                    plugin.GameDataServices.JobBrowserService.GetFilteredAllowedJobIds(allJobIds);

                jobEntries = plugin.GameDataServices.JobBrowserService.GetAll()
                                   .Where(j => filteredAllowedJobIds.Contains(j.RowId))
                                   .Select(j => new SearchPickerEntry(j.RowId, j.Name));
            }

            ImGui.Text(loc.T("ui.edit.label_job"));
            if (EntitySearchDropdown.Draw("###JobDropdown", ref jobId, jobEntries, loc,
                                          placeholder: loc.T("ui.edit.select_job")))
                jobMod.ClassJobId = jobId;
        }
        else if (modifier is TimerCondition timeMod)
        {
            var minutes = (int)timeMod.MaxDuration.TotalMinutes;

            ImGui.Text(loc.T("ui.edit.label_time_limit"));
            ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("###TimeLimit", ref minutes))
                timeMod.MaxDuration = TimeSpan.FromMinutes(Math.Max(1, minutes));
        }
        else if (modifier is WipeCondition wipeMod)
        {
            var maxWipes = wipeMod.MaxAllowedWipes;
            ImGui.Text(loc.T("ui.edit.label_max_wipes"));
            ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("###MaxWipes", ref maxWipes))
                wipeMod.MaxAllowedWipes = Math.Max(0, maxWipes);
        }
        else if (modifier is DeathCondition deathMod)
        {
            var maxDeaths = deathMod.MaxDeaths;
            ImGui.Text(loc.T("ui.edit.label_max_deaths"));
            ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("###MaxDeaths", ref maxDeaths))
                deathMod.MaxDeaths = Math.Max(0, maxDeaths);
        }
        else if (modifier is DutySettingsCondition dutyMod)
        {
            var requireLevelSync = dutyMod.RequireLevelSync;
            var requireMinItemLevel = dutyMod.RequireMinItemLevel;
            var requireUnrestrictedParty = dutyMod.RequireUnrestrictedParty;
            var requireSilenceEcho = dutyMod.RequireSilenceEcho;

            if (ImGui.Checkbox($"{loc.T("ui.edit.unrestricted_party")}###UnrestrictedParty",
                               ref requireUnrestrictedParty))
            {
                dutyMod.RequireUnrestrictedParty = requireUnrestrictedParty;
                // If Unrestricted Party is unchecked, disable Level Sync
                if (!requireUnrestrictedParty)
                {
                    requireLevelSync = false;
                    dutyMod.RequireLevelSync = false;
                }
            }

            using (ImRaii.Disabled(!requireUnrestrictedParty))
            {
                if (ImGui.Checkbox($"{loc.T("ui.edit.level_sync")}###LevelSync", ref requireLevelSync))
                    dutyMod.RequireLevelSync = requireLevelSync;
            }

            if (ImGui.Checkbox($"{loc.T("ui.edit.min_il")}###MinItemLevel", ref requireMinItemLevel))
                dutyMod.RequireMinItemLevel = requireMinItemLevel;
            if (ImGui.Checkbox($"{loc.T("ui.edit.silence_echo")}###SilenceEcho", ref requireSilenceEcho))
                dutyMod.RequireSilenceEcho = requireSilenceEcho;
        }
        else
            ImGui.TextDisabled($"No configuration available for {modifier.GetType().Name}");
    }

    private void DrawFooter()
    {
        var isTitleValid = !string.IsNullOrWhiteSpace(editingTitle);
        var isPrimarySelected = editingPrimaryCondition != null;
        var isPrimaryValid = isPrimarySelected && editingPrimaryCondition!.IsValid();
        var areModifiersValid = editingModifiers.All(mod => mod.IsValid());

        var canSave = isTitleValid && isPrimarySelected && isPrimaryValid && areModifiersValid;

        var loc = plugin.GameDataServices.Localization;
        var isEditing = editingObjective != null;
        using (ImRaii.Disabled(!canSave))
        {
            if (ImGui.Button(isEditing ? loc.T("ui.edit.btn_save") : loc.T("ui.edit.btn_create")))
            {
                if (editingPrimaryCondition is CustomTextCondition customCond)
                {
                    customCond.Text = customCond.Text.Trim();
                    editingResetInterval = ResetInterval.None;
                }

                if (editingPrimaryCondition != null &&
                    ShouldResetProgressOnEdit(oldPrimaryCondition, editingPrimaryCondition))
                    editingPrimaryCondition.ResetObjectiveProgress();

                var result = editingObjective ?? new TodoObjective();
                if (isEditing) result.Disable();

                var newRecurrence = editingResetInterval;
                if (!isEditing || result.Recurrence != newRecurrence)
                {
                    result.LastResetAt = DateTime.UtcNow;
                }

                result.Title = editingTitle;
                result.ContentId = DalamudServices.PlayerState.ContentId;
                result.Primary = editingPrimaryCondition!;
                result.Recurrence = newRecurrence;
                result.Modifiers = editingModifiers;

                result.InvalidateCachedDescription();
                OnSave?.Invoke(result);
                isOpen = false;
            }
        }

        ImGui.SameLine();
        if (ImGui.Button(loc.T("ui.btn_cancel")))
        {
            isOpen = false;
        }
    }

    private static bool ShouldResetProgressOnEdit(IPrimaryCondition? oldCond, IPrimaryCondition newCond)
    {
        if (oldCond == null || oldCond.GetType() != newCond.GetType())
            return true;

        return (oldCond, newCond) switch
        {
            (ItemCountCondition oldItem, ItemCountCondition newItem) =>
                oldItem.ItemId != newItem.ItemId || oldItem.HighQualityOnly != newItem.HighQualityOnly,

            (DutyClearCondition oldDuty, DutyClearCondition newDuty) =>
                oldDuty.ContentFinderConditionId != newDuty.ContentFinderConditionId,

            (FateClearCondition oldFate, FateClearCondition newFate) =>
                oldFate.MatchMode != newFate.MatchMode ||
                oldFate.FateRowId != newFate.FateRowId ||
                oldFate.TerritoryTypeRowId != newFate.TerritoryTypeRowId,

            _ => false
        };
    }
}
