using System;
using Dapper;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DataAccess;

public abstract class DataService(IReaderFactory readerFactory, ISaveStrategy saveStrategy, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory)
    : DataServiceBase(saveStrategy,databaseMapper), IDataService {
    static DataService() {
        SqlMapper.AddTypeHandler(new SqlTimeOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new DapperSqlDateOnlyTypeHandler());
    }

    public virtual IQueries<T> GetQueries<T>() where T : class => new Queries<T>(readerFactory.GetReader<T>(), loggerFactory.CreateLogger<Queries<T>>());
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