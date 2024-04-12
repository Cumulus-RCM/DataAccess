using System;
using System.Collections.Generic;
using DataAccess.Shared;

namespace DataAccess;

public class DatabaseMapper : IDatabaseMapper {
    private readonly Dictionary<Type, ITableInfo> tableInfos = [];

    public DatabaseMapper(IDatabaseMap? dbMap = null) {
        tableInfos.Clear();
        if (dbMap is null) return;

        foreach (var entry in dbMap.Map) {
            tableInfos.Add(entry.EntityType, entry);
        }
    }

    public ITableInfo GetTableInfo<T>() {
        ITableInfo newTableInfo;

        var type = typeof(T);
        if (type.IsGenericType) {
            type = type.GenericTypeArguments[0];
            if (tableInfos.TryGetValue(type, out var value)) return value;
            var ti = Activator.CreateInstance(typeof(TableInfo<>).MakeGenericType(type)) ?? 
                     throw new InvalidOperationException($"Could not create TableInfo<{type}>");
            newTableInfo = (ITableInfo) ti;
        }
        else {
            if (tableInfos.TryGetValue(type, out var value)) return value;
            newTableInfo = new TableInfo<T>();
        }
        tableInfos.Add(type, newTableInfo);
        return  newTableInfo;
    }

    public ITableInfo GetTableInfo(Type entityType) {
        if (tableInfos.TryGetValue(entityType, out var value)) return value;
        var newTableInfo = Activator.CreateInstance(typeof(TableInfo<>).MakeGenericType(entityType)) ??
                           throw new InvalidOperationException($"Could not create TableInfo<{entityType}>");
        tableInfos.Add(entityType, (ITableInfo)newTableInfo);
        return (ITableInfo)newTableInfo;
    }
}