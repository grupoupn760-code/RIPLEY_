namespace RipleyComprasMAUI.Structures;


// Nodo de la lista, la pila y la cola: guarda un dato y la referencia al siguiente.
public class NodoSimple<T>
{
    public T Dato { get; set; }
    public NodoSimple<T>? Siguiente { get; set; }

    public NodoSimple(T dato)
    {
        Dato = dato;
        Siguiente = null;
    }
}
