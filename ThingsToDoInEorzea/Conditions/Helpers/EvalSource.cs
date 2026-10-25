using System.Collections;
using System.Collections.Generic;

namespace ThingsToDoInEorzea.Conditions.Helpers
{
    /// <summary>
    /// Represents the source of an evaluation, which can be an addon row ID, a macro, or a localization key, along with optional arguments.
    /// </summary>
    public readonly record struct EvalSource
    {
        public uint? AddonRowId { get; init; }
        public string? Macro { get; init; }
        public string? LocKey { get; init; }
        public object[] Args { get; init; }

        public static EvalSource FromAddon(uint rowId, params object[] args) =>
            new() { AddonRowId = rowId, Args = NormalizeArgs(args) };

        public static EvalSource FromMacro(string macro, params object[] args) =>
            new() { Macro = macro, Args = NormalizeArgs(args) };

        public static EvalSource FromLocKey(string key, params object[] args) =>
            new() { LocKey = key, Args = NormalizeArgs(args) };

        private static object[] NormalizeArgs(object[] args)
        {
            switch (args.Length)
            {
                case 0:
                    return [];
                case 1 when args[0] is IEnumerable enumerable && args[0] is not string:
                {
                    var list = new List<object>();
                    foreach (var item in enumerable) list.Add(item);
                    return [.. list];
                }
                default:
                    return args;
            }
        }
    }
}
