using System;
using System.Collections;
using System.Collections.Generic;

namespace RipleyComprasMAUI.Structures;


// Lista enlazada simple propia (carrito e historial). Guarda la cabeza y el último nodo.
public class ListaEnlazada<T> : IEnumerable<T>
{
    private NodoSimple<T>? cabeza;
    private NodoSimple<T>? ultimo;

    public int Cantidad { get; private set; }
    public bool EstaVacia => cabeza == null;

    // Agrega al final usando la referencia al último. O(1)
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

    // Agrega al inicio. O(1)
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


    // Inserta en una posición (lo usa Deshacer al reponer una prenda). O(n)
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


    // Recorre la lista hasta el primer elemento que cumple la condición. O(n)
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

    // Busca el elemento y lo saca reenlazando el nodo anterior. Buscar O(n), reenlazar O(1)
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

    // Vacía la lista. O(1)
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
