using System;
using System.Collections;
using System.Collections.Generic;

namespace RipleyComprasMAUI.Structures;


public class ListaEnlazada<T> : IEnumerable<T>
{
    private NodoSimple<T>? cabeza;
    private NodoSimple<T>? ultimo;

    public int Cantidad { get; private set; }
    public bool EstaVacia => cabeza == null;

    public void AgregarAlFinal(T dato)
    {
        NodoSimple<T> nuevo = new NodoSimple<T>(dato);
        if (ultimo == null)
        {
            cabeza = nuevo;              
            ultimo = nuevo;
        }
        else
        {
            ultimo.Siguiente = nuevo;   
            ultimo = nuevo;
        }
        Cantidad++;
    }

    public void AgregarAlInicio(T dato)
    {
        NodoSimple<T> nuevo = new NodoSimple<T>(dato);
        nuevo.Siguiente = cabeza;
        cabeza = nuevo;
        if (ultimo == null)
        {
            ultimo = nuevo;
        }
        Cantidad++;
    }


    public void InsertarEn(int posicion, T dato)
    {
        if (posicion <= 0 || cabeza == null)
        {
            AgregarAlInicio(dato);
            return;
        }
        if (posicion >= Cantidad)
        {
            AgregarAlFinal(dato);
            return;
        }

        NodoSimple<T> anterior = cabeza;
        for (int i = 0; i < posicion - 1; i++)
        {
            anterior = anterior.Siguiente!;           
        }
        NodoSimple<T> nuevo = new NodoSimple<T>(dato);
        nuevo.Siguiente = anterior.Siguiente;         
        anterior.Siguiente = nuevo;                    
        Cantidad++;
    }


    public T? Buscar(Func<T, bool> condicion)
    {
        NodoSimple<T>? actual = cabeza;
        while (actual != null)
        {
            if (condicion(actual.Dato))
            {
                return actual.Dato;
            }
            actual = actual.Siguiente;
        }
        return default;
    }

    public bool Eliminar(Func<T, bool> condicion, out T? eliminado, out int posicion)
    {
        NodoSimple<T>? anterior = null;
        NodoSimple<T>? actual = cabeza;
        posicion = 0;

        while (actual != null)
        {
            if (condicion(actual.Dato))
            {
                if (anterior == null)
                {
                    cabeza = actual.Siguiente;              
                }
                else
                {
                    anterior.Siguiente = actual.Siguiente;   
                }
                if (actual == ultimo)
                {
                    ultimo = anterior;                     
                }
                Cantidad--;
                eliminado = actual.Dato;
                return true;
            }
            anterior = actual;
            actual = actual.Siguiente;
            posicion++;
        }

        eliminado = default;
        posicion = -1;
        return false;
    }

    public void Limpiar()
    {
        cabeza = null;
        ultimo = null;
        Cantidad = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        NodoSimple<T>? actual = cabeza;
        while (actual != null)
        {
            yield return actual.Dato;
            actual = actual.Siguiente;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
