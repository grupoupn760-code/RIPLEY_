using System;
using System.Collections.Generic;
using RipleyComprasMAUI.Helpers;
using RipleyComprasMAUI.Models;
using RipleyComprasMAUI.Structures;

namespace RipleyComprasMAUI.Services;

// Venta: fila de compras (cola), tarjeta, importes y pago simulado.
public class CompraService
{
    public const string Subproceso = "Venta - Pago";
    private const decimal FactorIgv = 1.18m;

    private readonly CarritoService carritos;
    private readonly CatalogoService catalogo;
    private readonly BoletaService boletas;
    private readonly ColaCompras<Venta> fila = new ColaCompras<Venta>();
    private int siguienteTurno = 1;
    private ContextoCarrito? correccion;
    private bool confirmando;

    public CompraService(CarritoService carritos, CatalogoService catalogo, BoletaService boletas)
    {
        this.carritos = carritos;
        this.catalogo = catalogo;
        this.boletas = boletas;
    }

    public Venta? CompraEnAtencion { get; private set; }
    public ListaEnlazada<string> Bitacora { get; } = new ListaEnlazada<string>();
    public int ClientesEnFila => fila.Cantidad;
    public int AccionesDeCorreccion => correccion == null ? 0 : correccion.AccionesParaDeshacer;

    public List<Venta> ObtenerFila()
    {
        return new List<Venta>(fila);
    }

    // “Enviar a caja”: valida datos opcionales y encola la compra. O(1)
    public bool RegistrarLlegada(string? nombre, string? dni, string? correo, out Venta? venta, out string mensaje)
    {
        venta = null;
        if (!InputHelper.TryValidarNombrePersona(nombre, out string? nombreValido, out mensaje)
            || !InputHelper.TryValidarDniOpcional(dni, out string? dniValido, out mensaje)
            || !InputHelper.TryValidarCorreo(correo, out string? correoValido, out mensaje))
        {
            Registrar("Registrar llegada", carritos.Actual.Referencia, "Rechazado: " + mensaje);
            return false;
        }

        ContextoCarrito carrito = carritos.Actual;
        if (carrito.EstaVacio)
        {
            mensaje = "El carrito está vacío: agrega al menos un producto antes de enviarlo a caja.";
            Registrar("Registrar llegada", carrito.Referencia, "Rechazado: " + mensaje);
            return false;
        }

        venta = new Venta
        {
            Id = Guid.NewGuid(),
            Turno = siguienteTurno,
            NombreCliente = nombreValido,
            Dni = dniValido,
            Correo = correoValido,
            Estado = EstadoVenta.EnFila,
            FechaLlegada = DateTime.Now,
            Articulos = carritos.CopiarArticulos(carrito)
        };
        siguienteTurno++;
        CalcularImportes(venta);
        fila.Encolar(venta);
        carritos.NuevoCarrito();

        mensaje = $"Turno {venta.Turno} ({venta.ClienteTexto}) enviado a caja con {venta.Articulos.Count} producto(s). Clientes en fila: {fila.Cantidad}.";
        Registrar("Registrar llegada", $"Turno {venta.Turno}", mensaje);
        return true;
    }

    // Desencola al primero de la fila y lo pone en caja. O(1)
    public bool AtenderSiguiente(out string mensaje)
    {
        if (CompraEnAtencion != null)
        {
            mensaje = $"Ya se atiende el turno {CompraEnAtencion.Turno}: confirma el pago o retira la compra antes de atender al siguiente.";
            Registrar("Atender siguiente", $"Turno {CompraEnAtencion.Turno}", "Rechazado: " + mensaje);
            return false;
        }
        if (fila.EstaVacia)
        {
            mensaje = "No hay clientes en la fila.";
            Registrar("Atender siguiente", "-", "Rechazado: " + mensaje);
            return false;
        }

        Venta venta = fila.Desencolar();
        venta.Estado = EstadoVenta.EnAtencion;
        CompraEnAtencion = venta;
        correccion = carritos.CrearContextoDeCorreccion(venta.Articulos, $"Turno {venta.Turno} (corrección en caja)");
        CalcularImportes(venta);

        mensaje = $"Atendiendo el turno {venta.Turno} ({venta.ClienteTexto}). Quedan {fila.Cantidad} en la fila. Selecciona una tarjeta.";
        Registrar("Atender siguiente", $"Turno {venta.Turno}", mensaje);
        return true;
    }

    // Guarda la tarjeta y recalcula los importes.
    public bool SeleccionarTarjeta(TipoTarjeta tarjeta, out string mensaje)
    {
        Venta? venta = CompraEnAtencion;
        if (venta == null)
        {
            mensaje = "No hay una compra en atención.";
            return false;
        }
        if (!InputHelper.EsTarjetaPermitida(tarjeta))
        {
            mensaje = "Selecciona una tarjeta: Tarjeta Ripley u Otra tarjeta.";
            Registrar("Elegir tarjeta", $"Turno {venta.Turno}", "Rechazado: " + mensaje);
            return false;
        }

        venta.Tarjeta = tarjeta;
        CalcularImportes(venta);
        mensaje = $"{venta.TarjetaTexto}: total final {InputHelper.FormatearSoles(venta.TotalFinal)}, ahorro {InputHelper.FormatearSoles(venta.Ahorro)}.";
        Registrar("Elegir tarjeta", $"Turno {venta.Turno}", mensaje);
        return true;
    }

    // Tarjeta Ripley → precio especial; otra tarjeta → precio regular.
    public static decimal PrecioSegunTarjeta(ItemCarrito articulo, TipoTarjeta tarjeta)
    {
        return tarjeta == TipoTarjeta.TarjetaRipley ? articulo.PrecioTarjetaRipley : articulo.PrecioRegular;
    }

    // Total regular, total final, ahorro, op. gravada = total/1.18 e IGV = total − op. gravada. O(m)
    public static void CalcularImportes(Venta venta)
    {
        decimal totalRegular = 0;
        decimal totalFinal = 0;
        foreach (ItemCarrito articulo in venta.Articulos)
        {
            articulo.PrecioAplicado = PrecioSegunTarjeta(articulo, venta.Tarjeta);
            totalRegular += articulo.SubtotalRegular;
            totalFinal += articulo.SubtotalAplicado;
        }

        venta.TotalRegular = totalRegular;
        venta.TotalFinal = totalFinal;
        venta.Ahorro = totalRegular - totalFinal;
        venta.OperacionGravada = Redondear(totalFinal / FactorIgv);
        venta.Igv = totalFinal - venta.OperacionGravada;
    }

    private static decimal Redondear(decimal monto)
    {
        return Math.Round(monto, 2, MidpointRounding.AwayFromZero);
    }

    public decimal EstimarTotalTarjetaRipley(IEnumerable<ItemCarrito> articulos)
    {
        decimal total = 0;
        foreach (ItemCarrito articulo in articulos)
        {
            total += PrecioSegunTarjeta(articulo, TipoTarjeta.TarjetaRipley) * articulo.Cantidad;
        }
        return total;
    }



    // Corrección en caja (por ejemplo, si falta stock); se puede deshacer.
    public bool CorregirCantidad(string codigo, int nuevaCantidad, out string mensaje)
    {
        if (CompraEnAtencion == null || correccion == null)
        {
            mensaje = "No hay una compra en atención.";
            return false;
        }
        if (!carritos.CambiarCantidad(correccion, codigo, nuevaCantidad, out mensaje))
        {
            return false;
        }
        SincronizarCorreccion("Corregir cantidad", mensaje);
        return true;
    }

    public bool QuitarArticulo(string codigo, out string mensaje)
    {
        if (CompraEnAtencion == null || correccion == null)
        {
            mensaje = "No hay una compra en atención.";
            return false;
        }
        if (!carritos.QuitarPrenda(correccion, codigo, out mensaje))
        {
            return false;
        }
        SincronizarCorreccion("Quitar artículo", mensaje);
        return true;
    }

    public bool DeshacerCorreccion(out string mensaje)
    {
        if (CompraEnAtencion == null || correccion == null)
        {
            mensaje = "No hay una compra en atención.";
            return false;
        }
        if (!carritos.Deshacer(correccion, out mensaje))
        {
            return false;
        }
        SincronizarCorreccion("Deshacer corrección", mensaje);
        return true;
    }

    private void SincronizarCorreccion(string accion, string detalle)
    {
        Venta venta = CompraEnAtencion!;
        venta.Articulos = carritos.CopiarArticulos(correccion!);
        CalcularImportes(venta);
        Registrar(accion, $"Turno {venta.Turno}", detalle + $" Nuevo total final: {InputHelper.FormatearSoles(venta.TotalFinal)}.");
    }

    public bool RetirarCompraEnAtencion(out string mensaje)
    {
        Venta? venta = CompraEnAtencion;
        if (venta == null)
        {
            mensaje = "No hay una compra en atención.";
            return false;
        }
        CompraEnAtencion = null;
        correccion = null;
        mensaje = $"El turno {venta.Turno} se retiró sin pagar. No se emitió boleta ni se descontó stock.";
        Registrar("Retirar compra", $"Turno {venta.Turno}", mensaje);
        return true;
    }


    // Revalida stock, emite la boleta, guarda ventas.json y productos.json; si algo falla, revierte.
    public bool ConfirmarPagoSimulado(out Venta? ventaFinalizada, out string mensaje)
    {
        ventaFinalizada = null;
        Venta? venta = CompraEnAtencion;

        if (venta == null)
        {
            mensaje = "No hay una compra en atención para confirmar.";
            Registrar("Confirmar pago simulado", "-", "Rechazado: " + mensaje);
            return false;
        }
        string referencia = $"Turno {venta.Turno}";
        if (confirmando)
        {
            mensaje = "La confirmación ya se está procesando.";
            return false;
        }
        if (!InputHelper.EsTarjetaPermitida(venta.Tarjeta))
        {
            mensaje = "Selecciona una tarjeta antes de confirmar el pago.";
            Registrar("Confirmar pago simulado", referencia, "Rechazado: " + mensaje);
            return false;
        }
        if (venta.Articulos.Count == 0)
        {
            mensaje = "La compra no tiene productos: corrígela o retírala.";
            Registrar("Confirmar pago simulado", referencia, "Rechazado: " + mensaje);
            return false;
        }

        confirmando = true;
        try
        {
            CalcularImportes(venta);

            if (!catalogo.HayStockSuficiente(venta.Articulos, out string conflictos))
            {
                mensaje = "Falta stock (otra venta usó unidades mientras el cliente esperaba):\n" + conflictos
                          + "\nCorrija la compra en atención y vuelva a confirmar.";
                Registrar("Confirmar pago simulado", referencia, "Rechazado por stock: " + conflictos.Replace('\n', ' '));
                return false;
            }

            Boleta boleta = boletas.PrepararBoleta(venta);
            if (!boletas.GuardarConNuevaVenta(venta, boleta, out string errorVentas))
            {
                mensaje = "No se registró la venta: " + errorVentas + " La compra sigue en atención; puedes reintentar.";
                Registrar("Confirmar pago simulado", referencia, "Error al guardar ventas.json");
                return false;
            }

            bool stockGuardado = catalogo.GuardarConDescuento(venta.Articulos, out string errorStock);
            if (!stockGuardado)
            {
                if (boletas.RestaurarArchivoVentas(venta, out _))
                {
                    mensaje = "No se pudo guardar el stock (" + errorStock + "). Se restauró ventas.json: la venta no quedó registrada y sigue en atención; puedes reintentar.";
                    Registrar("Confirmar pago simulado", referencia, "Error al guardar productos.json; ventas.json restaurado");
                    return false;
                }

                catalogo.MarcarStockPendiente();
            }

            boletas.PublicarVenta(venta);
            catalogo.AplicarDescuento(venta.Articulos);
            CompraEnAtencion = null;
            correccion = null;
            ventaFinalizada = venta;

            mensaje = $"Pago simulado confirmado. Boleta N° {boleta.Numero:D6} emitida para el turno {venta.Turno}.";
            if (!stockGuardado)
            {
                mensaje += " ADVERTENCIA: la venta quedó registrada, pero productos.json no se pudo guardar (" + errorStock
                           + "). El stock correcto está en memoria y se volverá a guardar en la siguiente venta.";
            }
            Registrar("Confirmar pago simulado", referencia, mensaje);
            return true;
        }
        finally
        {
            confirmando = false;
        }
    }

    private void Registrar(string accion, string referencia, string resultado)
    {
        Bitacora.AgregarAlFinal(InputHelper.CrearRegistro(accion, referencia, resultado));
    }
}
