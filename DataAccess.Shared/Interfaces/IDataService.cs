using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface IDataService : IUnitOfWork {
    IQueries<T> GetQueries<T>() where T : class;
    Task<IdPk> GetSequencesAsync<T>(int cnt) where T : class;
}
