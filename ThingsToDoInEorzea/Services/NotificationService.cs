using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using ThingsToDoInEorzea.Models;

namespace ThingsToDoInEorzea.Services
{
    public class NotificationService(LocalizationService localizationService)
    {
        private readonly LocalizationService loc = localizationService;

        private static Dalamud.Game.Text.SeStringHandling.SeString BuildChatMessage(string message, Vector4 color, bool omitPluginName = false)
        {
            var builder = new Lumina.Text.SeStringBuilder()
                .PushColorRgba(DalamudVector4ToLuminaVector4(color))
                .Append(omitPluginName ? message : $"[{Constants.PluginName}] {message}")
                .PopColor()
                .ToReadOnlySeString();
            return builder.ToDalamudString();
        }

        /// <summary>
        /// Sends a message to the chat log indicating that an objective has been completed.
        /// </summary>
        /// <param name="objectiveName">The name of the completed objective.</param>
        public void SendCompleteMessage(string objectiveName)
        {
            var isLoggedIn = DalamudServices.ClientState.IsLoggedIn;
            if (isLoggedIn)
            {
                var message = BuildChatMessage(loc.T("notify.completed", objectiveName), ImGuiColors.SuccessForeground);
                DalamudServices.ChatGui.Print(message);
            }
        }

        /// <summary>
        /// Sends a message to the chat log indicating that an objective is invalid, along with the reason for its invalidity.
        /// </summary>
        /// <param name="objectiveName"></param>
        /// <param name="reason"></param>
        public void SendInvalidMessage(string objectiveName, string reason)
        {
            var isLoggedIn = DalamudServices.ClientState.IsLoggedIn;
            if (isLoggedIn)
            {
                var message = BuildChatMessage(loc.T("notify.invalid", objectiveName, reason), ImGuiColors.DalamudOrange);
                DalamudServices.ChatGui.Print(message);
            }
        }

        /// <summary>
        /// Sends a message to the chat log indicating that an objective can no longer be completed, along with the reason why.
        /// </summary>
        /// <param name="objectiveName"></param>
        /// <param name="reason"></param>
        public void SendFailedMessage(string objectiveName, string reason)
        {
            var isLoggedIn = DalamudServices.ClientState.IsLoggedIn;
            if (isLoggedIn)
            {
                var message = loc.T("notify.failed", objectiveName, reason);
                DalamudServices.ChatGui.PrintError(message);
            }
        }

        /// <summary>
        /// Sends a summary message to chat on login reminding the user of completed objectives.
        /// </summary>
        /// <param name="completedCount">Number of completed objectives.</param>
        public void SendLoginSummary(List<TodoObjective> completedObjectives)
        {
            if (completedObjectives.Count <= 0) return;
            var isLoggedIn = DalamudServices.ClientState.IsLoggedIn;
            if (isLoggedIn)
            {
                var completedObjectiveNames = completedObjectives.Select(o => o.Title).ToList();
                var completedObjectivesStr = string.Join("\n", completedObjectiveNames);

                var message = BuildChatMessage(loc.T("notify.login_summary", [completedObjectives.Count, completedObjectivesStr]), ImGuiColors.InfoForeground);
                DalamudServices.ChatGui.Print(message);
            }
        }

        #region Helpers
        /// <summary>
        /// Converts a Dalamud ImGuiColors <see cref="Vector4"/> to a Lumina-compatible RGBA color struct.
        /// </summary>
        /// <param name="dalamudColor">The Dalamud color to convert.</param>
        /// <returns>A Vector4 of the same color in byte-form.</returns>
        private static Vector4 DalamudVector4ToLuminaVector4(Vector4 dalamudColor) =>
            new(
                (byte)(dalamudColor.X * 255),
                (byte)(dalamudColor.Y * 255),
                (byte)(dalamudColor.Z * 255),
                (byte)(dalamudColor.W * 255)
            );
        #endregion
    }
}
