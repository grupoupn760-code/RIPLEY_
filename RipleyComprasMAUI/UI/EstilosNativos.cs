namespace RipleyComprasMAUI.UI;

public static class EstilosNativos
{
    public static void Aplicar()
    {
#if WINDOWS
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("TemaClaroRipley", (handler, vista) =>
        {
            try
            {
                handler.PlatformView.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Light;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // En algunos equipos WinUI no permite cambiar el tema del calendario y lanza COMException.
                // Se ignora a propósito: el calendario se queda con el tema de Windows y la app sigue funcionando.
            }
        });
#endif
    }
}
