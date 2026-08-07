using System;
using System.Linq;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ThingsToDoInEorzea.Models;

namespace ThingsToDoInEorzea.Services
{
    /// <summary>
    /// Service that hooks into the game's event system to listen for specific game events
    /// </summary>
    public unsafe class HookService : IDisposable
    {
        public event Action<GameEvent>? OnGameEvent;
        private Hook<AgentFateReward.Delegates.EnqueueReward>? EnqueueRewardHook { get; init; }

        public HookService()
        {
            EnqueueRewardHook = DalamudServices.Hook.HookFromAddress<AgentFateReward.Delegates.EnqueueReward>(
                AgentFateReward.MemberFunctionPointers.EnqueueReward,
                EnqueueRewardDetour);
            EnqueueRewardHook.Enable();
        }

        /// <summary>
        /// Detour for EnqueueReward for FATE condition
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="reward"></param>
        private void EnqueueRewardDetour(AgentFateReward* agent, AgentFateReward.Reward* reward)
        {
            try
            {
                EnqueueRewardHook!.Original(agent, reward);

                if (!reward->IsSuccess) return;
                if (reward->Type != AgentFateReward.RewardType.FateReward) return;

                var fateId = (ushort)reward->Id;
                var medal = reward->Medal;

                var fate = DalamudServices.FateTable.FirstOrDefault(f => f.FateId == fateId);
                if (fate == null)
                {
                    DalamudServices.Log.Warning($"EnqueueReward fired for unresolvable FateId {fateId}");
                    return;
                }

                OnGameEvent?.Invoke(new GameEvent
                {
                    Type = GameEventType.FateCompleted,
                    FateRowId = fate.GameData.Value.RowId,
                    FateRewardMedal = medal,
                    TerritoryTypeId = fate.TerritoryType.RowId,
                    ClassJobId = DalamudServices.ObjectTable.LocalPlayer?.ClassJob.RowId ?? 0,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                DalamudServices.Log.Error(ex, "Error in EnqueueRewardDetour");
            }
        }

        public void Dispose()
        {
            EnqueueRewardHook?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
