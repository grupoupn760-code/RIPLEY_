using System;

namespace RipleyComprasMAUI.Structures;

public class PilaAcciones<T>
{
    private NodoSimple<T>? cima;

    public int Cantidad { get; private set; }
    public bool EstaVacia => cima == null;


    public void Apilar(T dato)
    {
        NodoSimple<T> nuevo = new NodoSimple<T>(dato);
        nuevo.Siguiente = cima;
        cima = nuevo;
        Cantidad++;
    }

    public T Desapilar()
    {
        if (cima == null)
        {
            throw new InvalidOperationException("La pila está vacía.");
        }
        T dato = cima.Dato;
        cima = cima.Siguiente;         
        Cantidad--;
        return dato;
    }

    public T? VerCima()
    {
        return cima == null ? default : cima.Dato;
    }

    public void Limpiar()
    {
        cima = null;
        Cantidad = 0;
    }
}
