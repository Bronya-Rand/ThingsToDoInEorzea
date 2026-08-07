using System;

namespace ThingsToDoInEorzea.Models
{
    public interface ITrackCondition : IDisposable
    {
        bool IsComplete { get; }
        event Action<ITrackCondition>? OnProgressChanged;

        void Enable();
        void Disable();

        void OnGameEvent(GameEvent evt);
    }
}
