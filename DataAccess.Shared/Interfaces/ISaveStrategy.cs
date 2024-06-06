using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface ISaveStrategy {
    [Post("/Save")]
    Task<SaveResponse> SaveAsync(IEnumerable<IDataChange> dataChanges);
}