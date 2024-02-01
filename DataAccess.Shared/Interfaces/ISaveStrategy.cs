using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface ISaveStrategy {
    [Post("/Save")]
    Task<SaveResult> SaveAsync(IEnumerable<IDataChange> dataChanges);

    [Post("/SequenceValues")]
    Task<IdPk> GetSequenceValuesAsync(string sequenceName, int cnt);
}