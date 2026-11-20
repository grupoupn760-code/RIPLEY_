using System.Collections.Generic;
using RipleyComprasMAUI.Helpers;
using RipleyComprasMAUI.Models;
using RipleyComprasMAUI.Structures;

namespace RipleyComprasMAUI.Services;

public enum TipoAccionCarrito
{
    AgregarLinea,       
    CambiarCantidad,    
    QuitarLinea       
}


public class AccionCarrito
{
    public TipoAccionCarrito Tipo { get; init; }
    public string Codigo { get; init; } = "";
    public int CantidadAnterior { get; init; }          
    public int Posicion { get; init; }                
    public ItemCarrito? ArticuloQuitado { get; init; }  
    public string Descripcion { get; init; } = "";
}


public class ContextoCarrito
{
    public ContextoCarrito(string referencia)
    {
        Referencia = referencia;
    }

    public string Referencia { get; }                                                  
    internal ListaEnlazada<ItemCarrito> Lista { get; } = new ListaEnlazada<ItemCarrito>();   
    internal PilaAcciones<AccionCarrito> Pila { get; } = new PilaAcciones<AccionCarrito>();

    public IEnumerable<ItemCarrito> Articulos => Lista;      
    public int CantidadLineas => Lista.Cantidad;
    public bool EstaVacio => Lista.EstaVacia;
    public int AccionesParaDeshacer => Pila.Cantidad;
}


public class CarritoService
{
    public const string Subproceso = "Preventa - Selección de productos";

    private readonly CatalogoService catalogo;
    private int numeroCarrito;

    public CarritoService(CatalogoService catalogo)
    {
        this.catalogo = catalogo;
        numeroCarrito = 1;
        Actual = new ContextoCarrito("Carrito 1");
    }

    public ContextoCarrito Actual { get; private set; }

    public ListaEnlazada<string> Bitacora { get; } = new ListaEnlazada<string>();

    public void NuevoCarrito()
    {
        numeroCarrito++;
        Actual = new ContextoCarrito($"Carrito {numeroCarrito}");
    }

    public bool AgregarPrenda(ContextoCarrito carrito, string codigo, int cantidad, out string mensaje)
    {
        if (!InputHelper.EsCodigoValido(codigo))
        {
            mensaje = "Código inválido: debe tener exactamente 13 dígitos.";
            Registrar(carrito, "Agregar prenda", "Rechazado: " + mensaje);
            return false;
        }
        if (cantidad < InputHelper.CantidadMinima || cantidad > InputHelper.CantidadMaxima)
        {
            mensaje = $"La cantidad debe estar entre {InputHelper.CantidadMinima} y {InputHelper.CantidadMaxima}.";
            Registrar(carrito, "Agregar prenda", "Rechazado: " + mensaje);
            return false;
        }
        if (!catalogo.ObtenerProducto(codigo, out Producto? producto) || producto == null)
        {
            mensaje = "Prenda no encontrada.";
            Registrar(carrito, "Agregar prenda", "Rechazado: " + mensaje);
            return false;
        }

        ItemCarrito? existente = carrito.Lista.Buscar(articulo => articulo.Codigo == codigo);   
        int cantidadAnterior = existente == null ? 0 : existente.Cantidad;
        int nuevaCantidad = cantidadAnterior + cantidad;

        if (nuevaCantidad > InputHelper.CantidadMaxima)
        {
            mensaje = $"Máximo {InputHelper.CantidadMaxima} unidades por prenda (ya hay {cantidadAnterior}).";
            Registrar(carrito, "Agregar prenda", "Rechazado: " + mensaje);
            return false;
        }
        if (nuevaCantidad > producto.Stock)
        {
            mensaje = $"Stock insuficiente de {producto.Nombre}: hay {producto.Stock} y el carrito tendría {nuevaCantidad}.";
            Registrar(carrito, "Agregar prenda", "Rechazado: " + mensaje);
            return false;
        }

        if (existente == null)
        {
            carrito.Lista.AgregarAlFinal(new ItemCarrito
            {
                Codigo = producto.Codigo,
                Nombre = producto.Nombre,
                Cantidad = cantidad,
                PrecioRegular = producto.PrecioRegular,
                PrecioTarjetaRipley = producto.PrecioTarjetaRipley
            });
            carrito.Pila.Apilar(new AccionCarrito
            {
                Tipo = TipoAccionCarrito.AgregarLinea,
                Codigo = codigo,
                Descripcion = $"agregar {producto.Nombre} x {cantidad}"
            });
        }
        else
        {
            carrito.Pila.Apilar(new AccionCarrito
            {
                Tipo = TipoAccionCarrito.CambiarCantidad,
                Codigo = codigo,
                CantidadAnterior = cantidadAnterior,
                Descripcion = $"{producto.Nombre} de {cantidadAnterior} a {nuevaCantidad}"
            });
            existente.Cantidad = nuevaCantidad;      
        }

        mensaje = $"Se agregó {producto.Nombre} x {cantidad}. Cantidad en el carrito: {nuevaCantidad}.";
        Registrar(carrito, "Agregar prenda", mensaje);
        return true;
    }

    public bool CambiarCantidad(ContextoCarrito carrito, string codigo, int nuevaCantidad, out string mensaje)
    {
        ItemCarrito? articulo = carrito.Lista.Buscar(item => item.Codigo == codigo);
        if (articulo == null)
        {
            mensaje = "El artículo no está en el carrito.";
            Registrar(carrito, "Cambiar cantidad", "Rechazado: " + mensaje);
            return false;
        }
        if (nuevaCantidad < InputHelper.CantidadMinima || nuevaCantidad > InputHelper.CantidadMaxima)
        {
            mensaje = $"La cantidad debe estar entre {InputHelper.CantidadMinima} y {InputHelper.CantidadMaxima}. Para sacar la prenda usa Quitar.";
            Registrar(carrito, "Cambiar cantidad", "Rechazado: " + mensaje);
            return false;
        }
        int stock = catalogo.StockDisponible(codigo);
        if (nuevaCantidad > stock)
        {
            mensaje = $"Stock insuficiente de {articulo.Nombre}: hay {stock}.";
            Registrar(carrito, "Cambiar cantidad", "Rechazado: " + mensaje);
            return false;
        }
        if (nuevaCantidad == articulo.Cantidad)
        {
            mensaje = "La cantidad no cambió.";
            return false;
        }

        carrito.Pila.Apilar(new AccionCarrito
        {
            Tipo = TipoAccionCarrito.CambiarCantidad,
            Codigo = codigo,
            CantidadAnterior = articulo.Cantidad,
            Descripcion = $"{articulo.Nombre} de {articulo.Cantidad} a {nuevaCantidad}"
        });
        articulo.Cantidad = nuevaCantidad;

        mensaje = $"{articulo.Nombre}: cantidad {nuevaCantidad}.";
        Registrar(carrito, "Cambiar cantidad", mensaje);
        return true;
    }

    public bool QuitarPrenda(ContextoCarrito carrito, string codigo, out string mensaje)
    {
        if (!carrito.Lista.Eliminar(item => item.Codigo == codigo, out ItemCarrito? quitado, out int posicion) || quitado == null)
        {
            mensaje = "El artículo no está en el carrito.";
            Registrar(carrito, "Quitar prenda", "Rechazado: " + mensaje);
            return false;
        }

        carrito.Pila.Apilar(new AccionCarrito
        {
            Tipo = TipoAccionCarrito.QuitarLinea,
            Codigo = codigo,
            Posicion = posicion,
            ArticuloQuitado = quitado.Copiar(),
            Descripcion = $"quitar {quitado.Nombre}"
        });

        mensaje = $"Se quitó {quitado.Nombre} del carrito.";
        Registrar(carrito, "Quitar prenda", mensaje);
        return true;
    }

    public bool Deshacer(ContextoCarrito carrito, out string mensaje)
    {
        if (carrito.Pila.EstaVacia)
        {
            mensaje = "No hay acciones para deshacer.";
            Registrar(carrito, "Deshacer", mensaje);
            return false;
        }

        AccionCarrito accion = carrito.Pila.Desapilar();     
        switch (accion.Tipo)
        {
            case TipoAccionCarrito.AgregarLinea:
                carrito.Lista.Eliminar(item => item.Codigo == accion.Codigo, out _, out _);
                break;

            case TipoAccionCarrito.CambiarCantidad:
                ItemCarrito? articulo = carrito.Lista.Buscar(item => item.Codigo == accion.Codigo);
                if (articulo != null)
                {
                    articulo.Cantidad = accion.CantidadAnterior;
                }
                break;

            case TipoAccionCarrito.QuitarLinea:
                if (accion.ArticuloQuitado != null)
                {
                    carrito.Lista.InsertarEn(accion.Posicion, accion.ArticuloQuitado.Copiar());
                }
                break;
        }

        mensaje = $"Se deshizo: {accion.Descripcion}.";
        Registrar(carrito, "Deshacer", mensaje);
        return true;
    }

    public void Vaciar(ContextoCarrito carrito, out string mensaje)
    {
        int lineas = carrito.Lista.Cantidad;
        carrito.Lista.Limpiar();
        carrito.Pila.Limpiar();
        mensaje = $"Carrito vaciado ({lineas} línea(s) retiradas).";
        Registrar(carrito, "Vaciar carrito", mensaje);
    }

    public decimal CalcularTotalRegular(ContextoCarrito carrito)
    {
        decimal total = 0;
        foreach (ItemCarrito articulo in carrito.Lista)
        {
            total += articulo.SubtotalRegular;
        }
        return total;
    }

    public int CantidadEnCarrito(ContextoCarrito carrito, string codigo)
    {
        ItemCarrito? articulo = carrito.Lista.Buscar(item => item.Codigo == codigo);
        return articulo == null ? 0 : articulo.Cantidad;
    }

    public List<ItemCarrito> CopiarArticulos(ContextoCarrito carrito)
    {
        List<ItemCarrito> copia = new List<ItemCarrito>();
        foreach (ItemCarrito articulo in carrito.Lista)
        {
            copia.Add(articulo.Copiar());
        }
        return copia;
    }

    public ContextoCarrito CrearContextoDeCorreccion(IEnumerable<ItemCarrito> articulos, string referencia)
    {
        ContextoCarrito contexto = new ContextoCarrito(referencia);
        foreach (ItemCarrito articulo in articulos)
        {
            contexto.Lista.AgregarAlFinal(articulo.Copiar());
        }
        return contexto;
    }

    private void Registrar(ContextoCarrito carrito, string accion, string resultado)
    {
        Bitacora.AgregarAlFinal(InputHelper.CrearRegistro(accion, carrito.Referencia, resultado));
    }
}
