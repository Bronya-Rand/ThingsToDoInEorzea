using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Windows.Components
{
    /// <summary>
    /// A modal dialog that allows users to search for and select an entity from a list of entries.
    /// </summary>
    /// <param name="loc"></param>
    public sealed class EntitySearchModal(LocalizationService loc)
    {
        private readonly LocalizationService loc = loc;
        private bool isOpen;
        private string searchFilter = string.Empty;
        private string title = string.Empty;
        private List<SearchPickerEntry> allEntries = [];
        private Action<uint>? onSelected;

        public EntitySearchModal() : this(new LocalizationService()) { }

        public void Open(string title, IEnumerable<SearchPickerEntry> entries, Action<uint> onSelected)
        {
            this.title = title;
            this.allEntries = [.. entries];
            this.onSelected = onSelected;
            searchFilter = string.Empty;
            isOpen = true;

            ImGui.OpenPopup($"{title}##EntitySearchModal");
        }
        public void Draw()
        {
            if (!isOpen) return;

            var center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(480, 420) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

            using var modal = ImRaii.PopupModal($"{title}##EntitySearchModal", ref isOpen, ImGuiWindowFlags.NoSavedSettings);
            if (!modal) return;

            // Search Box
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##EntitySearchInput", loc.T("ui.search.modal_input_hint"), ref searchFilter, 100);

            ImGui.Separator();
            ImGui.Spacing();

            // Filter and truncate list to 150 entries
            var filterText = searchFilter.Trim();
            var matches = allEntries.Where(e =>
                filterText.IsNullOrEmpty() ||
                e.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                e.Id.ToString().Contains(filterText))
                .Take(Constants.MaxItemResults)
                .ToList();

            if (matches.Count == 0)
                ImGui.TextDisabled(loc.T("ui.search.no_results"));
            else
            {
                using var child = ImRaii.Child("##EntitySearchChildList", new Vector2(0, -35 * ImGuiHelpers.GlobalScale), true);
                if (child)
                {
                    foreach (var entry in matches)
                    {
                        using var entryIdScope = ImRaii.PushId((int)entry.Id);
                        var label = $"{entry.Name} ({entry.Id})";

                        if (ImGui.Selectable(label))
                        {
                            onSelected?.Invoke(entry.Id);
                            Close();
                            return;
                        }

                        if (!entry.Subtext.IsNullOrEmpty() && ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(entry.Subtext);
                        }
                    }
                }
            }

            ImGui.Spacing();
            if (ImGui.Button(loc.T("ui.btn_cancel"), new Vector2(100, 25) * ImGuiHelpers.GlobalScale))
            {
                Close();
            }
        }
        private void Close()
        {
            isOpen = false;
            ImGui.CloseCurrentPopup();
        }
    }
}
