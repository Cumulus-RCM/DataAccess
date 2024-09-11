using System;
using System.Collections.Generic;
using DataAccess.Shared;

namespace DataAccess.Services;

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
        var type = typeof(T);
        if (type.IsGenericType) {  //ICollection<T>
            type = type.GenericTypeArguments[0]; 
            return getTableInfo(type, ()=> {
                var tableInfoType = typeof(TableInfo<>).MakeGenericType(type);
                return (ITableInfo)Activator.CreateInstance(tableInfoType)!;
            });
        }
        return getTableInfo(type, ()=> new TableInfo<T>());
    }

    public ITableInfo GetTableInfo(Type type) {
        if (type.IsGenericType) type = type.GenericTypeArguments[0]; 
        return getTableInfo(type, ()=> (ITableInfo) Activator.CreateInstance(typeof(TableInfo<>).MakeGenericType(type))!);
    }

    private ITableInfo getTableInfo(Type type, Func<ITableInfo> activator) {
        if (tableInfos.TryGetValue(type, out var value)) return value;
        var newTableInfo = activator() ?? throw new InvalidOperationException($"Could not create TableInfo<{type}>");
        tableInfos.Add(type, newTableInfo);
        return newTableInfo;
    }
}