using System;

namespace RipleyComprasMAUI.Models;


public class Boleta
{
    public const string LeyendaSimulada = "Boleta simulada - sin validez tributaria";

    public int Numero { get; set; }                 
    public DateTime FechaEmision { get; set; }      
    public Guid IdVenta { get; set; }               
    public string Leyenda { get; set; } = LeyendaSimulada;
}
