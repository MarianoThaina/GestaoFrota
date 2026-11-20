using Android.App;
using Android.Runtime;

namespace GestaoFrota.App;

// Texto claro (HTTP) só em Debug: o emulador acessa a API local por http://10.0.2.2:5137.
// Em Release o atributo não existe, então o app só aceita HTTPS.
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
