namespace DataAccess.Shared; 

public interface ISpecializedCrud {
    IEnumerable<(string endPoint, Delegate handler, string name)> GetEndPoints();
}