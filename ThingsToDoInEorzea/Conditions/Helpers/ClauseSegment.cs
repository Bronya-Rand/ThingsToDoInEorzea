namespace ThingsToDoInEorzea.Conditions.Helpers
{
    /// <summary>
    /// Represents a segment of a clause, which may consist of an optional label and a required value.
    /// </summary>
    /// <param name="LabelSource">The source of the label, if it exists.</param>
    /// <param name="ValueSource">The source of the value.</param>
    public readonly record struct ClauseSegment(EvalSource? LabelSource, EvalSource ValueSource)
    {
        public static ClauseSegment ValueOnly(EvalSource value) => new(null, value);
        public static ClauseSegment WithLabel(EvalSource label, EvalSource value) => new(label, value);
        public bool HasLabel => LabelSource.HasValue;
    }

    /// <summary>
    /// Represents the kind of section a clause belongs to, such as duty settings, requirements, or performance.
    /// </summary>
    public enum ClauseSectionKind
    {
        DutySettings,
        Requirements,
        Performance,
    }
}
