using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RipleyComprasMAUI.Models;

// Estados de una compra: en fila, en caja y finalizada.
public enum EstadoVenta
{
    EnFila,
    EnAtencion,
    Finalizada
}


// Tarjeta elegida en caja; define el precio aplicado.
public enum TipoTarjeta
{
    SinSeleccionar,
    TarjetaRipley,
    OtraTarjeta
}

// Compra de un cliente: turno, datos opcionales, artículos, tarjeta, importes y su boleta.
public class Venta
{
    public Guid Id { get; set; }
    public int Turno { get; set; }
    public string? NombreCliente { get; set; }
    public string? Dni { get; set; }
    public string? Correo { get; set; }
    public EstadoVenta Estado { get; set; } = EstadoVenta.EnFila;
    // List solo para guardar en JSON; las operaciones usan ListaEnlazada (ver CarritoService).
    public List<ItemCarrito> Articulos { get; set; } = new();
    public TipoTarjeta Tarjeta { get; set; } = TipoTarjeta.SinSeleccionar;
    public DateTime FechaLlegada { get; set; }
    public decimal TotalRegular { get; set; }
    public decimal Ahorro { get; set; }
    public decimal TotalFinal { get; set; }
    public decimal OperacionGravada { get; set; }
    public decimal Igv { get; set; }
    public Boleta? Boleta { get; set; }

    [JsonIgnore]
    public string ClienteTexto => string.IsNullOrWhiteSpace(NombreCliente) ? "Cliente anónimo" : NombreCliente;

    [JsonIgnore]
    public string TarjetaTexto => NombreTarjeta(Tarjeta);

    [JsonIgnore]
    public int Unidades
    {
        get
        {
            int total = 0;
            foreach (ItemCarrito articulo in Articulos)
            {
                total += articulo.Cantidad;
            }
            return total;
        }
    }

    public static string NombreTarjeta(TipoTarjeta tarjeta)
    {
        return tarjeta switch
        {
            TipoTarjeta.TarjetaRipley => "Tarjeta Ripley",
            TipoTarjeta.OtraTarjeta => "Otra tarjeta",
            _ => "Sin tarjeta elegida"
        };
    }
}
