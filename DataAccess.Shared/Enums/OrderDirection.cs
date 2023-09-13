using BaseLib;

namespace DataAccess.Shared; 

public record OrderDirection : Enumeration {
    public static readonly OrderDirection Ascending = new OrderDirection('A', "Asc");
    public static readonly OrderDirection Descending = new OrderDirection('D', "Desc");
    
    public OrderDirection() { }
    public OrderDirection(int value, string name) : base(typeof(OrderDirection),value,name) { }
}