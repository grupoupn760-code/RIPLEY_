using System;

namespace RipleyComprasMAUI.Structures;

// Pila LIFO propia: guarda las acciones del carrito para poder deshacerlas.
public class PilaAcciones<T>
{
    private NodoSimple<T>? cima;

    public int Cantidad { get; private set; }
    public bool EstaVacia => cima == null;


    // Pone el dato en la cima. O(1)
    public void Apilar(T dato)
    {
        NodoSimple<T> nuevo = new NodoSimple<T>(dato);
        nuevo.Siguiente = cima;
        cima = nuevo;
        Cantidad++;
    }

    // Saca y devuelve la cima (la última acción). O(1)
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

    // Mira la cima sin sacarla. O(1)
    public T? VerCima()
    {
        return cima == null ? default : cima.Dato;
    }

    // Vacía la pila. O(1)
    public void Limpiar()
    {
        cima = null;
        Cantidad = 0;
    }
}
