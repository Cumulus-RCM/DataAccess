using Refit;

namespace DataAccess.Shared; 
public abstract class ClientDataServiceBase : IDataService {
    private readonly string baseAddress;

    protected ClientDataServiceBase(string baseAddress) {
        this.baseAddress = baseAddress;
    }

    public virtual ICrud<T> GetCrud<T>() where T : class => 
        RestService.For<ICrud<T>>($"{baseAddress}/{typeof(T).Name}");
}