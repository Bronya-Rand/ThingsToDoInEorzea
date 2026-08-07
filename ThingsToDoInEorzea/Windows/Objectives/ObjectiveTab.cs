using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ThingsToDoInEorzea.Windows.Components;

namespace ThingsToDoInEorzea.Windows.Objectives
{
    internal sealed class ObjectiveTab(Plugin plugin)
    {
        private readonly Plugin plugin = plugin;

        #region Modals
        private readonly ObjectiveTableComponent objectiveTableComponent = new(plugin);

        #endregion
        public void Draw()
        {
            using var child = ImRaii.Child("Objectives", Vector2.Zero);
            var objectives = plugin.Tracker.Objectives.ToList();
            if (!child.Success) return;
            if (objectives == null || objectives.Count == 0)
            {
                return;
            }

            objectiveTableComponent.Draw(objectives, plugin.GameDataServices);
        }
    }
}
