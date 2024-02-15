using System;

namespace DataAccess.Shared;

public record SaveResult(int UpdatedCount, int DeletedCount, IdPk[] InsertedIds, Exception? Exception = null) {
    public bool Success => Exception is null;


    public static SaveResult Failed(string exceptionMessage) => 
        new(0, 0, [], new InvalidOperationException(exceptionMessage));
}