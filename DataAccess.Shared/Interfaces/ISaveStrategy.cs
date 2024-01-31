using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface ISaveStrategy {
    [Post("/")]
    Task<SaveResult> SaveAsync(IEnumerable<IDataChange> dataChanges);
}