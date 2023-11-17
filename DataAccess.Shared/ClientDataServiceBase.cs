using Refit;

namespace DataAccess.Shared; 
public abstract class ClientDataServiceBase(string baseAddress) : IDataService {
    public virtual ICrud<T> GetCrud<T>() where T : class => 
        RestService.For<ICrud<T>>($"{baseAddress}/{typeof(T).Name}");
}