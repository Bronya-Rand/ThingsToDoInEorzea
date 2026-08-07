using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Utility;

namespace ThingsToDoInEorzea.Conditions.Helpers
{
    internal static class ClauseFormatting
    {
        private const string JapaneseListConjunction = " ・ ";
        private const string GermanListEndConjunction = " und ";
        private const string EnglishListEndConjunction = " and ";
        private const string FrenchListEndConjunction = " et ";

        /// <summary>
        /// Defines the order in which clause sections should be displayed.
        /// </summary>
        public static readonly IReadOnlyList<ClauseSectionKind> SectionOrder =
            [ClauseSectionKind.DutySettings, ClauseSectionKind.Performance, ClauseSectionKind.Requirements];

        /// <summary>
        /// Returns the appropriate field divider string based on the specified language.
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public static string GetFieldDivider(ClientLanguage language) => language switch
        {
            ClientLanguage.Japanese => " ・ ",
            _ => " | ",
        };

        /// <summary>
        /// Formats a list of segments into a single string, using the appropriate formatting based on the specified language.
        /// </summary>
        /// <param name="segments">The list of segments to format.</param>
        /// <param name="language">The language to use for the formatting.</param>
        /// <returns>A string with the segments formatted according to the specified language.</returns>
        public static string FormatSection(IEnumerable<(string? Label, string Value)> segments, ClientLanguage language)
        {
            var segmentList = segments.Where(s => !s.Value.IsNullOrEmpty()).ToList();
            if (segmentList.Count == 0) return string.Empty;

            var fieldDivider = GetFieldDivider(language);
            var hasLabels = segmentList.Any(s => !s.Label.IsNullOrEmpty());

            if (!hasLabels)
                return JoinWithConjunction(segmentList.Select(s => s.Value), language);

            return string.Join(fieldDivider, segmentList.Select(s =>
                !s.Label.IsNullOrEmpty() ? $"{s.Label}: {s.Value}" : s.Value));
        }

        /// <summary>
        /// Joins a list of strings with the appropriate conjunction based on the specified language.
        /// </summary>
        /// <param name="values">The list of strings to join.</param>
        /// <param name="language">The language to use for the conjunction.</param>
        /// <returns>A string with the values joined by the appropriate conjunction.</returns>
        public static string JoinWithConjunction(IEnumerable<string> values, ClientLanguage language)
        {
            var list = values.ToList();
            if (list.Count == 0) return string.Empty;
            if (list.Count == 1) return list[0];

            return language switch
            {
                ClientLanguage.Japanese => string.Join(JapaneseListConjunction, list),
                ClientLanguage.German => string.Join(", ", list[..^1]) + GermanListEndConjunction + list[^1],
                ClientLanguage.French => string.Join(", ", list[..^1]) + FrenchListEndConjunction + list[^1],
                _ => list.Count == 2
                    ? string.Join(EnglishListEndConjunction, list)
                    : string.Join(", ", list[..^1]) + EnglishListEndConjunction + list[^1]
            };
        }
    }
}
