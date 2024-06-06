using System;
using Dapper;
using DataAccess.Shared;
using System.Data;

namespace DataAccess;

public abstract record DataServiceBase(IReaderFactory ReaderFactory, ISaveStrategy SaveStrategy, IDatabaseMapper DatabaseMapper)
    : DataServiceUnitOfWork(SaveStrategy,DatabaseMapper), IDataService {
    static DataServiceBase() {
        SqlMapper.AddTypeHandler(new SqlTimeOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new DapperSqlDateOnlyTypeHandler());
    }

    public virtual IQueries<T> GetQueries<T>() where T : class => new Queries<T>(ReaderFactory.GetReader<T>());
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