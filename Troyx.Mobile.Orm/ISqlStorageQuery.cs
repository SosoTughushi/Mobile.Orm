using System;
namespace Troyx.Mobile.Orm
{
    public interface ISqlStorageQuery<TEntity, TFieldKey>
     where TEntity : class, new()
    {
        int Count();
        int Delete();
        void Insert(TEntity entity);
        SqlStorageQuery<TEntity, TFieldKey> OrderBy(TFieldKey field, bool ascending);
        System.Collections.Generic.IEnumerable<TEntity> SelectMany();
        TEntity SelectSingle();
        TEntity SelectSingleOrDefault();
        SqlStorageQuery<TEntity, TFieldKey> Skip(int count);
        SqlStorageQuery<TEntity, TFieldKey> Take(int count);
        SqlStorageQuery<TEntity, TFieldKey> ThenBy(TFieldKey field, bool ascending);
        int UpdateMany(System.Collections.Generic.IDictionary<TFieldKey, object> values);
        int UpdateSingle(TEntity entity, params TFieldKey[] keys);
        SqlStorageQuery<TEntity, TFieldKey> Where(params QueryFilter<TFieldKey>[] queryFilters);
        SqlStorageQuery<TEntity, TFieldKey> WhereAny(params QueryFilter<TFieldKey>[] queryFilters);
    }
}
