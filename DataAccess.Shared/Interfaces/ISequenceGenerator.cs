using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface ISequenceGenerator {
    Task<IdPk> GetSequencesAsync<T>(int cnt, IDbConnection? dbConnection = null, IDbTransaction? dbTransaction = null) where T : class;
    Task<IdPk> GetSequencesAsync(ITableInfo tableInfo, int cnt, IDbConnection? dbConnection = null, IDbTransaction? dbTransaction = null);
}