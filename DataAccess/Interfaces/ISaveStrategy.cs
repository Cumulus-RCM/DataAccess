using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Interfaces;

public interface ISaveStrategy {
    Task<int> SaveAsync(IEnumerable<IDataChange> dataChanges);
}