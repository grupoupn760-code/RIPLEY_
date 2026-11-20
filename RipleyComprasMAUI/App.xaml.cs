using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using RipleyComprasMAUI.UI;

namespace RipleyComprasMAUI;

public partial class App : Application
{
    private readonly IServiceProvider servicios;

    public App(IServiceProvider servicios)
    {
        InitializeComponent();          // primero se cargan los recursos (colores y estilos)
        UserAppTheme = AppTheme.Light;
        this.servicios = servicios;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // MainPage se pide al contenedor DESPUÉS de cargar los recursos de App: sus StaticResource ya existen.
        // Es una única instancia (AddSingleton), con los mismos servicios durante toda la sesión.
        MainPage paginaPrincipal = servicios.GetRequiredService<MainPage>();
        return new Window(paginaPrincipal)
        {
            Title = "Ripley - Caja",
            Width = 1320,
            Height = 880,
            MinimumWidth = 1000,
            MinimumHeight = 680
        };
    }
}
