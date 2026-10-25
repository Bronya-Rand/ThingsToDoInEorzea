using Dalamud.Utility;

namespace ThingsToDoInEorzea.Conditions.Primary
{
    /// <summary>
    /// A class representing a condition that more a note than a specific task.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ItemCountCondition"/> or <see cref="DutyClearCondition"/>, this condition does not have any specific logic to determine completion.
    /// It is intended to be used as a note or reminder to do something that is not easily tracked via the game.
    /// </remarks>
    public class CustomTextCondition : PrimaryConditionBase
    {
        public string Text { get; set; } = string.Empty;

        public override bool ShowsProgress => false;

        public override void ResetObjectiveProgress()
        {
            /* No progress to reset */
        }

        public override bool IsValid() => !Text.IsNullOrEmpty();
        public override string DescribeAction() => Text;
    }
}
