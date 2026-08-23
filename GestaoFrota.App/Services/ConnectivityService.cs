namespace GestaoFrota.App.Services;

public interface IConnectivityService
{
    bool EstaOnline();
}

public class ConnectivityService : IConnectivityService
{
    public bool EstaOnline() => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
