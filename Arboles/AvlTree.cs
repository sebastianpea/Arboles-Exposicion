namespace SimuladorArboles.Trees;

/// <summary>AVL: |altura(izq) - altura(der)| ≤ 1 en todo nodo. Garantiza O(log n).</summary>
public class AvlTree : TreeBase
{
    public override string Name => "AVL";
    public override TreeKind Kind => TreeKind.Avl;

    private static int H(Node n) => n?.Height ?? 0;
    private static void Update(Node n) => n.Height = 1 + Math.Max(H(n.Left), H(n.Right));

    public override bool Insert(int key)
    {
        Node n = InsertBst(key, out bool inserted);
        if (inserted) RebalanceUp(n.Parent);
        return inserted;
    }

    public override bool Remove(int key)
    {
        Node z = FindNode(key);
        if (z == null) return false;
        RebalanceUp(DeleteNodeBst(z));
        return true;
    }

    /// <summary>Sube hacia la raíz actualizando alturas y aplicando rotaciones LL, LR, RR, RL.</summary>
    private void RebalanceUp(Node n)
    {
        while (n != null)
        {
            Update(n);
            int bf = H(n.Left) - H(n.Right);

            if (bf > 1)
            {
                Node a = n.Left;
                if (H(a.Left) < H(a.Right))          // caso LR
                {
                    RotateLeft(a);
                    Update(a);
                    Update(a.Parent);
                }
                RotateRight(n);                      // caso LL
                Update(n);
                Update(n.Parent);
            }
            else if (bf < -1)
            {
                Node a = n.Right;
                if (H(a.Right) < H(a.Left))          // caso RL
                {
                    RotateRight(a);
                    Update(a);
                    Update(a.Parent);
                }
                RotateLeft(n);                       // caso RR
                Update(n);
                Update(n.Parent);
            }
            n = n.Parent;
        }
    }
}