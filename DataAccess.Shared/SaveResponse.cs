using System;

namespace DataAccess.Shared;

public record SaveResponse(int UpdatedCount, int DeletedCount, IdPk[] InsertedIds, Exception? Exception = null) : Response(Exception) {
    public new static SaveResponse Failed(string exceptionMessage) => 
        new(0, 0, [], new InvalidOperationException(exceptionMessage));
}