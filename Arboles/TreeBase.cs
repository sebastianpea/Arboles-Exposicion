namespace SimuladorArboles.Trees;

public enum TreeKind { Bst, Avl, RedBlack, Splay }
public enum TraversalKind { InOrden, PreOrden, PostOrden, PorNiveles }

/// <summary>
/// Base de todos los árboles: búsqueda, inserción/eliminación BST, rotaciones,
/// recorridos y métricas. Todo es iterativo para soportar cientos de miles de nodos.
/// </summary>
public abstract class TreeBase
{
    public Node Root { get; protected set; }
    public int Count { get; protected set; }
    public long Comparisons { get; protected set; }
    public long Rotations { get; protected set; }

    public abstract string Name { get; }
    public abstract TreeKind Kind { get; }

    public abstract bool Insert(int key);
    public abstract bool Remove(int key);
    public virtual bool Contains(int key) => FindNode(key) != null;

    public void Clear() { Root = null; Count = 0; ResetMetrics(); }
    public void ResetMetrics() { Comparisons = 0; Rotations = 0; }

    // ---------------- Búsqueda ----------------
    protected Node FindNode(int key)
    {
        Node cur = Root;
        while (cur != null)
        {
            Comparisons++;
            if (key == cur.Key) return cur;
            cur = key < cur.Key ? cur.Left : cur.Right;
        }
        return null;
    }

    /// <summary>Ruta desde la raíz hasta la clave (o hasta donde debería estar). No modifica el árbol.</summary>
    public List<Node> SearchPath(int key)
    {
        var path = new List<Node>();
        Node cur = Root;
        while (cur != null)
        {
            path.Add(cur);
            if (key == cur.Key) break;
            cur = key < cur.Key ? cur.Left : cur.Right;
        }
        return path;
    }

    // ---------------- Inserción / eliminación BST ----------------
    protected Node InsertBst(int key, out bool inserted)
    {
        Node parent = null, cur = Root;
        while (cur != null)
        {
            Comparisons++;
            if (key == cur.Key) { inserted = false; return cur; }
            parent = cur;
            cur = key < cur.Key ? cur.Left : cur.Right;
        }
        var n = new Node(key) { Parent = parent };
        if (parent == null) Root = n;
        else if (key < parent.Key) parent.Left = n;
        else parent.Right = n;
        Count++;
        inserted = true;
        return n;
    }

    protected void Transplant(Node u, Node v)
    {
        if (u.Parent == null) Root = v;
        else if (u == u.Parent.Left) u.Parent.Left = v;
        else u.Parent.Right = v;
        if (v != null) v.Parent = u.Parent;
    }

    /// <summary>Elimina z (con sucesor in-orden si tiene dos hijos). Devuelve el nodo desde donde rebalancear.</summary>
    protected Node DeleteNodeBst(Node z)
    {
        Node fix;
        if (z.Left == null) { fix = z.Parent; Transplant(z, z.Right); }
        else if (z.Right == null) { fix = z.Parent; Transplant(z, z.Left); }
        else
        {
            Node y = MinNode(z.Right);
            if (y.Parent != z)
            {
                fix = y.Parent;
                Transplant(y, y.Right);
                y.Right = z.Right;
                y.Right.Parent = y;
            }
            else fix = y;
            Transplant(z, y);
            y.Left = z.Left;
            y.Left.Parent = y;
        }
        Count--;
        return fix;
    }

    protected static Node MinNode(Node n) { while (n.Left != null) n = n.Left; return n; }
    protected static Node MaxNode(Node n) { while (n.Right != null) n = n.Right; return n; }
    public int? MinKey() => Root == null ? null : MinNode(Root).Key;
    public int? MaxKey() => Root == null ? null : MaxNode(Root).Key;

    // ---------------- Rotaciones ----------------
    protected void RotateLeft(Node x)
    {
        Node y = x.Right;
        x.Right = y.Left;
        if (y.Left != null) y.Left.Parent = x;
        y.Parent = x.Parent;
        if (x.Parent == null) Root = y;
        else if (x == x.Parent.Left) x.Parent.Left = y;
        else x.Parent.Right = y;
        y.Left = x;
        x.Parent = y;
        Rotations++;
    }

    protected void RotateRight(Node x)
    {
        Node y = x.Left;
        x.Left = y.Right;
        if (y.Right != null) y.Right.Parent = x;
        y.Parent = x.Parent;
        if (x.Parent == null) Root = y;
        else if (x == x.Parent.Right) x.Parent.Right = y;
        else x.Parent.Left = y;
        y.Right = x;
        x.Parent = y;
        Rotations++;
    }

    // ---------------- Altura ----------------
    public int Height()
    {
        if (Root == null) return 0;
        int h = 0;
        var q = new Queue<Node>();
        q.Enqueue(Root);
        while (q.Count > 0)
        {
            h++;
            for (int i = q.Count; i > 0; i--)
            {
                var n = q.Dequeue();
                if (n.Left != null) q.Enqueue(n.Left);
                if (n.Right != null) q.Enqueue(n.Right);
            }
        }
        return h;
    }

    public static int IdealHeight(int n) => n <= 0 ? 0 : (int)Math.Floor(Math.Log2(n)) + 1;

    // ---------------- Recorridos (iterativos) ----------------
    public List<Node> Traverse(TraversalKind kind) => kind switch
    {
        TraversalKind.InOrden => InOrder(),
        TraversalKind.PreOrden => PreOrder(),
        TraversalKind.PostOrden => PostOrder(),
        _ => LevelOrder()
    };

    public List<Node> InOrder()
    {
        var r = new List<Node>(Count);
        var st = new Stack<Node>();
        Node cur = Root;
        while (cur != null || st.Count > 0)
        {
            while (cur != null) { st.Push(cur); cur = cur.Left; }
            cur = st.Pop();
            r.Add(cur);
            cur = cur.Right;
        }
        return r;
    }

    public List<Node> PreOrder()
    {
        var r = new List<Node>(Count);
        if (Root == null) return r;
        var st = new Stack<Node>();
        st.Push(Root);
        while (st.Count > 0)
        {
            var n = st.Pop();
            r.Add(n);
            if (n.Right != null) st.Push(n.Right);
            if (n.Left != null) st.Push(n.Left);
        }
        return r;
    }

    public List<Node> PostOrder()
    {
        var r = new List<Node>(Count);
        if (Root == null) return r;
        var s1 = new Stack<Node>();
        var s2 = new Stack<Node>();
        s1.Push(Root);
        while (s1.Count > 0)
        {
            var n = s1.Pop();
            s2.Push(n);
            if (n.Left != null) s1.Push(n.Left);
            if (n.Right != null) s1.Push(n.Right);
        }
        while (s2.Count > 0) r.Add(s2.Pop());
        return r;
    }

    public List<Node> LevelOrder()
    {
        var r = new List<Node>(Count);
        if (Root == null) return r;
        var q = new Queue<Node>();
        q.Enqueue(Root);
        while (q.Count > 0)
        {
            var n = q.Dequeue();
            r.Add(n);
            if (n.Left != null) q.Enqueue(n.Left);
            if (n.Right != null) q.Enqueue(n.Right);
        }
        return r;
    }
}

public static class TreeFactory
{
    public static readonly string[] Names =
    {
        "BST (Binario de Búsqueda)",
        "AVL (auto-balanceado)",
        "Rojo-Negro",
        "Splay (auto-ajustable)"
    };

    public static TreeBase Create(TreeKind kind) => kind switch
    {
        TreeKind.Bst => new BstTree(),
        TreeKind.Avl => new AvlTree(),
        TreeKind.RedBlack => new RedBlackTree(),
        TreeKind.Splay => new SplayTree(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}