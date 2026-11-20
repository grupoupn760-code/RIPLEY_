namespace RipleyComprasMAUI.Models;

public class Producto
{
    public string Codigo { get; set; } = "";        
    public string Nombre { get; set; } = "";
    public decimal PrecioRegular { get; set; }
    public decimal PrecioTarjetaRipley { get; set; }     
    public int Stock { get; set; }                        
}
