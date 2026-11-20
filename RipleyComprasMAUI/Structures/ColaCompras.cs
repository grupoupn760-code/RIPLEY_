using System;
using System.Collections;
using System.Collections.Generic;

namespace RipleyComprasMAUI.Structures;


public class ColaCompras<T> : IEnumerable<T>
{
    private NodoSimple<T>? frente;
    private NodoSimple<T>? final;

    public int Cantidad { get; private set; }
    public bool EstaVacia => frente == null;


    public void Encolar(T dato)
    {
        NodoSimple<T> nuevo = new NodoSimple<T>(dato);
        if (final == null)
        {
            frente = nuevo;             
            final = nuevo;
        }
        else
        {
            final.Siguiente = nuevo;     
            final = nuevo;
        }
        Cantidad++;
    }

    public T Desencolar()
    {
        if (frente == null)
        {
            throw new InvalidOperationException("La cola está vacía.");
        }
        T dato = frente.Dato;
        frente = frente.Siguiente;
        if (frente == null)
        {
            final = null;               
        }
        Cantidad--;
        return dato;
    }


    public T? VerFrente()
    {
        return frente == null ? default : frente.Dato;
    }

    public IEnumerator<T> GetEnumerator()
    {
        NodoSimple<T>? actual = frente;
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
