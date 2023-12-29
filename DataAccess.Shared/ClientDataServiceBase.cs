using Refit;

namespace DataAccess.Shared; 
public abstract class ClientDataServiceBase(string baseAddress) : IDataService {
    
    public virtual IQueries<T> GetQueries<T>() where T : class => 
        RestService.For<IQueries<T>>($"{baseAddress}/{typeof(T).Name}");

    public ICommands<T> GetCommands<T>() where T : class =>
        RestService.For<ICommands<T>>($"{baseAddress}/{typeof(T).Name}");
}