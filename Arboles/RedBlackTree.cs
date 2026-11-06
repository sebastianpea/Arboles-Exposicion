namespace SimuladorArboles.Trees;

/// <summary>
/// Rojo-Negro (versión CLRS con hojas null = negras). Altura ≤ 2·log2(n+1).
/// Hace menos rotaciones que AVL al insertar/eliminar, pero queda un poco más alto.
/// </summary>
public class RedBlackTree : TreeBase
{
    public override string Name => "Rojo-Negro";
    public override TreeKind Kind => TreeKind.RedBlack;

    private static bool IsRed(Node n) => n != null && n.IsRed;

    public override bool Insert(int key)
    {
        Node z = InsertBst(key, out bool inserted);
        if (!inserted) return false;
        z.IsRed = true;

        while (IsRed(z.Parent))
        {
            Node p = z.Parent, g = p.Parent;
            if (p == g.Left)
            {
                Node u = g.Right;
                if (IsRed(u)) { p.IsRed = false; u.IsRed = false; g.IsRed = true; z = g; }
                else
                {
                    if (z == p.Right) { z = p; RotateLeft(z); p = z.Parent; }
                    p.IsRed = false; g.IsRed = true; RotateRight(g);
                }
            }
            else
            {
                Node u = g.Left;
                if (IsRed(u)) { p.IsRed = false; u.IsRed = false; g.IsRed = true; z = g; }
                else
                {
                    if (z == p.Left) { z = p; RotateRight(z); p = z.Parent; }
                    p.IsRed = false; g.IsRed = true; RotateLeft(g);
                }
            }
        }
        Root.IsRed = false;
        return true;
    }

    public override bool Remove(int key)
    {
        Node z = FindNode(key);
        if (z == null) return false;

        Node y = z, x, xParent;
        bool yWasRed = y.IsRed;

        if (z.Left == null) { x = z.Right; xParent = z.Parent; Transplant(z, z.Right); }
        else if (z.Right == null) { x = z.Left; xParent = z.Parent; Transplant(z, z.Left); }
        else
        {
            y = MinNode(z.Right);
            yWasRed = y.IsRed;
            x = y.Right;
            if (y.Parent == z) xParent = y;
            else
            {
                xParent = y.Parent;
                Transplant(y, y.Right);
                y.Right = z.Right;
                y.Right.Parent = y;
            }
            Transplant(z, y);
            y.Left = z.Left;
            y.Left.Parent = y;
            y.IsRed = z.IsRed;
        }
        Count--;
        if (!yWasRed) DeleteFixup(x, xParent);
        return true;
    }

    private void DeleteFixup(Node x, Node parent)
    {
        while (x != Root && !IsRed(x))
        {
            if (x == parent.Left)
            {
                Node w = parent.Right;
                if (IsRed(w)) { w.IsRed = false; parent.IsRed = true; RotateLeft(parent); w = parent.Right; }
                if (!IsRed(w.Left) && !IsRed(w.Right)) { w.IsRed = true; x = parent; parent = x.Parent; }
                else
                {
                    if (!IsRed(w.Right)) { w.Left.IsRed = false; w.IsRed = true; RotateRight(w); w = parent.Right; }
                    w.IsRed = parent.IsRed; parent.IsRed = false;
                    if (w.Right != null) w.Right.IsRed = false;
                    RotateLeft(parent);
                    x = Root; parent = null;
                }
            }
            else
            {
                Node w = parent.Left;
                if (IsRed(w)) { w.IsRed = false; parent.IsRed = true; RotateRight(parent); w = parent.Left; }
                if (!IsRed(w.Left) && !IsRed(w.Right)) { w.IsRed = true; x = parent; parent = x.Parent; }
                else
                {
                    if (!IsRed(w.Left)) { w.Right.IsRed = false; w.IsRed = true; RotateLeft(w); w = parent.Left; }
                    w.IsRed = parent.IsRed; parent.IsRed = false;
                    if (w.Left != null) w.Left.IsRed = false;
                    RotateRight(parent);
                    x = Root; parent = null;
                }
            }
        }
        if (x != null) x.IsRed = false;
    }
}