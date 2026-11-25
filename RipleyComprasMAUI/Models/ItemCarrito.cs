using System.Text.Json.Serialization;

namespace RipleyComprasMAUI.Models;


// Línea del carrito: prenda, cantidad y precio aplicado según la tarjeta.
public class ItemCarrito
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioRegular { get; set; }
    public decimal PrecioTarjetaRipley { get; set; }
    public decimal PrecioAplicado { get; set; }

    [JsonIgnore]
    public decimal SubtotalRegular => PrecioRegular * Cantidad;

    [JsonIgnore]
    public decimal SubtotalAplicado => PrecioAplicado * Cantidad;

    // Copia independiente (para no modificar el carrito original al corregir en caja).
    public ItemCarrito Copiar()
    {
        return new ItemCarrito
        {
            Codigo = Codigo,
            Nombre = Nombre,
            Cantidad = Cantidad,
            PrecioRegular = PrecioRegular,
            PrecioTarjetaRipley = PrecioTarjetaRipley,
            PrecioAplicado = PrecioAplicado
        };
    }
}
