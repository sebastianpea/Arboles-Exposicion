namespace SimuladorArboles.Trees;

/// <summary>
/// Splay: cada acceso sube el nodo a la raíz (zig, zig-zig, zig-zag).
/// O(log n) amortizado; muy rápido cuando se repiten búsquedas de los mismos valores.
/// </summary>
public class SplayTree : TreeBase
{
    public override string Name => "Splay";
    public override TreeKind Kind => TreeKind.Splay;

    private void Splay(Node x)
    {
        while (x.Parent != null)
        {
            Node p = x.Parent, g = p.Parent;
            if (g == null)
            {
                if (x == p.Left) RotateRight(p); else RotateLeft(p);          // zig
            }
            else if (x == p.Left && p == g.Left) { RotateRight(g); RotateRight(p); }   // zig-zig
            else if (x == p.Right && p == g.Right) { RotateLeft(g); RotateLeft(p); }   // zig-zig
            else if (x == p.Right && p == g.Left) { RotateLeft(p); RotateRight(g); }   // zig-zag
            else { RotateRight(p); RotateLeft(g); }                                     // zig-zag
        }
    }

    public override bool Insert(int key)
    {
        Node n = InsertBst(key, out bool inserted);
        Splay(n);
        return inserted;
    }

    public override bool Contains(int key)
    {
        Node cur = Root, last = null;
        while (cur != null)
        {
            last = cur;
            Comparisons++;
            if (key == cur.Key) { Splay(cur); return true; }
            cur = key < cur.Key ? cur.Left : cur.Right;
        }
        if (last != null) Splay(last);
        return false;
    }

    public override bool Remove(int key)
    {
        Node z = FindNode(key);
        if (z == null) return false;
        Splay(z);
        DeleteNodeBst(z);
        return true;
    }
}