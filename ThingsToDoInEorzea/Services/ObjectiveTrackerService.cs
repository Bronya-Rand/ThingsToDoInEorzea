using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin.Services;
using ThingsToDoInEorzea.Conditions.Modifiers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Utility;

namespace ThingsToDoInEorzea.Services
{
    /// <summary>
    /// Service responsible for tracking and managing objectives for the player.
    /// </summary>
    public sealed class ObjectiveTrackerService : IDisposable
    {
        private readonly GameDataServices gameData;
        private readonly Configuration configuration;

        private readonly EntityFileStore<TodoObjective> objectiveStore;
        private static readonly string ObjectivesDirectory = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "objectives");

        public event Action? OnObjectivesChanged;

        public IReadOnlyCollection<TodoObjective> Objectives => GetObjectives();

        public ObjectiveTrackerService(GameDataServices gameData, Configuration configuration, IReliableFileStorage reliableFileStorage)
        {
            this.gameData = gameData;
            this.configuration = configuration;

            Directory.CreateDirectory(ObjectivesDirectory);

            this.objectiveStore = new EntityFileStore<TodoObjective>(
                reliableFileStorage,
                ObjectivesDirectory,
                o => o.Id,
                "Objectives"
                );

            DalamudServices.ClientState.Login += OnLogin;
            DalamudServices.ClientState.Logout += OnLogout;
            DalamudServices.Framework.Update += OnFrameworkUpdate;

            // Load objectives if already logged in
            if (DalamudServices.ClientState.IsLoggedIn)
                OnLogin();
        }

        /// <summary>
        /// Loads objectives from disk and sends a summary notification for any completed objectives upon player login.
        /// </summary>
        private void OnLogin()
        {
            UnloadObjectives();
            LoadObjectives();

            // Reset objectives that have a reset interval and have passed their reset time
            CheckRecurrentObjectivesForReset();

            var completedObjectives = objectiveStore.Items.Where(o => o.IsComplete).ToList();
            if (completedObjectives.Count > 0)
            {
                gameData.NotificationService.SendLoginSummary(completedObjectives);
                foreach (var objective in completedObjectives)
                    objective.Disable();
            }
        }

        private void OnLogout(int type, int code) => UnloadObjectives();
        private void OnFrameworkUpdate(IFramework framework) => CheckRecurrentObjectivesForReset();

        private void UnloadObjectives()
        {
            foreach (var objective in objectiveStore.Items)
            {
                objective.OnProgressChanged -= HandleObjectiveProgressChanged;
                objective.Disable();
                objective.Dispose();
            }
            objectiveStore.Clear();
            OnObjectivesChanged?.Invoke();
        }

        private void LoadObjectives()
        {
            var currentContentId = DalamudServices.PlayerState.ContentId;
            if (currentContentId == 0) return;

            objectiveStore.LoadAll((objective => objective.ContentId == currentContentId));
            foreach (var objective in objectiveStore.Items)
                if (!objective.IsComplete)
                    EnableObjective(objective);

            UpgradeObjectiveJobs();
            OnObjectivesChanged?.Invoke();
        }

        private List<TodoObjective> GetObjectives() => !DalamudServices.ClientState.IsLoggedIn ? [] : objectiveStore.Items;

        public void AddObjective(TodoObjective objective)
        {
            objective.ContentId = DalamudServices.PlayerState.ContentId;
            objectiveStore.Save(objective);
            if (!objective.IsComplete)
                EnableObjective(objective);
            OnObjectivesChanged?.Invoke();
        }

        public void RemoveObjective(Guid id)
        {
            var objective = objectiveStore.FindById(id);
            if (objective == null) return;

            objective.OnProgressChanged -= HandleObjectiveProgressChanged;
            objective.Disable();
            objective.Dispose();

            objectiveStore.Delete(id);
            OnObjectivesChanged?.Invoke();
        }

        public void EnableObjective(TodoObjective objective)
        {
            objective.OnProgressChanged -= HandleObjectiveProgressChanged; // Ensure we don't double-subscribe
            objective.OnProgressChanged += HandleObjectiveProgressChanged;
            objective.Enable(gameData, configuration);
        }

        private void CheckRecurrentObjectivesForReset()
        {
            var anyReset = false;
            var objectiveStoreCopy = objectiveStore.Items.ToList(); // Snapshot

            foreach (var objective in objectiveStoreCopy)
            {
                if (objective.Recurrence != ResetInterval.None && Timers.NeedsReset(objective.Recurrence, objective.LastResetAt))
                {
                    objective.Primary.ResetObjectiveProgress();
                    objective.LastResetAt = DateTime.UtcNow;
                    if (!objective.IsComplete)
                        EnableObjective(objective);

                    objectiveStore.Save(objective);
                    anyReset = true;
                }
            }

            if (anyReset)
                OnObjectivesChanged?.Invoke();
        }

        /// <summary>
        /// Handles the event when an objective's progress changes.
        /// </summary>
        /// <param name="objective"></param>
        private void HandleObjectiveProgressChanged(TodoObjective objective)
        {
            if (objective.IsComplete)
            {
                gameData.NotificationService.SendCompleteMessage(objective.Title);
                objective.Disable();
            }

            objectiveStore.Save(objective);
            OnObjectivesChanged?.Invoke();
        }

        /// <summary>
        /// Updates job IDs in objectives to their effective job IDs based on the current game data.
        /// Fixes stale data like "Complete X as a Gladiator" when player has unlocked the Paladin job.
        /// </summary>
        private void UpgradeObjectiveJobs()
        {
            var objectiveStoreCopy = objectiveStore.Items.ToList(); // Snapshot 

            foreach (var objective in objectiveStoreCopy)
            {
                var modifier = objective.Modifiers.Find(m => m is JobCondition);
                if (modifier != null)
                {
                    var jobModifier = (JobCondition)modifier;
                    var storedJobId = jobModifier.ClassJobId;
                    var newJobId = gameData.JobBrowserService.GetEffectiveJobId(jobModifier.ClassJobId);
                    if (newJobId != 0 && newJobId != storedJobId)
                    {
                        jobModifier.ClassJobId = newJobId;
                        objectiveStore.Save(objective);
                    }
                }
            }
        }
        public void Dispose()
        {
            DalamudServices.ClientState.Login -= OnLogin;
            DalamudServices.ClientState.Logout -= OnLogout;
            DalamudServices.Framework.Update -= OnFrameworkUpdate;
            UnloadObjectives();
        }
    }
}
