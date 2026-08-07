using Dalamud.Game;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Plugin.Services;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Helpers
{
    /// <summary>
    /// Evaluates objective clauses and segments macros, localizing in-game macros and addon strings to human-readable text.
    /// </summary>
    public static class ClauseEvaluator
    {
        /// <summary>
        /// Resolves a given <see cref="EvalSource"/> macro or addon to a human-readable string, using <see cref="ISeStringEvaluator"/>.
        /// </summary>
        /// <param name="source">The macro or addon to resolve.</param>
        /// <param name="evaluator">The in-game string evaluator.</param>
        /// <param name="loc">The localization service.</param>
        /// <param name="language">The language to use for localization.</param>
        /// <returns>The resolved string.</returns>
        public static string Resolve(EvalSource source, ISeStringEvaluator evaluator, LocalizationService loc, ClientLanguage language)
        {
            var seStringParams = ToParameters(source.Args);

            if (source.AddonRowId.HasValue)
                return evaluator.EvaluateFromAddon(source.AddonRowId.Value, seStringParams, language).ExtractText();

            var macro = source.LocKey != null
                ? loc.GetMacro(source.LocKey, language)
                : source.Macro;

            if (string.IsNullOrEmpty(macro)) return string.Empty;

            return evaluator.EvaluateMacroString(macro, seStringParams, language).ExtractText();
        }

        /// <summary>
        /// Resolves a given <see cref="ClauseSegment"/> to a human-readable label and value, using <see cref="ISeStringEvaluator"/>.
        /// </summary>
        /// <param name="segment">The segment to resolve.</param>
        /// <param name="evaluator">The in-game string evaluator.</param>
        /// <param name="loc">The localization service.</param>
        /// <param name="language">The language to use for localization.</param>
        /// <returns>A tuple containing the resolved label and value.</returns>
        public static (string? Label, string Value) ResolveSegment(
            ClauseSegment segment,
            ISeStringEvaluator evaluator,
            LocalizationService loc,
            ClientLanguage language)
        {
            var value = Resolve(segment.ValueSource, evaluator, loc, language);
            var label = segment.HasLabel
                ? Resolve(segment.LabelSource!.Value, evaluator, loc, language)
                : null;

            return (label, value);
        }

        /// <summary>
        /// Converts an array of objects to an array of <see cref="SeStringParameter"/> for use in string evaluation.
        /// </summary>
        /// <param name="args">The array of objects to convert.</param>
        /// <returns>An array of <see cref="SeStringParameter"/>.</returns>
        public static SeStringParameter[] ToParameters(object[]? args)
        {
            if (args == null || args.Length == 0)
                return [];

            var result = new SeStringParameter[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                result[i] = arg switch
                {
                    int i32 => i32,
                    uint u32 => u32,
                    long i64 => (int)i64,
                    ulong u64 => (uint)u64,
                    float f => (int)f,
                    double d => (int)d,
                    bool b => b ? 1 : 0,
                    string s => s,
                    SeStringParameter p => p,
                    _ => arg?.ToString() ?? string.Empty
                };
            }
            return result;
        }
    }
}
