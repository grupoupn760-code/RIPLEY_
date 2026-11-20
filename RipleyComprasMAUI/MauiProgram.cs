using System.Globalization;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using RipleyComprasMAUI.Data;
using RipleyComprasMAUI.Services;
using RipleyComprasMAUI.UI;

namespace RipleyComprasMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        string carpetaDatos = Path.Combine(FileSystem.AppDataDirectory, "data");
        builder.Services.AddSingleton(new JsonDataManager(carpetaDatos, () => FileSystem.OpenAppPackageFileAsync("productos.json")));

        builder.Services.AddSingleton<CatalogoService>();
        builder.Services.AddSingleton<CarritoService>();
        builder.Services.AddSingleton<BoletaService>();
        builder.Services.AddSingleton<CompraService>();
        builder.Services.AddSingleton<MainPage>();


        EstilosNativos.Aplicar();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
