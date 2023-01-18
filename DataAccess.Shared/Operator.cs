namespace DataAccess.Shared; 

public sealed class Operator : Enumeration {
    public static readonly Operator Equal = new Operator(1, "=");
    public static readonly Operator NotEqual = new Operator(2, "<>");
    public static readonly Operator LessThan = new Operator(3, "<");
    public static readonly Operator GreatThan = new Operator(4, ">");
    public static readonly Operator LessThanOrEqual = new Operator(5, "<=");
    public static readonly Operator GreaterThanOrEqual = new Operator(6, ">=");
    public static readonly Operator StartsWith = new Operator(7, "like");
    public static readonly Operator Contains = new Operator(8, "like");

    public Operator() { }
    public Operator(int value, string name) : base(value,name) { }
}