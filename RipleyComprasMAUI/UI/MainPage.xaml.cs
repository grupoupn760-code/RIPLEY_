using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using RipleyComprasMAUI.Data;
using RipleyComprasMAUI.Helpers;
using RipleyComprasMAUI.Models;
using RipleyComprasMAUI.Services;
using RipleyComprasMAUI.Structures;

namespace RipleyComprasMAUI.UI;

/// <summary>
/// Pantalla de caja con cinco secciones: Preventa, Venta, Boleta, Bitácora y Red de cajas.
/// Cada evento lee y valida las entradas con InputHelper, llama al servicio que corresponde y
/// refresca la vista. Aquí no hay estructuras de datos, cálculos comerciales ni acceso a archivos.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly JsonDataManager datos;
    private readonly CatalogoService catalogo;
    private readonly CarritoService carritos;
    private readonly CompraService compras;
    private readonly BoletaService boletas;
    private GrafoCajas grafo = GrafoCajas.CrearEjemplo();

    private bool datosCargados;                       // la carga inicial se hace una sola vez
    private bool procesandoPago;                      // bloquea el doble clic en "Confirmar pago"
    private bool actualizandoVista;                   // ignora eventos provocados por el propio código
    private Producto? productoSeleccionado;
    private ItemCarrito? articuloSeleccionado;        // línea elegida en el carrito de Preventa
    private ItemCarrito? articuloEnCajaSeleccionado;  // línea elegida en la compra en atención

    public MainPage(JsonDataManager datos, CatalogoService catalogo, CarritoService carritos,
                    CompraService compras, BoletaService boletas)
    {
        InitializeComponent();
        this.datos = datos;
        this.catalogo = catalogo;
        this.carritos = carritos;
        this.compras = compras;
        this.boletas = boletas;

        txtCarpetaDatos.Text = datos.CarpetaDatos;
        txtCantidadAgregar.Text = "1";
        dpFecha.Date = DateTime.Today;
        pkBitacora.SelectedIndex = 0;
        PrepararGrafo();
        MostrarSeccion(seccionPreventa, btnNavPreventa);
        RefrescarTodo();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (datosCargados)
        {
            return;
        }
        datosCargados = true;
        await CargarDatosAsync();
    }

    /// <summary>Primer inicio: crea los JSON de trabajo si faltan; luego carga catálogo y ventas.</summary>
    private async Task CargarDatosAsync()
    {
        (bool preparado, string mensajePreparacion) = await datos.PrepararArchivosAsync();
        if (!preparado)
        {
            MostrarMensaje(mensajePreparacion, true);
            return;
        }

        bool catalogoCorrecto = catalogo.Cargar(out string mensajeCatalogo);
        bool ventasCorrectas = boletas.Cargar(out string mensajeVentas);

        bool hayProblemas = !catalogoCorrecto || !ventasCorrectas
                            || !catalogo.Advertencias.EstaVacia || !boletas.Advertencias.EstaVacia;
        if (hayProblemas)
        {
            // Solo se detalla la carga cuando algo falló; si todo está bien basta con "Caja lista".
            List<string> lineas = new List<string> { mensajePreparacion, mensajeCatalogo, mensajeVentas };
            foreach (string aviso in catalogo.Advertencias)
            {
                lineas.Add("Advertencia: " + aviso);
            }
            foreach (string aviso in boletas.Advertencias)
            {
                lineas.Add("Advertencia: " + aviso);
            }
            MostrarMensaje(string.Join(Environment.NewLine, lineas), true);
        }
        else
        {
            MostrarMensaje($"Caja lista. {catalogo.Cantidad} productos en el catálogo.");
        }
        OnListarCatalogoClicked(this, EventArgs.Empty);   // el catálogo queda a la vista desde el inicio
        RefrescarTodo();
    }

    // ============================ Navegación y mensajes ============================

    private void OnNavegarClicked(object? sender, EventArgs e)
    {
        if (sender == btnNavVenta)
        {
            MostrarSeccion(seccionVenta, btnNavVenta);
        }
        else if (sender == btnNavBoleta)
        {
            MostrarSeccion(seccionBoleta, btnNavBoleta);
        }
        else if (sender == btnNavBitacora)
        {
            RefrescarBitacora();
            MostrarSeccion(seccionBitacora, btnNavBitacora);
        }
        else if (sender == btnNavRed)
        {
            MostrarSeccion(seccionRed, btnNavRed);
        }
        else
        {
            MostrarSeccion(seccionPreventa, btnNavPreventa);
        }
    }

    private void MostrarSeccion(View seccion, Button botonActivo)
    {
        seccionPreventa.IsVisible = seccion == seccionPreventa;
        seccionVenta.IsVisible = seccion == seccionVenta;
        seccionBoleta.IsVisible = seccion == seccionBoleta;
        seccionBitacora.IsVisible = seccion == seccionBitacora;
        seccionRed.IsVisible = seccion == seccionRed;

        // La sección activa se pinta en blanco con texto morado; las demás quedan sobre el morado oscuro.
        Button[] botones = { btnNavPreventa, btnNavVenta, btnNavBoleta, btnNavBitacora, btnNavRed };
        foreach (Button boton in botones)
        {
            bool activo = boton == botonActivo;
            boton.BackgroundColor = activo ? Colors.White : ColorDe("ColorPrimarioOscuro", Colors.Indigo);
            boton.TextColor = activo ? ColorDe("ColorPrimario", Colors.Purple) : Colors.White;
            boton.FontAttributes = activo ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    private void MostrarMensaje(string texto, bool esError = false)
    {
        lblMensaje.Text = texto;
        lblMensaje.TextColor = esError ? ColorDe("ColorError", Colors.DarkRed) : ColorDe("ColorTexto", Colors.Black);
        brdMensaje.BackgroundColor = esError ? ColorDe("ColorErrorFondo", Colors.MistyRose) : ColorDe("ColorSuperficie", Colors.White);
    }

    private static void MostrarError(Label etiqueta, string error)
    {
        etiqueta.Text = error;
        etiqueta.IsVisible = error.Length > 0;
    }

    private static Color ColorDe(string clave, Color respaldo)
    {
        if (Application.Current != null
            && Application.Current.Resources.TryGetValue(clave, out object? valor)
            && valor is Color color)
        {
            return color;
        }
        return respaldo;
    }

    private void RefrescarTodo()
    {
        RefrescarListaProductos();
        ActualizarProductoSeleccionado();
        RefrescarCarrito();
        RefrescarFila();
        RefrescarCompraEnAtencion();
        RefrescarInfoBoletas();
        RefrescarBitacora();
    }

    // ============================ PREVENTA: búsqueda ============================

    private void OnCodigoCambiado(object? sender, TextChangedEventArgs e)
    {
        bool valido = InputHelper.TryValidarCodigo(e.NewTextValue, out _, out string error);
        MostrarError(lblErrorCodigo, string.IsNullOrWhiteSpace(e.NewTextValue) ? "" : error);
        btnBuscarCodigo.IsEnabled = valido;
    }

    private void OnBuscarCodigoClicked(object? sender, EventArgs e)
    {
        if (!InputHelper.TryValidarCodigo(txtCodigo.Text, out string codigo, out string error))
        {
            MostrarError(lblErrorCodigo, error);
            return;
        }

        if (catalogo.BuscarPorCodigo(codigo, out Producto? producto, out _) && producto != null)
        {
            MostrarResultados(new List<Producto> { producto }, "1 producto encontrado.");
            cvProductos.SelectedItem = producto;
        }
        else
        {
            MostrarResultados(new List<Producto>(), "Prenda no encontrada.");
        }
    }

    private void OnNombreBuscarCambiado(object? sender, TextChangedEventArgs e)
    {
        bool valido = InputHelper.TryValidarTextoBusqueda(e.NewTextValue, out _, out string error);
        MostrarError(lblErrorNombreBuscar, string.IsNullOrWhiteSpace(e.NewTextValue) ? "" : error);
        btnBuscarNombre.IsEnabled = valido;
    }

    private void OnBuscarNombreClicked(object? sender, EventArgs e)
    {
        if (!InputHelper.TryValidarTextoBusqueda(txtNombreBuscar.Text, out _, out string error))
        {
            MostrarError(lblErrorNombreBuscar, error);
            return;
        }
        ListaEnlazada<Producto> encontrados = catalogo.BuscarPorNombre(txtNombreBuscar.Text, out _);
        MostrarResultados(new List<Producto>(encontrados), encontrados.EstaVacia
            ? "Prenda no encontrada."
            : $"{encontrados.Cantidad} producto(s) encontrado(s).");
    }

    private void OnListarCatalogoClicked(object? sender, EventArgs e)
    {
        ListaEnlazada<Producto> todos = catalogo.Listar();
        MostrarResultados(new List<Producto>(todos), $"Catálogo completo: {todos.Cantidad} producto(s).");
    }

    private void MostrarResultados(List<Producto> productos, string mensaje)
    {
        productoSeleccionado = null;
        cvProductos.SelectedItem = null;
        cvProductos.ItemsSource = productos;
        lblResultadoBusqueda.Text = mensaje;
        ActualizarProductoSeleccionado();
    }

    /// <summary>Vuelve a dibujar los resultados mostrados (por ejemplo, para ver el stock después de una venta).</summary>
    private void RefrescarListaProductos()
    {
        if (cvProductos.ItemsSource is not List<Producto> mostrados)
        {
            return;
        }
        Producto? seleccionado = productoSeleccionado;
        cvProductos.ItemsSource = new List<Producto>(mostrados);
        cvProductos.SelectedItem = seleccionado;
        productoSeleccionado = seleccionado;
    }

    private void OnProductoSeleccionado(object? sender, SelectionChangedEventArgs e)
    {
        productoSeleccionado = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as Producto : null;
        ActualizarProductoSeleccionado();
    }

    private void ActualizarProductoSeleccionado()
    {
        if (productoSeleccionado == null)
        {
            lblProductoSeleccionado.Text = "Selecciona un producto de la lista.";
            btnAgregar.IsEnabled = false;
            return;
        }
        int enCarrito = carritos.CantidadEnCarrito(carritos.Actual, productoSeleccionado.Codigo);
        lblProductoSeleccionado.Text =
            $"{productoSeleccionado.Nombre} · {productoSeleccionado.Stock} disponibles" +
            (enCarrito > 0 ? $" · {enCarrito} en el carrito" : "");
        btnAgregar.IsEnabled = InputHelper.TryLeerEntero(txtCantidadAgregar.Text, "Cantidad",
            InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out _, out _);
    }

    private void OnCantidadAgregarCambiada(object? sender, TextChangedEventArgs e)
    {
        bool valida = InputHelper.TryLeerEntero(e.NewTextValue, "Cantidad",
            InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out _, out string error);
        MostrarError(lblErrorCantidadAgregar, valida ? "" : error);
        btnAgregar.IsEnabled = valida && productoSeleccionado != null;
    }

    // ============================ PREVENTA: carrito (CarritoService) ============================

    private void OnAgregarClicked(object? sender, EventArgs e)
    {
        if (productoSeleccionado == null)
        {
            MostrarMensaje("Selecciona un producto antes de agregarlo.", true);
            return;
        }
        if (!InputHelper.TryLeerEntero(txtCantidadAgregar.Text, "Cantidad",
                InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out int cantidad, out string error))
        {
            MostrarError(lblErrorCantidadAgregar, error);
            return;
        }

        bool correcto = carritos.AgregarPrenda(carritos.Actual, productoSeleccionado.Codigo, cantidad, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        RefrescarCarrito();
        ActualizarProductoSeleccionado();
    }

    private void OnArticuloCarritoSeleccionado(object? sender, SelectionChangedEventArgs e)
    {
        if (actualizandoVista)
        {
            return;
        }
        articuloSeleccionado = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as ItemCarrito : null;
        if (articuloSeleccionado != null)
        {
            txtNuevaCantidad.Text = articuloSeleccionado.Cantidad.ToString();
        }
        ActualizarBotonesCarrito();
    }

    private void OnMasClicked(object? sender, EventArgs e)
    {
        CambiarCantidadSeleccionada(1);
    }

    private void OnMenosClicked(object? sender, EventArgs e)
    {
        CambiarCantidadSeleccionada(-1);
    }

    private void CambiarCantidadSeleccionada(int diferencia)
    {
        if (articuloSeleccionado == null)
        {
            MostrarMensaje("Selecciona un producto del carrito.", true);
            return;
        }
        int nueva = articuloSeleccionado.Cantidad + diferencia;
        if (nueva < InputHelper.CantidadMinima)
        {
            MostrarMensaje("La cantidad mínima es 1. Para sacar el producto usa Quitar.", true);
            return;
        }
        bool correcto = carritos.CambiarCantidad(carritos.Actual, articuloSeleccionado.Codigo, nueva, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        RefrescarCarrito();
    }

    private void OnNuevaCantidadCambiada(object? sender, TextChangedEventArgs e)
    {
        bool valida = InputHelper.TryLeerEntero(e.NewTextValue, "Nueva cantidad",
            InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out _, out string error);
        MostrarError(lblErrorNuevaCantidad, valida || string.IsNullOrWhiteSpace(e.NewTextValue) ? "" : error);
        ActualizarBotonesCarrito();
    }

    private void OnCambiarCantidadClicked(object? sender, EventArgs e)
    {
        if (articuloSeleccionado == null)
        {
            MostrarMensaje("Selecciona un producto del carrito.", true);
            return;
        }
        if (!InputHelper.TryLeerEntero(txtNuevaCantidad.Text, "Nueva cantidad",
                InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out int nueva, out string error))
        {
            MostrarError(lblErrorNuevaCantidad, error);
            return;
        }
        bool correcto = carritos.CambiarCantidad(carritos.Actual, articuloSeleccionado.Codigo, nueva, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        RefrescarCarrito();
    }

    private void OnQuitarClicked(object? sender, EventArgs e)
    {
        if (articuloSeleccionado == null)
        {
            MostrarMensaje("Selecciona un producto del carrito.", true);
            return;
        }
        bool correcto = carritos.QuitarPrenda(carritos.Actual, articuloSeleccionado.Codigo, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        articuloSeleccionado = null;
        RefrescarCarrito();
        ActualizarProductoSeleccionado();
    }

    private void OnDeshacerClicked(object? sender, EventArgs e)
    {
        bool correcto = carritos.Deshacer(carritos.Actual, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        RefrescarCarrito();
        ActualizarProductoSeleccionado();
    }

    private void OnVaciarClicked(object? sender, EventArgs e)
    {
        carritos.Vaciar(carritos.Actual, out string mensaje);
        MostrarMensaje(mensaje);
        articuloSeleccionado = null;
        RefrescarCarrito();
        ActualizarProductoSeleccionado();
    }

    private void RefrescarCarrito()
    {
        ContextoCarrito actual = carritos.Actual;
        string? codigoSeleccionado = articuloSeleccionado?.Codigo;
        List<ItemCarrito> lineas = new List<ItemCarrito>(actual.Articulos);   // copia solo para presentar

        actualizandoVista = true;
        articuloSeleccionado = null;
        foreach (ItemCarrito linea in lineas)
        {
            if (linea.Codigo == codigoSeleccionado)
            {
                articuloSeleccionado = linea;            // se conserva la selección tras refrescar
            }
        }
        cvCarrito.ItemsSource = lineas;
        cvCarrito.SelectedItem = articuloSeleccionado;
        actualizandoVista = false;

        if (articuloSeleccionado != null)
        {
            txtNuevaCantidad.Text = articuloSeleccionado.Cantidad.ToString();
        }

        decimal totalRegular = carritos.CalcularTotalRegular(actual);
        decimal estimadoRipley = compras.EstimarTotalTarjetaRipley(actual.Articulos);
        lblTituloCarrito.Text = actual.EstaVacio ? "Sin productos" : $"{actual.CantidadLineas} producto(s)";
        lblTotalRegular.Text = InputHelper.FormatearSoles(totalRegular);
        lblEstimadoRipley.Text = actual.EstaVacio || estimadoRipley >= totalRegular
            ? ""
            : $"Con Tarjeta Ripley: {InputHelper.FormatearSoles(estimadoRipley)} (ahorras {InputHelper.FormatearSoles(totalRegular - estimadoRipley)})";

        ActualizarBotonesCarrito();
        ActualizarBotonEnviar();
    }

    private void ActualizarBotonesCarrito()
    {
        bool haySeleccion = articuloSeleccionado != null;
        btnMas.IsEnabled = haySeleccion;
        btnMenos.IsEnabled = haySeleccion && articuloSeleccionado!.Cantidad > InputHelper.CantidadMinima;
        btnQuitar.IsEnabled = haySeleccion;
        btnCambiarCantidad.IsEnabled = haySeleccion && InputHelper.TryLeerEntero(txtNuevaCantidad.Text, "Nueva cantidad",
            InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out _, out _);
        btnVaciar.IsEnabled = !carritos.Actual.EstaVacio;
        btnDeshacer.IsEnabled = carritos.Actual.AccionesParaDeshacer > 0;
    }

    // ============================ PREVENTA: cliente y envío a caja ============================

    private void OnDatosClienteCambiados(object? sender, TextChangedEventArgs e)
    {
        ActualizarBotonEnviar();
    }

    private void ActualizarBotonEnviar()
    {
        List<string> errores = new List<string>();
        if (!InputHelper.TryValidarNombrePersona(txtClienteNombre.Text, out _, out string errorNombre))
        {
            errores.Add(errorNombre);
        }
        if (!InputHelper.TryValidarDniOpcional(txtClienteDni.Text, out _, out string errorDni))
        {
            errores.Add(errorDni);
        }
        if (!InputHelper.TryValidarCorreo(txtClienteCorreo.Text, out _, out string errorCorreo))
        {
            errores.Add(errorCorreo);
        }
        MostrarError(lblErrorCliente, string.Join(Environment.NewLine, errores));
        btnEnviarCaja.IsEnabled = errores.Count == 0 && !carritos.Actual.EstaVacio;
    }

    private void OnEnviarCajaClicked(object? sender, EventArgs e)
    {
        bool correcto = compras.RegistrarLlegada(txtClienteNombre.Text, txtClienteDni.Text, txtClienteCorreo.Text,
                                                 out _, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        if (!correcto)
        {
            return;
        }

        txtClienteNombre.Text = "";
        txtClienteDni.Text = "";
        txtClienteCorreo.Text = "";
        articuloSeleccionado = null;
        RefrescarCarrito();
        RefrescarFila();
        ActualizarProductoSeleccionado();
    }

    // ============================ VENTA (CompraService) ============================

    private void RefrescarFila()
    {
        List<Venta> enEspera = compras.ObtenerFila();
        cvFila.ItemsSource = enEspera;
        lblInfoFila.Text = enEspera.Count == 0
            ? "Sin clientes en espera"
            : $"{enEspera.Count} en espera";
        btnAtenderSiguiente.Text = enEspera.Count == 0
            ? "Atender siguiente"
            : $"Atender siguiente (turno {enEspera[0].Turno})";
        btnAtenderSiguiente.IsEnabled = enEspera.Count > 0 && compras.CompraEnAtencion == null;
    }

    private void OnAtenderSiguienteClicked(object? sender, EventArgs e)
    {
        bool correcto = compras.AtenderSiguiente(out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        if (correcto)
        {
            articuloEnCajaSeleccionado = null;
            txtCantidadCaja.Text = "";
        }
        RefrescarFila();
        RefrescarCompraEnAtencion();
    }

    private void OnTarjetaCambiada(object? sender, EventArgs e)
    {
        if (actualizandoVista || compras.CompraEnAtencion == null)
        {
            return;
        }
        if (!InputHelper.TryLeerTarjeta(pkTarjeta.SelectedIndex, out TipoTarjeta tarjeta, out string error))
        {
            MostrarMensaje(error, true);
            return;
        }
        bool correcto = compras.SeleccionarTarjeta(tarjeta, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        RefrescarCompraEnAtencion();
    }

    private void OnArticuloCajaSeleccionado(object? sender, SelectionChangedEventArgs e)
    {
        if (actualizandoVista)
        {
            return;
        }
        articuloEnCajaSeleccionado = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as ItemCarrito : null;
        if (articuloEnCajaSeleccionado != null)
        {
            txtCantidadCaja.Text = articuloEnCajaSeleccionado.Cantidad.ToString();
        }
        ActualizarBotonesVenta();
    }

    private void OnCantidadCajaCambiada(object? sender, TextChangedEventArgs e)
    {
        bool valida = InputHelper.TryLeerEntero(e.NewTextValue, "Cantidad",
            InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out _, out string error);
        MostrarError(lblErrorCantidadCaja, valida || string.IsNullOrWhiteSpace(e.NewTextValue) ? "" : error);
        ActualizarBotonesVenta();
    }

    private void OnCorregirCantidadClicked(object? sender, EventArgs e)
    {
        if (articuloEnCajaSeleccionado == null)
        {
            MostrarMensaje("Selecciona un producto de la compra en caja.", true);
            return;
        }
        if (!InputHelper.TryLeerEntero(txtCantidadCaja.Text, "Cantidad",
                InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out int cantidad, out string error))
        {
            MostrarError(lblErrorCantidadCaja, error);
            return;
        }
        bool correcto = compras.CorregirCantidad(articuloEnCajaSeleccionado.Codigo, cantidad, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        RefrescarCompraEnAtencion();
    }

    private void OnQuitarCajaClicked(object? sender, EventArgs e)
    {
        if (articuloEnCajaSeleccionado == null)
        {
            MostrarMensaje("Selecciona un producto de la compra en caja.", true);
            return;
        }
        bool correcto = compras.QuitarArticulo(articuloEnCajaSeleccionado.Codigo, out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        articuloEnCajaSeleccionado = null;
        RefrescarCompraEnAtencion();
    }

    private void OnDeshacerCajaClicked(object? sender, EventArgs e)
    {
        bool correcto = compras.DeshacerCorreccion(out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        RefrescarCompraEnAtencion();
    }

    private void OnRetirarCompraClicked(object? sender, EventArgs e)
    {
        bool correcto = compras.RetirarCompraEnAtencion(out string mensaje);
        MostrarMensaje(mensaje, !correcto);
        articuloEnCajaSeleccionado = null;
        RefrescarCompraEnAtencion();
        RefrescarFila();
    }

    private async void OnConfirmarPagoClicked(object? sender, EventArgs e)
    {
        if (procesandoPago)
        {
            return;                                    // un segundo clic mientras se procesa no hace nada
        }
        procesandoPago = true;
        ActualizarBotonesVenta();                      // deshabilita Confirmar, Retirar y el selector de tarjeta
        MostrarMensaje("Procesando pago...");

        try
        {
            await Task.Delay(400);                     // pausa breve para que se vea el estado "procesando"
            bool correcto = compras.ConfirmarPagoSimulado(out Venta? venta, out string mensaje);
            MostrarMensaje(mensaje, !correcto);

            if (correcto && venta != null)
            {
                lblDetalleBoleta.Text = boletas.ObtenerComprobante(venta);
                MostrarVentas(new List<Venta> { venta }, "Boleta emitida");
                MostrarBoletaElegida(venta);
                MostrarSeccion(seccionBoleta, btnNavBoleta);         // la boleta recién emitida queda a la vista
            }
        }
        finally
        {
            procesandoPago = false;
            RefrescarTodo();
        }
    }

    private void RefrescarCompraEnAtencion()
    {
        Venta? venta = compras.CompraEnAtencion;
        actualizandoVista = true;

        brdSinCompra.IsVisible = venta == null;
        panelCompra.IsVisible = venta != null;

        if (venta == null)
        {
            lblCompraActual.Text = "";
            articuloEnCajaSeleccionado = null;
            cvArticulosCaja.ItemsSource = new List<ItemCarrito>();
            pkTarjeta.SelectedIndex = -1;
            lblCajaRegular.Text = "-";
            lblCajaAhorro.Text = "-";
            lblCajaGravada.Text = "-";
            lblCajaIgv.Text = "-";
            lblCajaFinal.Text = "-";
            lblAvisoTarjeta.Text = "";
        }
        else
        {
            string datosCliente = venta.Dni == null ? "" : $" · DNI {InputHelper.OcultarDni(venta.Dni)}";
            lblCompraActual.Text = $"Turno {venta.Turno} · {venta.ClienteTexto}{datosCliente}";

            string? codigoSeleccionado = articuloEnCajaSeleccionado?.Codigo;
            List<ItemCarrito> lineas = new List<ItemCarrito>(venta.Articulos);
            articuloEnCajaSeleccionado = null;
            foreach (ItemCarrito linea in lineas)
            {
                if (linea.Codigo == codigoSeleccionado)
                {
                    articuloEnCajaSeleccionado = linea;
                }
            }
            cvArticulosCaja.ItemsSource = lineas;
            cvArticulosCaja.SelectedItem = articuloEnCajaSeleccionado;

            pkTarjeta.SelectedIndex = venta.Tarjeta switch
            {
                TipoTarjeta.TarjetaRipley => 0,
                TipoTarjeta.OtraTarjeta => 1,
                _ => -1
            };
            lblCajaRegular.Text = InputHelper.FormatearSoles(venta.TotalRegular);
            lblCajaAhorro.Text = InputHelper.FormatearSoles(venta.Ahorro);
            lblCajaGravada.Text = InputHelper.FormatearSoles(venta.OperacionGravada);
            lblCajaIgv.Text = InputHelper.FormatearSoles(venta.Igv);
            lblCajaFinal.Text = InputHelper.FormatearSoles(venta.TotalFinal);
            lblAvisoTarjeta.Text = InputHelper.EsTarjetaPermitida(venta.Tarjeta)
                ? ""
                : "Selecciona una tarjeta para confirmar el pago.";
            lblAvisoTarjeta.IsVisible = lblAvisoTarjeta.Text.Length > 0;
        }

        actualizandoVista = false;
        ActualizarBotonesVenta();
    }

    private void ActualizarBotonesVenta()
    {
        Venta? venta = compras.CompraEnAtencion;
        bool puedeOperar = venta != null && !procesandoPago;
        bool puedeConfirmar = false;
        if (venta != null && puedeOperar)
        {
            puedeConfirmar = InputHelper.EsTarjetaPermitida(venta.Tarjeta) && venta.Articulos.Count > 0;
        }

        btnConfirmarPago.IsEnabled = puedeConfirmar;
        btnRetirarCompra.IsEnabled = puedeOperar;
        pkTarjeta.IsEnabled = puedeOperar;
        btnDeshacerCaja.IsEnabled = puedeOperar && compras.AccionesDeCorreccion > 0;
        btnAtenderSiguiente.IsEnabled = venta == null && !procesandoPago && compras.ClientesEnFila > 0;
        btnQuitarCaja.IsEnabled = puedeOperar && articuloEnCajaSeleccionado != null;
        btnCorregirCantidad.IsEnabled = puedeOperar && articuloEnCajaSeleccionado != null
            && InputHelper.TryLeerEntero(txtCantidadCaja.Text, "Cantidad", InputHelper.CantidadMinima, InputHelper.CantidadMaxima, out _, out _);
    }

    // ============================ BOLETA (BoletaService) ============================

    private void RefrescarInfoBoletas()
    {
        lblInfoBoletas.Text = boletas.ArchivoDisponible
            ? $"{boletas.CantidadVentas} boleta(s) emitidas"
            : "Las ventas no están disponibles: revisa el mensaje superior.";
        if (catalogo.StockPendienteDeGuardar)
        {
            lblInfoBoletas.Text += " · Atención: el stock no se pudo guardar.";
        }
    }

    private void OnNumeroBoletaCambiado(object? sender, TextChangedEventArgs e)
    {
        bool valido = InputHelper.TryLeerEntero(e.NewTextValue, "Número de boleta", 1, int.MaxValue, out _, out string error);
        MostrarError(lblErrorNumero, valido || string.IsNullOrWhiteSpace(e.NewTextValue) ? "" : error);
        btnBuscarNumero.IsEnabled = valido;
    }

    private void OnBuscarNumeroClicked(object? sender, EventArgs e)
    {
        if (!InputHelper.TryLeerEntero(txtNumeroBoleta.Text, "Número de boleta", 1, int.MaxValue, out int numero, out string error))
        {
            MostrarError(lblErrorNumero, error);
            return;
        }
        if (boletas.BuscarPorNumero(numero, out Venta? venta, out _) && venta != null)
        {
            MostrarVentas(new List<Venta> { venta }, "1 boleta encontrada");
            MostrarBoletaElegida(venta);
        }
        else
        {
            MostrarVentas(new List<Venta>(), $"No existe la boleta N° {numero}");
        }
    }

    private void OnDniBuscarCambiado(object? sender, TextChangedEventArgs e)
    {
        bool valido = InputHelper.TryValidarDni(e.NewTextValue, out _, out string error);
        MostrarError(lblErrorDniBuscar, valido || string.IsNullOrWhiteSpace(e.NewTextValue) ? "" : error);
        btnBuscarDni.IsEnabled = valido;
    }

    private void OnBuscarDniClicked(object? sender, EventArgs e)
    {
        if (!InputHelper.TryValidarDni(txtDniBuscar.Text, out string dni, out string error))
        {
            MostrarError(lblErrorDniBuscar, error);
            return;
        }
        ListaEnlazada<Venta> encontradas = boletas.BuscarPorDni(dni, out string mensaje);
        MostrarVentas(new List<Venta>(encontradas), mensaje);
    }

    private void OnBuscarFechaClicked(object? sender, EventArgs e)
    {
        DateTime fecha = dpFecha.Date is DateTime elegida ? elegida : DateTime.Today;
        ListaEnlazada<Venta> encontradas = boletas.BuscarPorFecha(fecha, out string mensaje);
        MostrarVentas(new List<Venta>(encontradas), mensaje);
    }

    private void OnVerTodasClicked(object? sender, EventArgs e)
    {
        ListaEnlazada<Venta> recientes = boletas.ListarRecientes();
        MostrarVentas(new List<Venta>(recientes), $"{recientes.Cantidad} boleta(s)");
    }

    private void OnVentaSeleccionada(object? sender, SelectionChangedEventArgs e)
    {
        if (actualizandoVista)
        {
            return;
        }
        if (e.CurrentSelection.Count > 0 && e.CurrentSelection[0] is Venta venta)
        {
            MostrarBoletaElegida(venta);
        }
    }

    /// <summary>Muestra en el panel derecho la boleta elegida y la marca en la lista de resultados.</summary>
    private void MostrarBoletaElegida(Venta venta)
    {
        lblBoletaSeleccionada.Text = venta.Boleta != null
            ? $"Boleta N° {venta.Boleta.Numero:D6} · {venta.ClienteTexto}"
            : venta.ClienteTexto;
        lblDetalleBoleta.Text = boletas.ObtenerComprobante(venta);
        actualizandoVista = true;
        cvVentas.SelectedItem = venta;
        actualizandoVista = false;
    }

    private void MostrarVentas(List<Venta> ventas, string mensaje)
    {
        actualizandoVista = true;
        cvVentas.SelectedItem = null;
        cvVentas.ItemsSource = ventas;
        actualizandoVista = false;
        lblResultadoVentas.Text = mensaje;
        lblBoletaSeleccionada.Text = "Ninguna boleta seleccionada";
        lblDetalleBoleta.Text = ventas.Count == 0
            ? "No hay boletas para mostrar."
            : "Elige una boleta de los resultados para ver su detalle.";
        RefrescarInfoBoletas();
    }

    // ============================ BITÁCORA ============================

    private void OnFiltroBitacoraCambiado(object? sender, EventArgs e)
    {
        RefrescarBitacora();
    }

    private void OnActualizarBitacoraClicked(object? sender, EventArgs e)
    {
        RefrescarBitacora();
    }

    /// <summary>0 = todos agrupados; 1, 2 y 3 = un solo subproceso.</summary>
    private void RefrescarBitacora()
    {
        int filtro = pkBitacora.SelectedIndex;
        List<string> lineas = new List<string>();
        if (filtro <= 0 || filtro == 1)
        {
            AgregarGrupo(lineas, CarritoService.Subproceso, carritos.Bitacora);
        }
        if (filtro <= 0 || filtro == 2)
        {
            AgregarGrupo(lineas, CompraService.Subproceso, compras.Bitacora);
        }
        if (filtro <= 0 || filtro == 3)
        {
            AgregarGrupo(lineas, BoletaService.Subproceso, boletas.Bitacora);
        }
        cvBitacora.ItemsSource = lineas;
    }

    private static void AgregarGrupo(List<string> lineas, string subproceso, ListaEnlazada<string> registros)
    {
        lineas.Add($"■ {subproceso} ({registros.Cantidad} registro(s))");
        if (registros.EstaVacia)
        {
            lineas.Add("     Sin registros todavía.");
        }
        foreach (string registro in registros)
        {
            lineas.Add("     " + registro);
        }
    }

    // ============================ RED DE CAJAS ============================

    private void PrepararGrafo()
    {
        List<string> nombres = new List<string>();
        for (int i = 0; i < grafo.CantidadCajas; i++)
        {
            nombres.Add(grafo.NombreCaja(i));
        }
        pkOrigen.ItemsSource = nombres;
        pkDestino.ItemsSource = nombres;
        pkOrigen.SelectedIndex = 0;
        pkDestino.SelectedIndex = grafo.CantidadCajas - 1;
        lblRuta.Text = "";
        RefrescarEnlaces();
    }

    private void RefrescarEnlaces()
    {
        cvEnlaces.ItemsSource = new List<string>(grafo.DescribirEnlaces());
    }

    private bool LeerCajasElegidas(out int origen, out int destino)
    {
        origen = pkOrigen.SelectedIndex;
        destino = pkDestino.SelectedIndex;
        if (origen < 0 || destino < 0)
        {
            lblRuta.Text = "Elige el origen y el destino.";
            return false;
        }
        if (origen == destino)
        {
            lblRuta.Text = "El origen y el destino deben ser distintos.";
            return false;
        }
        return true;
    }

    private void OnRutaMinimaClicked(object? sender, EventArgs e)
    {
        if (!LeerCajasElegidas(out int origen, out int destino))
        {
            return;
        }
        if (grafo.BuscarRutaMinima(origen, destino, out ListaEnlazada<int> ruta, out double distancia))
        {
            lblRuta.Text = $"Ruta: {grafo.DescribirRuta(ruta)}{Environment.NewLine}" +
                           $"Tiempo total: {distancia:0.##} ms";
        }
        else
        {
            lblRuta.Text = $"No hay una ruta disponible entre {grafo.NombreCaja(origen)} y {grafo.NombreCaja(destino)}.";
        }
    }

    private void OnActivarEnlaceClicked(object? sender, EventArgs e)
    {
        CambiarEstadoEnlace(true);
    }

    private void OnDesactivarEnlaceClicked(object? sender, EventArgs e)
    {
        CambiarEstadoEnlace(false);
    }

    private void CambiarEstadoEnlace(bool activo)
    {
        if (!LeerCajasElegidas(out int origen, out int destino))
        {
            return;
        }
        grafo.CambiarEstadoEnlace(origen, destino, activo, out string mensaje);
        lblRuta.Text = mensaje;
        RefrescarEnlaces();
    }

    private void OnPesoCambiado(object? sender, TextChangedEventArgs e)
    {
        bool valido = InputHelper.TryLeerDouble(e.NewTextValue, "Tiempo", 0.1, 10000, 2, out _, out string error);
        MostrarError(lblErrorPeso, valido || string.IsNullOrWhiteSpace(e.NewTextValue) ? "" : error);
        btnCambiarPeso.IsEnabled = valido;
    }

    private void OnCambiarPesoClicked(object? sender, EventArgs e)
    {
        if (!InputHelper.TryLeerDouble(txtPeso.Text, "Tiempo", 0.1, 10000, 2, out double peso, out string error))
        {
            MostrarError(lblErrorPeso, error);
            return;
        }
        if (!LeerCajasElegidas(out int origen, out int destino))
        {
            return;
        }
        grafo.CambiarPeso(origen, destino, peso, out string mensaje);
        lblRuta.Text = mensaje;
        RefrescarEnlaces();
    }

    private void OnRestablecerGrafoClicked(object? sender, EventArgs e)
    {
        grafo = GrafoCajas.CrearEjemplo();
        PrepararGrafo();
        lblRuta.Text = "Se restauró la red inicial.";
    }
}
