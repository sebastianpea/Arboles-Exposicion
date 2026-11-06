namespace SimuladorArboles.Trees;

/// <summary>Árbol Binario de Búsqueda sin balanceo. Con datos ordenados degenera en lista: O(n).</summary>
public class BstTree : TreeBase
{
    public override string Name => "BST";
    public override TreeKind Kind => TreeKind.Bst;

    public override bool Insert(int key)
    {
        InsertBst(key, out bool inserted);
        return inserted;
    }

    public override bool Remove(int key)
    {
        Node z = FindNode(key);
        if (z == null) return false;
        DeleteNodeBst(z);
        return true;
    }
}