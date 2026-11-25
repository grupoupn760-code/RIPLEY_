namespace RipleyComprasMAUI.Models;

// Prenda del catálogo: código de 13 dígitos (texto), nombre, precio regular, precio Tarjeta Ripley y stock.
public class Producto
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal PrecioRegular { get; set; }
    public decimal PrecioTarjetaRipley { get; set; }
    public int Stock { get; set; }
}
