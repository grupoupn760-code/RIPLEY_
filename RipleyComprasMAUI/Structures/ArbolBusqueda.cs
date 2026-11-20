using System;
using System.Collections.Generic;

namespace RipleyComprasMAUI.Structures;

public class ArbolBusqueda<TClave, TValor>
{
    private readonly IComparer<TClave> comparador;
    private NodoArbol<TClave, TValor>? raiz;

    public ArbolBusqueda(IComparer<TClave>? comparador = null)
    {

        this.comparador = comparador ?? Comparer<TClave>.Default;
    }

    public int Cantidad { get; private set; }
    public int Altura => AlturaDe(raiz);
    public int Rotaciones { get; private set; }             
    public int UltimasComparaciones { get; private set; }    
    public bool EstaVacio => raiz == null;
    public TClave? ClaveRaiz => raiz == null ? default : raiz.Clave;

    public bool Insertar(TClave clave, TValor valor)
    {
        bool insertado = false;
        raiz = Insertar(raiz, clave, valor, ref insertado);
        if (insertado)
        {
            Cantidad++;
        }
        return insertado;
    }

    private NodoArbol<TClave, TValor> Insertar(NodoArbol<TClave, TValor>? nodo, TClave clave, TValor valor, ref bool insertado)
    {
        if (nodo == null)
        {
            insertado = true;
            return new NodoArbol<TClave, TValor>(clave, valor);      
        }

        int comparacion = comparador.Compare(clave, nodo.Clave);
        if (comparacion < 0)
        {
            nodo.Izquierdo = Insertar(nodo.Izquierdo, clave, valor, ref insertado);
        }
        else if (comparacion > 0)
        {
            nodo.Derecho = Insertar(nodo.Derecho, clave, valor, ref insertado);
        }
        else
        {
            return nodo;                                             
        }

        return Balancear(nodo);                                      
    }

    public bool Buscar(TClave clave, out TValor? valor)
    {
        UltimasComparaciones = 0;
        NodoArbol<TClave, TValor>? actual = raiz;

        while (actual != null)
        {
            UltimasComparaciones++;
            int comparacion = comparador.Compare(clave, actual.Clave);
            if (comparacion == 0)
            {
                valor = actual.Valor;
                return true;
            }
            actual = comparacion < 0 ? actual.Izquierdo : actual.Derecho;
        }

        valor = default;
        return false;
    }

    public ListaEnlazada<TValor> RecorrerInOrden()
    {
        ListaEnlazada<TValor> resultado = new ListaEnlazada<TValor>();
        RecorrerInOrden(raiz, resultado);
        return resultado;
    }

    private static void RecorrerInOrden(NodoArbol<TClave, TValor>? nodo, ListaEnlazada<TValor> resultado)
    {
        if (nodo == null)
        {
            return;
        }
        RecorrerInOrden(nodo.Izquierdo, resultado);
        resultado.AgregarAlFinal(nodo.Valor);
        RecorrerInOrden(nodo.Derecho, resultado);
    }


    private static int AlturaDe(NodoArbol<TClave, TValor>? nodo)
    {
        return nodo == null ? 0 : nodo.Altura;
    }

    private static void ActualizarAltura(NodoArbol<TClave, TValor> nodo)
    {
        nodo.Altura = 1 + Math.Max(AlturaDe(nodo.Izquierdo), AlturaDe(nodo.Derecho));
    }


    private static int FactorEquilibrio(NodoArbol<TClave, TValor> nodo)
    {
        return AlturaDe(nodo.Izquierdo) - AlturaDe(nodo.Derecho);
    }

    private NodoArbol<TClave, TValor> Balancear(NodoArbol<TClave, TValor> nodo)
    {
        ActualizarAltura(nodo);
        int factor = FactorEquilibrio(nodo);

        if (factor > 1)                                   
        {
            if (FactorEquilibrio(nodo.Izquierdo!) < 0)
            {
                nodo.Izquierdo = RotarIzquierda(nodo.Izquierdo!);   
            }
            return RotarDerecha(nodo);
        }

        if (factor < -1)                                   
        {
            if (FactorEquilibrio(nodo.Derecho!) > 0)
            {
                nodo.Derecho = RotarDerecha(nodo.Derecho!);        
            }
            return RotarIzquierda(nodo);
        }

        return nodo;                                      
    }

    private NodoArbol<TClave, TValor> RotarDerecha(NodoArbol<TClave, TValor> nodo)
    {
        NodoArbol<TClave, TValor> hijo = nodo.Izquierdo!;
        nodo.Izquierdo = hijo.Derecho;
        hijo.Derecho = nodo;
        ActualizarAltura(nodo);          
        ActualizarAltura(hijo);
        Rotaciones++;
        return hijo;                   
    }

    private NodoArbol<TClave, TValor> RotarIzquierda(NodoArbol<TClave, TValor> nodo)
    {
        NodoArbol<TClave, TValor> hijo = nodo.Derecho!;
        nodo.Derecho = hijo.Izquierdo;
        hijo.Izquierdo = nodo;
        ActualizarAltura(nodo);
        ActualizarAltura(hijo);
        Rotaciones++;
        return hijo;
    }
}
