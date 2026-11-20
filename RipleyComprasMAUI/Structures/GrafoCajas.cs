using System;
using System.Globalization;

namespace RipleyComprasMAUI.Structures;

public class GrafoCajas
{
    public class Enlace
    {
        public int Destino { get; }         
        public double Peso { get; set; }    
        public bool Activo { get; set; }

        public Enlace(int destino, double peso)
        {
            Destino = destino;
            Peso = peso;
            Activo = true;
        }
    }

    private readonly string[] nombres;                       
    private readonly ListaEnlazada<Enlace>[] adyacencia;    
    private int cantidad;

    public GrafoCajas(int capacidad)
    {
        nombres = new string[capacidad];
        adyacencia = new ListaEnlazada<Enlace>[capacidad];
    }

    public int CantidadCajas => cantidad;

    public string NombreCaja(int indice)
    {
        return EsIndiceValido(indice) ? nombres[indice] : "(caja inexistente)";
    }

    public int AgregarCaja(string nombre)
    {
        if (cantidad == nombres.Length)
        {
            throw new InvalidOperationException("Se alcanzó la capacidad del grafo de ejemplo.");
        }
        nombres[cantidad] = nombre;
        adyacencia[cantidad] = new ListaEnlazada<Enlace>();
        cantidad++;
        return cantidad - 1;
    }


    public bool Conectar(int a, int b, double peso)
    {
        if (!EsIndiceValido(a) || !EsIndiceValido(b) || a == b || peso <= 0 || BuscarEnlace(a, b) != null)
        {
            return false;
        }
        adyacencia[a].AgregarAlFinal(new Enlace(b, peso));
        adyacencia[b].AgregarAlFinal(new Enlace(a, peso));
        return true;
    }

    public bool CambiarEstadoEnlace(int a, int b, bool activo, out string mensaje)
    {
        Enlace? ida = BuscarEnlace(a, b);
        Enlace? vuelta = BuscarEnlace(b, a);
        if (ida == null || vuelta == null)
        {
            mensaje = $"No hay enlace directo entre {NombreCaja(a)} y {NombreCaja(b)}.";
            return false;
        }
        ida.Activo = activo;
        vuelta.Activo = activo;
        mensaje = $"Enlace {NombreCaja(a)} - {NombreCaja(b)}: {(activo ? "activo" : "caído")}.";
        return true;
    }

    public bool CambiarPeso(int a, int b, double peso, out string mensaje)
    {
        Enlace? ida = BuscarEnlace(a, b);
        Enlace? vuelta = BuscarEnlace(b, a);
        if (ida == null || vuelta == null)
        {
            mensaje = $"No hay enlace directo entre {NombreCaja(a)} y {NombreCaja(b)}.";
            return false;
        }
        if (!double.IsFinite(peso) || peso <= 0)
        {
            mensaje = "El peso debe ser un número positivo.";
            return false;
        }
        ida.Peso = peso;
        vuelta.Peso = peso;
        mensaje = $"Nuevo peso del enlace {NombreCaja(a)} - {NombreCaja(b)}: {Formatear(peso)} ms.";
        return true;
    }


    public bool BuscarRutaMinima(int origen, int destino, out ListaEnlazada<int> ruta, out double distanciaTotal)
    {
        ruta = new ListaEnlazada<int>();
        distanciaTotal = 0;
        if (!EsIndiceValido(origen) || !EsIndiceValido(destino))
        {
            return false;
        }

        double[] distancia = new double[cantidad];
        int[] previo = new int[cantidad];
        bool[] visitado = new bool[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            distancia[i] = double.PositiveInfinity;
            previo[i] = -1;
        }
        distancia[origen] = 0;

        for (int paso = 0; paso < cantidad; paso++)
        {
            int actual = -1;
            for (int i = 0; i < cantidad; i++)      
            {
                if (!visitado[i] && (actual == -1 || distancia[i] < distancia[actual]))
                {
                    actual = i;
                }
            }
            if (actual == -1 || double.IsPositiveInfinity(distancia[actual]))
            {
                break;                              
            }
            visitado[actual] = true;
            if (actual == destino)
            {
                break;                               
            }

            foreach (Enlace enlace in adyacencia[actual])
            {
                if (!enlace.Activo || visitado[enlace.Destino])
                {
                    continue;                       
                }
                double candidata = distancia[actual] + enlace.Peso;
                if (candidata < distancia[enlace.Destino])
                {
                    distancia[enlace.Destino] = candidata;  
                    previo[enlace.Destino] = actual;
                }
            }
        }

        if (double.IsPositiveInfinity(distancia[destino]))
        {
            return false;                         
        }

        for (int caja = destino; caja != -1; caja = previo[caja])
        {
            ruta.AgregarAlInicio(caja);             
        }
        distanciaTotal = distancia[destino];
        return true;
    }

    public string DescribirRuta(ListaEnlazada<int> ruta)
    {
        string texto = "";
        foreach (int caja in ruta)
        {
            texto += (texto.Length == 0 ? "" : " → ") + NombreCaja(caja);
        }
        return texto;
    }


    public ListaEnlazada<string> DescribirEnlaces()
    {
        ListaEnlazada<string> lineas = new ListaEnlazada<string>();
        for (int a = 0; a < cantidad; a++)
        {
            foreach (Enlace enlace in adyacencia[a])
            {
                if (a < enlace.Destino)
                {
                    lineas.AgregarAlFinal($"{nombres[a]} ↔ {nombres[enlace.Destino]} · {Formatear(enlace.Peso)} ms · {(enlace.Activo ? "activo" : "CAÍDO")}");
                }
            }
        }
        return lineas;
    }

    public static GrafoCajas CrearEjemplo()
    {
        GrafoCajas grafo = new GrafoCajas(6);
        int caja511 = grafo.AgregarCaja("Caja 511");
        int caja512 = grafo.AgregarCaja("Caja 512");
        int caja513 = grafo.AgregarCaja("Caja 513");
        int servidor = grafo.AgregarCaja("Servidor de tienda");
        int almacen = grafo.AgregarCaja("Almacén");
        int central = grafo.AgregarCaja("Central (simulada)");

        grafo.Conectar(caja511, caja512, 4);
        grafo.Conectar(caja511, caja513, 9);
        grafo.Conectar(caja512, caja513, 3);
        grafo.Conectar(caja512, servidor, 10);
        grafo.Conectar(caja513, servidor, 4);
        grafo.Conectar(caja513, almacen, 7);
        grafo.Conectar(servidor, central, 15);
        grafo.Conectar(almacen, central, 20);
        return grafo;
    }

    private Enlace? BuscarEnlace(int a, int b)
    {
        if (!EsIndiceValido(a) || !EsIndiceValido(b))
        {
            return null;
        }
        return adyacencia[a].Buscar(enlace => enlace.Destino == b);   
    }

    private bool EsIndiceValido(int indice)
    {
        return indice >= 0 && indice < cantidad;
    }

    private static string Formatear(double valor)
    {
        return valor.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
