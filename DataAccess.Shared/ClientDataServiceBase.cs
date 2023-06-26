using Refit;

namespace DataAccess.Shared; 
public class ClientDataServiceConfig : IClientDataServiceConfig{
    public string BaseAddress { get; set; } = "";
}

public abstract class ClientDataServiceBase : IDataService {
    private readonly string baseAddress;

    protected ClientDataServiceBase(string baseAddress) {
        this.baseAddress = baseAddress;
    }

    public virtual ICrud<T> GetCrud<T>() where T : class {
        var type = typeof(T);
        var url = $"{baseAddress}/{type.Name}";
        return RestService.For<ICrud<T>>(url);
    }
}