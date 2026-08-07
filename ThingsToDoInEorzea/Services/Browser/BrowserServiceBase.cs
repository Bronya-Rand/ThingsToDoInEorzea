using System;
using System.Collections.Generic;
using System.Threading;
using Dalamud.Plugin.Services;
using ThingsToDoInEorzea.Models;

namespace ThingsToDoInEorzea.Services.Browser
{
    /// <summary>
    /// Represents a base class for browser services.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class BrowserServiceBase<TKey, TEntity> : IBrowserService<TKey, TEntity> where TKey : notnull
    {
        protected readonly IDataManager dataManager;
        protected readonly Configuration configuration;

        private readonly CancellationTokenSource cts = new();
        protected readonly Dictionary<TKey, TEntity> Entries = [];

        public bool IsLoaded { get; private set; }
        public int Count => Entries.Count;

        protected BrowserServiceBase(IDataManager dataManager, Configuration configuration)
        {
            this.dataManager = dataManager;
            this.configuration = configuration;
            DalamudServices.Framework.RunOnFrameworkThread(() => LoadInternal(cts.Token));
        }
        private void LoadInternal(CancellationToken token)
        {
            try
            {
                LoadData(token);
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;
                DalamudServices.Log.Error($"Error loading {GetType().Name} data: {ex}");
            }
        }

        // Derived classes
        protected abstract void LoadData(CancellationToken token);
        public virtual bool TryGet(TKey id, out TEntity? entity) => Entries.TryGetValue(id, out entity);
        public virtual IEnumerable<TEntity> GetAll() => Entries.Values;
        public virtual void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
