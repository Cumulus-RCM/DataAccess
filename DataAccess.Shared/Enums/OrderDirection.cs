using BaseLib;
using System.Text.Json.Serialization;

namespace DataAccess.Shared; 

[JsonConverter(typeof(EnumerationJsonConverter<OrderDirection>))]
public record OrderDirection : Enumeration {
    public static readonly OrderDirection Ascending = new ('A', "Asc");
    public static readonly OrderDirection Descending = new ('D', "Desc");
    
    public OrderDirection() { }
    public OrderDirection(int value, string name) : base(typeof(OrderDirection),value,name) { }
}