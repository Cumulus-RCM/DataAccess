namespace DataAccess.Shared;

public record SaveResult(int UpdatedCount, int DeletedCount, long[] InsertedIds);