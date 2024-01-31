using BaseLib;

namespace DataAccess.Shared;

public sealed record DataChangeKind : Enumeration {
    public const int INSERT = 1;
    public const int UPDATE = 2;
    public const int DELETE = 3;
    public const int STORED_PROCEDURE = 4;

    public static readonly DataChangeKind Insert = new(INSERT, "Insert");
    public static readonly DataChangeKind Update =  new (UPDATE, "Update");
    public static readonly DataChangeKind Delete = new(DELETE, "Delete");
    public static readonly DataChangeKind StoredProcedure = new(STORED_PROCEDURE, "Stored Proceddure");

    public DataChangeKind() { }

    public DataChangeKind(int value, string name) : base(typeof(DataChangeKind),value, name) { }
}