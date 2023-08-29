using BaseLib;
using Dapper;
using DataAccess.Interfaces;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DataAccess;

public abstract class DataService : IDataService {
    protected readonly IDbConnectionManager connectionManager;
    protected readonly IDatabaseMapper databaseMapper;
    protected readonly ILoggerFactory loggerFactory;

    protected DataService(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;
        SqlMapper.AddTypeHandler(new SqlTimeOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new DapperSqlDateOnlyTypeHandler());
    }

    public virtual ICrud<T> GetCrud<T>() where T : class => new Crud<T>(connectionManager, databaseMapper, loggerFactory);
}

public class SqlTimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly> {
    public override void SetValue(IDbDataParameter parameter, TimeOnly time) {
        parameter.Value = time.ToString();
    }

    public override TimeOnly Parse(object value) {
        return TimeOnly.FromTimeSpan((TimeSpan)value);
    }
}

public class DapperSqlDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly> {
    public override void SetValue(IDbDataParameter parameter, DateOnly date)
        => parameter.Value = date.ToDateTime(new TimeOnly(0, 0));

    public override DateOnly Parse(object value)
        => DateOnly.FromDateTime((DateTime)value);
}