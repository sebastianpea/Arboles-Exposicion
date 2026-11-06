using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SimuladorArboles.Controls;

/// <summary>Gráfica de barras sencilla dibujada con GDI+ (no requiere paquetes extra).</summary>
public class BarChart : Control
{
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title { get; set; } = "";

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<(string Label, double Value, Color Color)> Items { get; set; } = new();

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DarkMode { get; set; }

    public BarChart()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    private static string Fmt(double v) =>
        v >= 1000 ? v.ToString("N0") : v >= 10 ? v.ToString("F1") : v.ToString("0.###");

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color bg = DarkMode ? Color.FromArgb(28, 28, 30) : Color.White;
        Color fg = DarkMode ? Color.Gainsboro : Color.FromArgb(40, 40, 40);
        Color gridColor = DarkMode ? Color.FromArgb(60, 60, 65) : Color.FromArgb(225, 225, 225);
        g.Clear(bg);

        using var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var font = new Font("Segoe UI", 9f);
        using var fgBrush = new SolidBrush(fg);
        g.DrawString(Title ?? "", titleFont, fgBrush, 10, 8);

        if (Items.Count == 0)
        {
            g.DrawString("Ejecuta un benchmark para ver la gráfica.", font, fgBrush, 10, 40);
            return;
        }

        var area = new RectangleF(80, 45, Width - 100, Height - 100);
        if (area.Width < 50 || area.Height < 30) return;

        double max = Items.Max(i => i.Value);
        if (max <= 0) max = 1;
        max *= 1.15;

        using var gridPen = new Pen(gridColor);
        for (int i = 0; i <= 5; i++)
        {
            float y = area.Bottom - area.Height * i / 5f;
            g.DrawLine(gridPen, area.Left, y, area.Right, y);
            string lbl = Fmt(max * i / 5);
            SizeF sz = g.MeasureString(lbl, font);
            g.DrawString(lbl, font, fgBrush, area.Left - sz.Width - 6, y - sz.Height / 2);
        }

        float slot = area.Width / Items.Count;
        float bw = Math.Min(120f, slot * 0.6f);
        using var center = new StringFormat { Alignment = StringAlignment.Center };
        for (int i = 0; i < Items.Count; i++)
        {
            var it = Items[i];
            float h = (float)(it.Value / max * area.Height);
            float x = area.Left + slot * i + (slot - bw) / 2;
            using (var b = new SolidBrush(it.Color)) g.FillRectangle(b, x, area.Bottom - h, bw, h);
            g.DrawString(Fmt(it.Value), font, fgBrush, new RectangleF(x - 20, area.Bottom - h - 18, bw + 40, 18), center);
            g.DrawString(it.Label, font, fgBrush, new RectangleF(area.Left + slot * i, area.Bottom + 6, slot, 40), center);
        }
    }
}