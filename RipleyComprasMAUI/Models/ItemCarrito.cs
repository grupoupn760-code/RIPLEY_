using System.Text.Json.Serialization;

namespace RipleyComprasMAUI.Models;


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
