using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using RipleyComprasMAUI.Models;

namespace RipleyComprasMAUI.Data;

public class JsonDataManager
{
    private const string ArchivoProductos = "productos.json";
    private const string ArchivoVentas = "ventas.json";

    private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),   
        Converters = { new JsonStringEnumConverter() }          
    };

    private readonly Func<Task<Stream>> abrirSemillaProductos;

    public JsonDataManager(string carpetaDatos, Func<Task<Stream>> abrirSemillaProductos)
    {
        CarpetaDatos = carpetaDatos;
        this.abrirSemillaProductos = abrirSemillaProductos;
    }

    public string CarpetaDatos { get; }
    public string RutaProductos => Path.Combine(CarpetaDatos, ArchivoProductos);
    public string RutaVentas => Path.Combine(CarpetaDatos, ArchivoVentas);


    public async Task<(bool Correcto, string Mensaje)> PrepararArchivosAsync()
    {
        try
        {
            Directory.CreateDirectory(CarpetaDatos);
            string creados = "";

            if (!File.Exists(RutaProductos))
            {
                using Stream semilla = await abrirSemillaProductos();
                using StreamReader lector = new StreamReader(semilla);
                string json = await lector.ReadToEndAsync();
                EscribirConReemplazo(RutaProductos, json);
                creados += " productos.json (copiado de la semilla)";
            }
            if (!File.Exists(RutaVentas))
            {
                EscribirConReemplazo(RutaVentas, "[]");
                creados += " ventas.json (vacío)";
            }

            string mensaje = creados.Length == 0
                ? "Se usaron los archivos de datos existentes."
                : "Primer inicio: se crearon" + creados + ".";
            return (true, mensaje);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return (false, "No se pudo preparar la carpeta de datos: " + ex.Message);
        }
    }

    public bool CargarProductos(out List<Producto> productos, out string error)
    {
        return Cargar(RutaProductos, out productos, out error);
    }

    public bool GuardarProductos(List<Producto> productos, out string error)
    {
        return Guardar(RutaProductos, productos, out error);
    }

    public bool CargarVentas(out List<Venta> ventas, out string error)
    {
        return Cargar(RutaVentas, out ventas, out error);
    }

    public bool GuardarVentas(List<Venta> ventas, out string error)
    {
        return Guardar(RutaVentas, ventas, out error);
    }

    /// <summary>
    /// Lee una lista JSON. Un archivo con [] es una colección vacía válida; un archivo vacío,
    /// con null o dañado se informa como error y NO se reemplaza por [].
    /// </summary>
    private static bool Cargar<T>(string ruta, out List<T> datos, out string error)
    {
        datos = new List<T>();
        string nombre = Path.GetFileName(ruta);

        try
        {
            if (!File.Exists(ruta))
            {
                error = $"No existe {nombre} en la carpeta de datos.";
                return false;
            }

            string json = File.ReadAllText(ruta);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = $"{nombre} está vacío (no contiene JSON). Se conservó sin cambios; revíselo.";
                return false;
            }

            List<T>? leidos = JsonSerializer.Deserialize<List<T>>(json, Opciones);
            if (leidos == null)
            {
                error = $"{nombre} contiene null en lugar de una lista. Se conservó sin cambios.";
                return false;
            }

            datos = leidos;
            error = "";
            return true;
        }
        catch (JsonException ex)
        {
            error = $"{nombre} está dañado y no se modificó ({ex.Message}).";
            return false;
        }
        catch (IOException ex)
        {
            error = $"No se pudo leer {nombre}: {ex.Message}";
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            error = $"Sin permiso para leer {nombre}: {ex.Message}";
            return false;
        }
    }

    private static bool Guardar<T>(string ruta, List<T> datos, out string error)
    {
        try
        {
            string json = JsonSerializer.Serialize(datos, Opciones);
            EscribirConReemplazo(ruta, json);
            error = "";
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            error = $"No se pudo guardar {Path.GetFileName(ruta)}: {ex.Message}";
            return false;
        }
    }

    private static void EscribirConReemplazo(string ruta, string contenido)
    {
        string temporal = ruta + ".tmp";
        File.WriteAllText(temporal, contenido);
        File.Move(temporal, ruta, true);
    }
}
