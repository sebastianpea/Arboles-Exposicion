using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SimuladorArboles.Trees;

namespace SimuladorArboles.Controls;

/// <summary>Panel con scroll que dibuja el árbol (posición X = orden in-orden, Y = profundidad).</summary>
public class TreeCanvas : Panel
{
    public const int MaxDrawNodes = 1000;
    private const float Pad = 30f;
    private const float MaxContentWidth = 30000f;   // evita límites de scroll de Windows

    private readonly Dictionary<Node, PointF> _pos = new();
    private float _contentWidth;
    private float _d = 36f;   // diámetro real usado al dibujar

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TreeBase Tree { get; set; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float Zoom { get; set; } = 1f;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DarkMode { get; set; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HashSet<Node> PathNodes { get; set; } = new();

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HashSet<Node> VisitedNodes { get; } = new();

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Node FoundNode { get; set; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Node CurrentNode { get; set; }

    public event Action<int> NodeClicked;

    public TreeCanvas()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
    }

    public void RecalculateLayout()
    {
        _pos.Clear();
        _contentWidth = 0;
        if (Tree?.Root == null || Tree.Count > MaxDrawNodes)
        {
            AutoScrollMinSize = Size.Empty;
            Invalidate();
            return;
        }

        float hs = Math.Min(36f * Zoom * 1.1f, (MaxContentWidth - 2 * Pad) / Tree.Count);
        _d = hs / 1.1f;
        float vs = _d * 1.8f;

        var st = new Stack<(Node node, int depth)>();
        Node cur = Tree.Root;
        int depth = 0, index = 0, maxDepth = 0;
        while (cur != null || st.Count > 0)
        {
            while (cur != null) { st.Push((cur, depth)); cur = cur.Left; depth++; }
            var (n, dp) = st.Pop();
            _pos[n] = new PointF(Pad + index * hs + _d / 2, Pad + dp * vs + _d / 2);
            index++;
            maxDepth = Math.Max(maxDepth, dp);
            cur = n.Right;
            depth = dp + 1;
        }

        _contentWidth = Pad * 2 + index * hs;
        AutoScrollMinSize = new Size((int)_contentWidth, (int)(Pad * 2 + (maxDepth + 1) * vs));
        Invalidate();
    }

    private float OffsetX => Math.Max(0, (ClientSize.Width - _contentWidth) / 2f);

    public void EnsureVisible(Node n)
    {
        if (n == null || !_pos.TryGetValue(n, out var p)) return;
        float x = p.X + OffsetX, y = p.Y;
        int sx = -AutoScrollPosition.X, sy = -AutoScrollPosition.Y;
        if (x < sx + _d || x > sx + ClientSize.Width - _d || y < sy + _d || y > sy + ClientSize.Height - _d)
        {
            AutoScrollPosition = new Point(Math.Max(0, (int)(x - ClientSize.Width / 2f)),
                                           Math.Max(0, (int)(y - ClientSize.Height / 2f)));
            Invalidate();
        }
    }

    protected override void OnScroll(ScrollEventArgs se) { base.OnScroll(se); Invalidate(); }
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        float x = e.X - AutoScrollPosition.X - OffsetX, y = e.Y - AutoScrollPosition.Y, r = _d / 2;
        foreach (var (n, p) in _pos)
        {
            if ((p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y) <= r * r)
            {
                NodeClicked?.Invoke(n.Key);
                return;
            }
        }
    }

    private static int H(Node n) => n?.Height ?? 0;

    private (Color fill, Color text, Color border) StyleFor(Node n, bool isRb)
    {
        if (n == FoundNode) return (Color.FromArgb(46, 160, 67), Color.White, Color.FromArgb(20, 100, 40));
        if (n == CurrentNode) return (Color.FromArgb(255, 140, 0), Color.White, Color.FromArgb(180, 90, 0));
        if (PathNodes.Contains(n)) return (Color.FromArgb(255, 214, 10), Color.Black, Color.DarkGoldenrod);
        if (VisitedNodes.Contains(n)) return (Color.FromArgb(120, 190, 255), Color.Black, Color.FromArgb(60, 120, 200));
        if (isRb)
            return n.IsRed
                ? (Color.FromArgb(214, 40, 57), Color.White, Color.FromArgb(150, 20, 30))
                : (Color.FromArgb(33, 33, 33), Color.White, DarkMode ? Color.Gray : Color.Black);
        return DarkMode
            ? (Color.FromArgb(58, 90, 140), Color.White, Color.FromArgb(120, 160, 220))
            : (Color.FromArgb(225, 236, 250), Color.FromArgb(20, 40, 80), Color.FromArgb(70, 110, 170));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        Color bg = DarkMode ? Color.FromArgb(28, 28, 30) : Color.White;
        Color fg = DarkMode ? Color.Gainsboro : Color.FromArgb(50, 50, 50);
        g.Clear(bg);

        using var msgFont = new Font("Segoe UI", 11f);
        using var fgBrush = new SolidBrush(fg);

        if (Tree?.Root == null)
        {
            g.DrawString("El árbol está vacío. Inserta valores o carga un archivo .txt para comenzar.", msgFont, fgBrush, 20, 20);
            return;
        }
        if (Tree.Count > MaxDrawNodes)
        {
            g.DrawString($"El árbol tiene {Tree.Count:N0} nodos (límite de dibujo: {MaxDrawNodes:N0}).\n" +
                         "Las operaciones, estadísticas y recorridos siguen funcionando normalmente.", msgFont, fgBrush, 20, 20);
            return;
        }

        g.TranslateTransform(AutoScrollPosition.X + OffsetX, AutoScrollPosition.Y);

        // Aristas
        using var edgePen = new Pen(DarkMode ? Color.FromArgb(110, 110, 115) : Color.FromArgb(170, 170, 175), 1.6f);
        using var pathPen = new Pen(Color.Goldenrod, 3f);
        foreach (var (n, p) in _pos)
        {
            foreach (Node child in new[] { n.Left, n.Right })
            {
                if (child == null || !_pos.TryGetValue(child, out var pc)) continue;
                bool onPath = PathNodes.Contains(n) && PathNodes.Contains(child);
                g.DrawLine(onPath ? pathPen : edgePen, p, pc);
            }
        }

        // Nodos
        float d = _d, r = d / 2;
        bool isRb = Tree.Kind == TreeKind.RedBlack, isAvl = Tree.Kind == TreeKind.Avl;
        var fonts = new Dictionary<int, Font>();
        using var smallFont = new Font("Segoe UI", Math.Max(6f, d * 0.22f));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        try
        {
            foreach (var (n, p) in _pos)
            {
                var rect = new RectangleF(p.X - r, p.Y - r, d, d);
                var (fill, text, border) = StyleFor(n, isRb);
                using (var b = new SolidBrush(fill)) g.FillEllipse(b, rect);
                using (var pen = new Pen(border, n == CurrentNode || n == FoundNode ? 3f : 1.5f)) g.DrawEllipse(pen, rect);

                string s = n.Key.ToString();
                int len = Math.Max(3, s.Length);
                if (!fonts.TryGetValue(len, out Font f))
                {
                    f = new Font("Segoe UI", Math.Max(5f, d * 0.30f * 3f / len), FontStyle.Bold);
                    fonts[len] = f;
                }
                using (var tb = new SolidBrush(text)) g.DrawString(s, f, tb, rect, sf);

                if (isAvl)   // factor de equilibrio
                {
                    int bf = H(n.Left) - H(n.Right);
                    g.DrawString(bf > 0 ? $"+{bf}" : bf.ToString(), smallFont, fgBrush, p.X + r * 0.7f, p.Y - r * 1.45f);
                }
            }
        }
        finally
        {
            foreach (Font f in fonts.Values) f.Dispose();
        }
    }
}