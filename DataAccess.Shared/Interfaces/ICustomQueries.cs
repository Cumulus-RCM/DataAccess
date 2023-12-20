using System;
using System.Collections.Generic;

namespace DataAccess.Shared; 

public interface ICustomQueries {
    IEnumerable<(string endPoint, Delegate handler, string name)> GetEndPoints();
}