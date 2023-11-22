using System;
using System.Collections.Generic;

namespace DataAccess.Shared; 

public interface ICustomCrud {
    IEnumerable<(string endPoint, Delegate handler, string name)> GetEndPoints();
}