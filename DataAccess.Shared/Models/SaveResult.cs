namespace DataAccess.Shared;

public record SaveResult(int UpdatedCount, int DeletedCount, IdPk[] InsertedIds);