using System;
using System.Collections.Generic;
using System.Text;
using RipleyComprasMAUI.Data;
using RipleyComprasMAUI.Helpers;
using RipleyComprasMAUI.Models;
using RipleyComprasMAUI.Structures;

namespace RipleyComprasMAUI.Services;


// Catálogo: carga productos.json en un árbol AVL por código y maneja el stock.
public class CatalogoService
{
    private readonly JsonDataManager datos;
    private ArbolBusqueda<string, Producto> arbol = new ArbolBusqueda<string, Producto>(StringComparer.Ordinal);
    private readonly List<Producto> rechazados = new List<Producto>();

    public CatalogoService(JsonDataManager datos)
    {
        this.datos = datos;
    }

    public bool Cargado { get; private set; }
    public ListaEnlazada<string> Advertencias { get; } = new ListaEnlazada<string>();
    public bool StockPendienteDeGuardar { get; private set; }

    public int Cantidad => arbol.Cantidad;
    public int Altura => arbol.Altura;
    public int Rotaciones => arbol.Rotaciones;
    public int UltimasComparaciones => arbol.UltimasComparaciones;
    public string? CodigoRaiz => arbol.ClaveRaiz;

    // Lee productos.json, valida cada prenda y la inserta en el árbol.
    public bool Cargar(out string mensaje)
    {
        arbol = new ArbolBusqueda<string, Producto>(StringComparer.Ordinal);
        rechazados.Clear();
        Advertencias.Limpiar();
        Cargado = false;

        if (!datos.CargarProductos(out List<Producto> productos, out string error))
        {
            mensaje = "Catálogo no disponible: " + error;
            return false;
        }

        foreach (Producto producto in productos)
        {
            if (producto is null)
            {
                Advertencias.AgregarAlFinal("Se ignoró un elemento null en productos.json.");
                continue;
            }
            if (!EsProductoValido(producto, out string motivo))
            {
                rechazados.Add(producto);
                Advertencias.AgregarAlFinal($"Prenda rechazada ({producto.Codigo}): {motivo}.");
                continue;
            }
            if (!arbol.Insertar(producto.Codigo, producto))
            {
                rechazados.Add(producto);
                Advertencias.AgregarAlFinal($"Código repetido {producto.Codigo}: se conservó la primera prenda.");
            }
        }

        Cargado = true;
        mensaje = $"Catálogo: {arbol.Cantidad} prendas.";
        if (!Advertencias.EstaVacia)
        {
            mensaje += $" {Advertencias.Cantidad} registro(s) rechazado(s).";
        }
        return true;
    }

    public static bool EsProductoValido(Producto producto, out string motivo)
    {
        if (!InputHelper.EsCodigoValido(producto.Codigo))
        {
            motivo = "el código debe tener exactamente 13 dígitos";
            return false;
        }
        if (!InputHelper.TryValidarNombreProducto(producto.Nombre, out _, out motivo))
        {
            return false;
        }
        if (!InputHelper.EsImporteValido(producto.PrecioRegular))
        {
            motivo = "el precio regular debe ser mayor que 0 y tener como máximo 2 decimales";
            return false;
        }
        if (!InputHelper.EsImporteValido(producto.PrecioTarjetaRipley) || producto.PrecioTarjetaRipley > producto.PrecioRegular)
        {
            motivo = "el precio Tarjeta Ripley debe ser mayor que 0 y no mayor que el precio regular";
            return false;
        }
        if (producto.Stock < 0)
        {
            motivo = "el stock no puede ser negativo";
            return false;
        }
        motivo = "";
        return true;
    }

    // Valida el código y lo busca en el árbol. O(log n)
    public bool BuscarPorCodigo(string? texto, out Producto? producto, out string mensaje)
    {
        producto = null;
        if (!InputHelper.TryValidarCodigo(texto, out string codigo, out mensaje))
        {
            return false;
        }
        if (!arbol.Buscar(codigo, out producto) || producto == null)
        {
            mensaje = "Prenda no encontrada.";
            return false;
        }
        mensaje = $"Prenda encontrada: {producto.Nombre}.";
        return true;
    }

    // Recorre el catálogo sin distinguir mayúsculas ni tildes. O(n)
    public ListaEnlazada<Producto> BuscarPorNombre(string? texto, out string mensaje)
    {
        ListaEnlazada<Producto> encontrados = new ListaEnlazada<Producto>();
        if (!InputHelper.TryValidarTextoBusqueda(texto, out string consulta, out mensaje))
        {
            return encontrados;
        }

        foreach (Producto producto in arbol.RecorrerInOrden())
        {
            if (InputHelper.NombreContiene(producto.Nombre, consulta))
            {
                encontrados.AgregarAlFinal(producto);
            }
        }

        mensaje = encontrados.EstaVacia
            ? "Prenda no encontrada."
            : $"{encontrados.Cantidad} producto(s) encontrado(s) para \"{consulta}\".";
        return encontrados;
    }


    public ListaEnlazada<Producto> Listar()
    {
        return arbol.RecorrerInOrden();
    }

    public bool ObtenerProducto(string codigo, out Producto? producto)
    {
        return arbol.Buscar(codigo, out producto) && producto != null;
    }

    public int StockDisponible(string codigo)
    {
        return ObtenerProducto(codigo, out Producto? producto) ? producto!.Stock : 0;
    }

    // Vuelve a validar el stock antes de confirmar el pago.
    public bool HayStockSuficiente(IEnumerable<ItemCarrito> articulos, out string conflictos)
    {
        StringBuilder detalle = new StringBuilder();
        foreach (ItemCarrito articulo in articulos)
        {
            int disponible = StockDisponible(articulo.Codigo);
            if (articulo.Cantidad > disponible)
            {
                detalle.AppendLine($"- {articulo.Nombre}: pide {articulo.Cantidad} y hay {disponible}.");
            }
        }
        conflictos = detalle.ToString().TrimEnd();
        return conflictos.Length == 0;
    }

    // Guarda productos.json con el stock ya descontado.
    public bool GuardarConDescuento(IEnumerable<ItemCarrito> articulos, out string error)
    {
        if (!datos.GuardarProductos(CrearListaParaGuardar(articulos), out error))
        {
            return false;
        }
        StockPendienteDeGuardar = false;
        return true;
    }

    public void AplicarDescuento(IEnumerable<ItemCarrito> articulos)
    {
        foreach (ItemCarrito articulo in articulos)
        {
            if (ObtenerProducto(articulo.Codigo, out Producto? producto))
            {
                producto!.Stock -= articulo.Cantidad;
            }
        }
    }

    public void MarcarStockPendiente()
    {
        StockPendienteDeGuardar = true;
    }

    private List<Producto> CrearListaParaGuardar(IEnumerable<ItemCarrito>? descontar)
    {
        List<Producto> lista = new List<Producto>();
        foreach (Producto producto in arbol.RecorrerInOrden())
        {
            int vendido = 0;
            if (descontar != null)
            {
                foreach (ItemCarrito articulo in descontar)
                {
                    if (articulo.Codigo == producto.Codigo)
                    {
                        vendido += articulo.Cantidad;
                    }
                }
            }
            lista.Add(new Producto
            {
                Codigo = producto.Codigo,
                Nombre = producto.Nombre,
                PrecioRegular = producto.PrecioRegular,
                PrecioTarjetaRipley = producto.PrecioTarjetaRipley,
                Stock = producto.Stock - vendido
            });
        }
        lista.AddRange(rechazados);
        return lista;
    }
}
