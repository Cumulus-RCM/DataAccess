namespace DataAccess.Shared;

public interface ISqlBuilder {
    string GetWriteSql(IDataChange dataChange);
    string GetReadSql(Filter? filter = null, int pageSize = 0, int pageNum = 1, OrderBy? orderBy = null );
    string GetCountSql(Filter? filter = null);
    string GetWhereClause(Filter? filter);
}