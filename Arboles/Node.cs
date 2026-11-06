namespace SimuladorArboles.Trees;

/// <summary>
/// Nodo común para todos los árboles. Cada tipo usa los campos que necesita:
/// AVL usa Height, Rojo-Negro usa IsRed. Parent permite operaciones iterativas
/// (sin recursión), así no hay StackOverflow con archivos grandes u ordenados.
/// </summary>
public class Node
{
    public int Key;
    public Node Left, Right, Parent;
    public int Height = 1;   // AVL
    public bool IsRed;       // Rojo-Negro

    public Node(int key) => Key = key;
}