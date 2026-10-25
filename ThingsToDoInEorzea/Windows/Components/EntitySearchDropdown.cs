using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Windows.Components
{
    public record SearchPickerEntry(uint Id, string Name, string Subtext = "");

    /// <summary>
    /// A dropdown component that allows users to search and select from a list of entries.
    /// </summary>
    public static class EntitySearchDropdown
    {
        private static string SearchFilter = string.Empty;

        public static bool Draw(
            string label,
            ref uint selectedId,
            IEnumerable<SearchPickerEntry> entries,
            LocalizationService? loc = null,
            string? placeholder = null,
            float width = 250f)
        {
            loc ??= new LocalizationService();
            placeholder ??= loc.T("ui.search.placeholder");
            var changed = false;
            var entryList = entries as List<SearchPickerEntry> ?? [.. entries];

            // Find current selected entry
            var currentId = selectedId;
            var current = currentId != 0 ? entryList.FirstOrDefault(e => e.Id == currentId) : null;
            var previewText = current != null
                ? current.Name
                : placeholder;

            // Get longest entry name for width calculation
            var longestEntry = entryList.OrderByDescending(e => e.Name.Length).FirstOrDefault();

            if (longestEntry != null)
            {
                width = Math.Max(width, ImGui.CalcTextSize(longestEntry.Name).X - 50);
            }

            ImGui.SetNextItemWidth(-1);
            using var combo = ImRaii.Combo(label, previewText, ImGuiComboFlags.HeightLargest);
            if (combo)
            {
                if (ImGui.IsWindowAppearing())
                {
                    SearchFilter = string.Empty;
                    ImGui.SetKeyboardFocusHere();
                }

                // Search box
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("###EntitySearchDropdownSearch", loc.T("ui.search.input_hint"), ref SearchFilter, 100);
                ImGui.Separator();

                // Scrollable list of entries
                using var child = ImRaii.Child("EntitySearchDropdownChild", new Vector2(0, width));
                if (child)
                {
                    var filterText = SearchFilter.Trim();
                    var matches = entryList.Where(e =>
                        filterText.IsNullOrEmpty() ||
                        e.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                        e.Id.ToString().Contains(filterText))
                        .Take(Constants.MaxItemResults);

                    foreach (var entry in matches)
                    {
                        var isSelected = entry.Id == currentId;
                        var itemLabel = $"{entry.Name}###{entry.Id}";

                        if (ImGui.Selectable(itemLabel, isSelected))
                        {
                            selectedId = entry.Id;
                            changed = true;
                            ImGui.CloseCurrentPopup();
                        }
                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }
            }
            return changed;
        }
    }
}
