using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Primary
{
    /// <summary>
    /// Represents the base class for primary conditions that track progress towards a specific target.
    /// </summary>
    public abstract class PrimaryConditionBase : IPrimaryCondition
    {
        // ITrackCondition implementation
        public event Action<ITrackCondition>? OnProgressChanged;
        [JsonIgnore] public virtual bool IsComplete => Target > 0 && Current >= Target;
        public virtual void Enable() { }
        public virtual void Disable() { }
        public virtual void OnGameEvent(GameEvent evt) { }
        protected void NotifyProgressChanged() => OnProgressChanged?.Invoke(this);

        // IPrimaryCondition implementation
        private int target = 1;
        public virtual int Target
        {
            get => target;
            set
            {
                if (target != value)
                {
                    target = value;
                    NotifyProgressChanged();
                }
            }
        }
        [JsonIgnore] public virtual bool ShowsProgress => true;
        [JsonIgnore] public virtual int Current => 0;
        [JsonIgnore] public virtual IReadOnlySet<Type> AllowedModifierTypes { get; } = new HashSet<Type>();
        public virtual string DescribeAction(GameDataServices gameData) => string.Empty;
        public virtual EvalSource DescribeActionSource(GameDataServices gameData) =>
            EvalSource.FromMacro(DescribeAction(gameData));
        public virtual bool IsValid() => false;
        public virtual void Dispose() => GC.SuppressFinalize(this);
    }
}
