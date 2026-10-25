using System;
using ThingsToDoInEorzea.Conditions.Primary;
using ThingsToDoInEorzea.Models;

namespace ThingsToDoInEorzea.Services
{
    /// <summary>
    /// Dispatches game events to objectives that are tracking non-Dalamud conditions
    /// </summary>
    public sealed class ObjectiveDispatchService : IDisposable
    {
        private readonly HookService hookService;
        private readonly ObjectiveTrackerService trackerService;

        public ObjectiveDispatchService(HookService hookService, ObjectiveTrackerService trackerService)
        {
            this.hookService = hookService;
            this.trackerService = trackerService;
            hookService.OnGameEvent += Dispatch;
        }

        private void Dispatch(GameEvent evt)
        {
            foreach (var objective in trackerService.Objectives)
            {
                if (objective.Primary is FateClearCondition)
                    objective.ReceiveExternalEvent(evt);
            }
        }

        public void Dispose() => hookService.OnGameEvent -= Dispatch;
    }
}
