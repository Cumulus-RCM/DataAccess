using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface IDataService : IUnitOfWork {
    IQueries<T> GetQueries<T>(bool useCache = false) where T : class;
    Task<IdPk> GetSequenceValuesAsync<T>(int cnt) where T : class;
}
