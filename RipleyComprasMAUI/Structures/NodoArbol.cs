namespace RipleyComprasMAUI.Structures;


// Nodo del árbol AVL: clave, valor, hijos izquierdo y derecho, y su altura.
public class NodoArbol<TClave, TValor>
{
    public TClave Clave { get; }
    public TValor Valor { get; set; }
    public NodoArbol<TClave, TValor>? Izquierdo { get; set; }
    public NodoArbol<TClave, TValor>? Derecho { get; set; }
    public int Altura { get; set; }

    public NodoArbol(TClave clave, TValor valor)
    {
        Clave = clave;
        Valor = valor;
        Altura = 1;
    }
}
