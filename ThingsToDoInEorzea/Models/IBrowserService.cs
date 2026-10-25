using System;
using System.Collections.Generic;

namespace ThingsToDoInEorzea.Models
{
    public interface IBrowserService : IDisposable
    {
        bool IsLoaded { get; }
        int Count { get; }
    }
    public interface IBrowserService<in TKey, TEntity> : IBrowserService where TKey : notnull
    {
        bool TryGet(TKey id, out TEntity? entity);
        IEnumerable<TEntity> GetAll();
    }
}
