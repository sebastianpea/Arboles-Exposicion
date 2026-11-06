using System.Diagnostics;
using System.Globalization;
using System.Text;
using SimuladorArboles.Bench;
using SimuladorArboles.Controls;
using SimuladorArboles.Trees;

namespace Arboles
{
    public partial class MainForm : Form
    {
        // ---------------- Estado ----------------
        private TreeBase _tree = new AvlTree();
        private readonly List<int> _keys = new();          // claves actuales en orden de inserción
        private List<Node> _animList;
        private int _animIndex;
        private int[] _benchFileData;
        private List<BenchmarkResult> _results = new();
        private CancellationTokenSource _cts;
        private readonly System.Windows.Forms.Timer animTimer = new();

        // ---------------- Controles generales ----------------
        private TabControl tabs;
        private Label lblStatus;
        private CheckBox chkOscuro;

        // ---------------- Visualizador ----------------
        private ComboBox cboTipo;
        private TextBox txtValor;
        private NumericUpDown nudAleatorios;
        private TrackBar trkZoom, trkVelocidad;
        private TreeCanvas canvas;
        private CheckBox chkAnimar;
        private TextBox txtRecorrido;
        private Label lblStats;

        // ---------------- Benchmark ----------------
        private RadioButton rbArchivo, rbGenerar;
        private Label lblArchivoBench, lblBenchInfo;
        private NumericUpDown nudN, nudMin, nudMax, nudReps;
        private ComboBox cboDistribucion, cboMetrica;
        private CheckedListBox clbArboles;
        private Button btnEjecutar, btnCancelar, btnExportar;
        private ProgressBar progress;
        private DataGridView grid;
        private BarChart chart;
        private SplitContainer splitBench;

        private static readonly string[] Metricas =
        {
        "Tiempo de inserción (ms)", "Tiempo de búsqueda (ms)", "Tiempo de recorrido InOrden (ms)",
        "Tiempo de eliminación (ms)", "Altura final", "Comparaciones en inserción",
        "Comparaciones en búsqueda", "Rotaciones en inserción"
    };
        public MainForm()
        {
            Text = "Simulador de Árboles + Benchmark";
            Size = new Size(1350, 860);
            MinimumSize = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildVisualizerTab());
            tabs.TabPages.Add(BuildBenchmarkTab());

            var header = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 0, 12, 0) };
            var title = new Label
            {
                Text = "Simulador de Árboles — BST · AVL · Rojo-Negro · Splay",
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 700,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold)
            };
            chkOscuro = new CheckBox { Text = "Modo oscuro", Dock = DockStyle.Right, Width = 120 };
            chkOscuro.CheckedChanged += (s, e) => ApplyTheme(chkOscuro.Checked);
            header.Controls.Add(title);
            header.Controls.Add(chkOscuro);

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Text = "Listo."
            };

            Controls.Add(tabs);
            Controls.Add(header);
            Controls.Add(lblStatus);

            animTimer.Tick += AnimTimer_Tick;
            FormClosing += (s, e) => { _cts?.Cancel(); animTimer.Stop(); };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try { splitBench.SplitterDistance = Math.Max(150, splitBench.Height / 2); } catch { /* tamaño aún no válido */ }
            RefreshView();
            UpdateChart();
        }

        // =====================================================================
        //  CONSTRUCCIÓN DE LA INTERFAZ
        // =====================================================================
        private static Label L(string t) => new() { Text = t, AutoSize = true, Margin = new Padding(6, 8, 2, 3) };
        private static Label Titulo(string t) => new()
        {
            Text = t,
            AutoSize = true,
            Margin = new Padding(3, 12, 3, 4),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
        };
        private static Button B(string t, EventHandler h)
        {
            var b = new Button { Text = t, AutoSize = true, Height = 30 };
            b.Click += h;
            return b;
        }
        private static NumericUpDown Nud(decimal min, decimal max, decimal val) =>
            new() { Minimum = min, Maximum = max, Value = val, Width = 150, ThousandsSeparator = true };

        private TabPage BuildVisualizerTab()
        {
            var page = new TabPage("Visualizador");

            // Barra superior
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6), WrapContents = true };
            cboTipo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
            cboTipo.Items.AddRange(TreeFactory.Names);
            cboTipo.SelectedIndex = (int)TreeKind.Avl;
            cboTipo.SelectedIndexChanged += async (s, e) =>
            {
                if (_tree.Kind == SelectedKind) return;
                await ReconstruirAsync(SelectedKind, _keys.ToList(), $"Cambiado a {TreeFactory.Names[(int)SelectedKind]}");
            };

            txtValor = new TextBox { Width = 150, PlaceholderText = "Ej. 50  o  10, 20, 30" };
            txtValor.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Insertar(); }
            };

            nudAleatorios = new NumericUpDown { Minimum = 1, Maximum = 1_000_000, Value = 15, Width = 90, ThousandsSeparator = true };

            trkZoom = new TrackBar { Minimum = 3, Maximum = 20, Value = 10, TickStyle = TickStyle.None, AutoSize = false, Width = 120, Height = 30 };
            trkZoom.ValueChanged += (s, e) => { canvas.Zoom = trkZoom.Value / 10f; canvas.RecalculateLayout(); };

            bar.Controls.AddRange(new Control[]
            {
            L("Tipo:"), cboTipo, L("Valor(es):"), txtValor,
            B("Insertar", (s, e) => Insertar()),
            B("Buscar", (s, e) => Buscar()),
            B("Eliminar", (s, e) => Eliminar()),
            B("Limpiar", (s, e) => Limpiar()),
            L("|"),
            B("Cargar .txt…", async (s, e) => await CargarArchivoAsync()),
            L("Aleatorios:"), nudAleatorios,
            B("Insertar aleatorios", async (s, e) => await InsertarAleatoriosAsync()),
            L("|"), L("Zoom:"), trkZoom
            });

            // Panel inferior: recorridos + estadísticas
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 175, Padding = new Padding(6) };
            var travBar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
            chkAnimar = new CheckBox { Text = "Animar", Checked = true, AutoSize = true, Margin = new Padding(12, 8, 3, 3) };
            trkVelocidad = new TrackBar { Minimum = 1, Maximum = 10, Value = 6, TickStyle = TickStyle.None, AutoSize = false, Width = 120, Height = 30 };
            trkVelocidad.ValueChanged += (s, e) => animTimer.Interval = IntervaloAnimacion;
            travBar.Controls.AddRange(new Control[]
            {
            L("Recorridos:"),
            B("InOrden", (s, e) => Recorrer(TraversalKind.InOrden)),
            B("PreOrden", (s, e) => Recorrer(TraversalKind.PreOrden)),
            B("PostOrden", (s, e) => Recorrer(TraversalKind.PostOrden)),
            B("Por niveles", (s, e) => Recorrer(TraversalKind.PorNiveles)),
            chkAnimar, L("Velocidad:"), trkVelocidad,
            B("Detener", (s, e) => { StopAnimation(); canvas.Invalidate(); })
            });
            txtRecorrido = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10f)
            };
            lblStats = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            bottom.Controls.Add(txtRecorrido);
            bottom.Controls.Add(travBar);
            bottom.Controls.Add(lblStats);

            canvas = new TreeCanvas { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Tree = _tree };
            canvas.NodeClicked += k => txtValor.Text = k.ToString();

            page.Controls.Add(canvas);
            page.Controls.Add(bar);
            page.Controls.Add(bottom);
            return page;
        }

        private TabPage BuildBenchmarkTab()
        {
            var page = new TabPage("Benchmark");

            var left = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 310,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            rbArchivo = new RadioButton { Text = "Usar archivo .txt", AutoSize = true };
            rbGenerar = new RadioButton { Text = "Generar datos", AutoSize = true, Checked = true };
            lblArchivoBench = new Label { Text = "(ningún archivo seleccionado)", AutoSize = false, Width = 270, Height = 36 };

            nudN = Nud(10, 5_000_000, 100_000);
            nudMin = Nud(-1_000_000_000, 1_000_000_000, 1);
            nudMax = Nud(-1_000_000_000, 1_000_000_000, 1_000_000);
            nudReps = Nud(1, 20, 3);

            cboDistribucion = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 270 };
            cboDistribucion.Items.AddRange(NumberFile.DistributionNames);
            cboDistribucion.SelectedIndex = 0;

            clbArboles = new CheckedListBox { CheckOnClick = true, Width = 270, Height = 90 };
            clbArboles.Items.AddRange(TreeFactory.Names);
            for (int i = 0; i < clbArboles.Items.Count; i++) clbArboles.SetItemChecked(i, true);

            btnEjecutar = B("▶  Ejecutar benchmark", async (s, e) => await EjecutarBenchmarkAsync());
            btnEjecutar.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnCancelar = B("Cancelar", (s, e) => _cts?.Cancel());
            btnCancelar.Enabled = false;
            btnExportar = B("Exportar resultados a CSV…", (s, e) => ExportarCsv());

            progress = new ProgressBar { Width = 270, Height = 18 };
            lblBenchInfo = new Label { AutoSize = false, Width = 270, Height = 60, Text = "" };

            left.Controls.AddRange(new Control[]
            {
            Titulo("1. Origen de los datos"),
            rbArchivo, B("Elegir .txt…", (s, e) => ElegirArchivoBench()), lblArchivoBench,
            rbGenerar, L("Cantidad (N):"), nudN, L("Distribución:"), cboDistribucion,
            L("Mínimo (en ordenados = valor inicial):"), nudMin, L("Máximo (solo aleatorio):"), nudMax,
            B("Guardar datos generados como .txt…", (s, e) => GuardarTxtGenerado()),
            Titulo("2. Árboles a comparar"), clbArboles,
            L("Repeticiones (se promedia):"), nudReps,
            Titulo("3. Ejecutar"), btnEjecutar, btnCancelar, progress, lblBenchInfo, btnExportar
            });

            // Lado derecho: tabla + gráfica
            splitBench = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window
            };
            string[] cols = { "Árbol", "N", "Únicos", "Inserción (ms)", "Búsqueda (ms)", "Recorrido (ms)",
                          "Eliminación (ms)", "Altura", "Altura ideal", "Comp. inserción", "Comp. búsqueda", "Rotaciones" };
            for (int i = 0; i < cols.Length; i++) grid.Columns.Add("c" + i, cols[i]);
            splitBench.Panel1.Controls.Add(grid);

            var chartBar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
            cboMetrica = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
            cboMetrica.Items.AddRange(Metricas);
            cboMetrica.SelectedIndex = 0;
            cboMetrica.SelectedIndexChanged += (s, e) => UpdateChart();
            chartBar.Controls.Add(L("Graficar:"));
            chartBar.Controls.Add(cboMetrica);

            chart = new BarChart { Dock = DockStyle.Fill };
            splitBench.Panel2.Controls.Add(chart);
            splitBench.Panel2.Controls.Add(chartBar);

            page.Controls.Add(splitBench);
            page.Controls.Add(left);
            return page;
        }

        // =====================================================================
        //  VISUALIZADOR
        // =====================================================================
        private TreeKind SelectedKind => (TreeKind)cboTipo.SelectedIndex;
        private int IntervaloAnimacion => 1100 - trkVelocidad.Value * 100;

        private void Status(string msg) => lblStatus.Text = msg;

        private bool TryGetValues(out List<int> values)
        {
            values = new List<int>();
            foreach (string p in txtValor.Text.Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(p, out int v))
                {
                    Status($"\"{p}\" no es un número entero válido.");
                    return false;
                }
                values.Add(v);
            }
            if (values.Count == 0) { Status("Escribe un número entero en el campo Valor."); return false; }
            return true;
        }

        private void Insertar()
        {
            if (!TryGetValues(out var vals)) return;
            StopAnimation(); ClearHighlights();

            long c0 = _tree.Comparisons, r0 = _tree.Rotations;
            int ok = 0;
            var sw = Stopwatch.StartNew();
            foreach (int v in vals)
                if (_tree.Insert(v)) { _keys.Add(v); ok++; }
            sw.Stop();

            int last = vals[^1];
            var path = _tree.SearchPath(last);
            if (path.Count > 0 && path[^1].Key == last) canvas.FoundNode = path[^1];
            RefreshView();
            canvas.EnsureVisible(canvas.FoundNode);

            int dup = vals.Count - ok;
            Status($"Insertados: {ok}{(dup > 0 ? $" ({dup} repetido(s) ignorado(s))" : "")} · " +
                   $"{_tree.Comparisons - c0} comparaciones · {_tree.Rotations - r0} rotaciones · " +
                   $"{sw.Elapsed.TotalMilliseconds * 1000:F1} µs");
            txtValor.SelectAll();
            txtValor.Focus();
        }

        private void Buscar()
        {
            if (!TryGetValues(out var vals)) return;
            int v = vals[0];
            StopAnimation(); ClearHighlights();

            var path = _tree.SearchPath(v);              // ruta antes de que Splay reacomode
            long c0 = _tree.Comparisons;
            bool found = _tree.Contains(v);

            canvas.PathNodes = new HashSet<Node>(path);
            if (found) canvas.FoundNode = path[^1];
            RefreshView();
            if (found) canvas.EnsureVisible(canvas.FoundNode);

            string ruta = string.Join(" → ", path.Take(25).Select(n => n.Key)) + (path.Count > 25 ? " → …" : "");
            string extra = _tree.Kind == TreeKind.Splay && path.Count > 0 ? " (Splay: el último nodo visitado subió a la raíz)" : "";
            Status(found
                ? $"Encontrado {v} en {path.Count} pasos ({_tree.Comparisons - c0} comparaciones). Ruta: {ruta}{extra}"
                : $"{v} no existe en el árbol. Ruta recorrida: {ruta}{extra}");
        }

        private void Eliminar()
        {
            if (!TryGetValues(out var vals)) return;
            StopAnimation(); ClearHighlights();

            long r0 = _tree.Rotations;
            int ok = 0;
            foreach (int v in vals)
                if (_tree.Remove(v)) { _keys.Remove(v); ok++; }

            RefreshView();
            Status(ok > 0
                ? $"Eliminados: {ok} de {vals.Count} · {_tree.Rotations - r0} rotaciones de rebalanceo"
                : "Ninguno de esos valores existe en el árbol.");
        }

        private void Limpiar()
        {
            StopAnimation(); ClearHighlights();
            _tree.Clear();
            _keys.Clear();
            txtRecorrido.Clear();
            RefreshView();
            Status("Árbol vaciado.");
        }

        private async Task CargarArchivoAsync()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                Title = "Selecciona un archivo con números"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            int[] nums; int bad;
            try { (nums, bad) = NumberFile.Load(ofd.FileName); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Error al leer", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            if (nums.Length == 0)
            {
                MessageBox.Show(this, "El archivo no contiene números enteros válidos.", "Archivo vacío",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string inval = bad > 0 ? $", {bad:N0} valores inválidos" : "";
            await ReconstruirAsync(SelectedKind, nums, $"{Path.GetFileName(ofd.FileName)} ({nums.Length:N0} números leídos{inval})");
        }

        private async Task InsertarAleatoriosAsync()
        {
            int n = (int)nudAleatorios.Value;
            int max = Math.Max(999, n * 10);
            var rnd = new Random();
            var source = _keys.ToList();
            for (int i = 0; i < n; i++) source.Add(rnd.Next(1, max + 1));
            await ReconstruirAsync(SelectedKind, source, $"{n:N0} aleatorios agregados (rango 1–{max:N0})");
        }

        /// <summary>Construye un árbol nuevo en segundo plano (no congela la ventana con archivos grandes).</summary>
        private async Task ReconstruirAsync(TreeKind kind, IReadOnlyList<int> source, string origen)
        {
            StopAnimation(); ClearHighlights();
            UseWaitCursor = true;
            tabs.Enabled = false;
            Status($"Construyendo {TreeFactory.Names[(int)kind]} con {source.Count:N0} valores…");
            try
            {
                var sw = Stopwatch.StartNew();
                var (tree, inserted) = await Task.Run(() =>
                {
                    TreeBase t = TreeFactory.Create(kind);
                    var ins = new List<int>(source.Count);
                    foreach (int x in source)
                        if (t.Insert(x)) ins.Add(x);
                    return (t, ins);
                });
                sw.Stop();

                _tree = tree;
                _keys.Clear();
                _keys.AddRange(inserted);
                txtRecorrido.Clear();
                RefreshView();

                int dup = source.Count - inserted.Count;
                string dibujo = _tree.Count > TreeCanvas.MaxDrawNodes
                    ? $" · Más de {TreeCanvas.MaxDrawNodes:N0} nodos: no se dibuja, pero estadísticas y recorridos sí funcionan."
                    : "";
                Status($"{origen}: {inserted.Count:N0} nodos en {sw.Elapsed.TotalMilliseconds:F1} ms · {dup:N0} repetidos ignorados{dibujo}");
            }
            finally
            {
                UseWaitCursor = false;
                tabs.Enabled = true;
            }
        }

        private static string NombreRecorrido(TraversalKind k) => k switch
        {
            TraversalKind.InOrden => "InOrden",
            TraversalKind.PreOrden => "PreOrden",
            TraversalKind.PostOrden => "PostOrden",
            _ => "Por niveles"
        };

        private void Recorrer(TraversalKind k)
        {
            StopAnimation(); ClearHighlights();
            if (_tree.Root == null) { Status("El árbol está vacío."); return; }

            var sw = Stopwatch.StartNew();
            var list = _tree.Traverse(k);
            sw.Stop();
            string nombre = NombreRecorrido(k);

            if (chkAnimar.Checked && _tree.Count <= TreeCanvas.MaxDrawNodes)
            {
                _animList = list;
                _animIndex = 0;
                txtRecorrido.Text = $"{nombre}: ";
                animTimer.Interval = IntervaloAnimacion;
                animTimer.Start();
            }
            else
            {
                txtRecorrido.Text = $"{nombre}: " + Formatear(list);
                canvas.Invalidate();
            }
            Status($"Recorrido {nombre}: {list.Count:N0} nodos en {sw.Elapsed.TotalMilliseconds:F3} ms");
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (_animList == null || _animIndex >= _animList.Count)
            {
                StopAnimation();
                canvas.Invalidate();
                return;
            }
            if (canvas.CurrentNode != null) canvas.VisitedNodes.Add(canvas.CurrentNode);
            Node n = _animList[_animIndex++];
            canvas.CurrentNode = n;
            txtRecorrido.AppendText((_animIndex > 1 ? ", " : "") + n.Key);
            canvas.EnsureVisible(n);
            canvas.Invalidate();
        }

        private void StopAnimation()
        {
            animTimer.Stop();
            if (canvas.CurrentNode != null)
            {
                canvas.VisitedNodes.Add(canvas.CurrentNode);
                canvas.CurrentNode = null;
            }
            _animList = null;
        }

        private void ClearHighlights()
        {
            canvas.PathNodes.Clear();
            canvas.VisitedNodes.Clear();
            canvas.FoundNode = null;
            canvas.CurrentNode = null;
        }

        private static string Formatear(List<Node> list)
        {
            const int max = 20000;
            var sb = new StringBuilder();
            int c = Math.Min(max, list.Count);
            for (int i = 0; i < c; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(list[i].Key);
            }
            if (list.Count > max) sb.Append($" … (+{list.Count - max:N0} más)");
            return sb.ToString();
        }

        private void RefreshView()
        {
            canvas.Tree = _tree;
            canvas.RecalculateLayout();
            lblStats.Text = _tree.Count == 0
                ? $"{_tree.Name}  |  vacío"
                : $"{_tree.Name}  |  Nodos: {_tree.Count:N0}  |  Altura: {_tree.Height()} (mínima posible: {TreeBase.IdealHeight(_tree.Count)})" +
                  $"  |  Mín: {_tree.MinKey()}   Máx: {_tree.MaxKey()}  |  Comparaciones: {_tree.Comparisons:N0}  |  Rotaciones: {_tree.Rotations:N0}";
        }

        // =====================================================================
        //  BENCHMARK
        // =====================================================================
        private void ElegirArchivoBench()
        {
            using var ofd = new OpenFileDialog { Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*" };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var (nums, bad) = NumberFile.Load(ofd.FileName);
                if (nums.Length == 0) { MessageBox.Show(this, "El archivo no contiene números válidos."); return; }
                _benchFileData = nums;
                lblArchivoBench.Text = $"{Path.GetFileName(ofd.FileName)} — {nums.Length:N0} números" + (bad > 0 ? $" ({bad} inválidos)" : "");
                rbArchivo.Checked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error al leer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int[] GenerarDatos() =>
            NumberFile.Generate((int)nudN.Value, (Distribution)cboDistribucion.SelectedIndex,
                                (int)nudMin.Value, (int)nudMax.Value, Environment.TickCount);

        private void GuardarTxtGenerado()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Archivos de texto (*.txt)|*.txt",
                FileName = $"numeros_{(int)nudN.Value}_{(Distribution)cboDistribucion.SelectedIndex}.txt"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            NumberFile.Save(sfd.FileName, GenerarDatos());
            Status($"Archivo guardado: {sfd.FileName}");
        }

        private async Task EjecutarBenchmarkAsync()
        {
            int[] data;
            if (rbArchivo.Checked)
            {
                if (_benchFileData == null) { MessageBox.Show(this, "Primero elige un archivo .txt."); return; }
                data = _benchFileData;
            }
            else data = GenerarDatos();

            var kinds = clbArboles.CheckedIndices.Cast<int>().Select(i => (TreeKind)i).ToList();
            if (kinds.Count == 0) { MessageBox.Show(this, "Selecciona al menos un árbol."); return; }

            if (kinds.Contains(TreeKind.Bst) && data.Length > 20000 && NumberFile.LooksSorted(data))
            {
                var r = MessageBox.Show(this,
                    "Los datos están (casi) ordenados: el BST degenera en una lista y cada operación es O(n).\n" +
                    "Con muchos datos puede tardar MUCHO (minutos). ¿Continuar de todos modos?\n\n" +
                    "Tip: esto es justo lo que demuestra por qué existen AVL y Rojo-Negro.",
                    "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }

            int reps = (int)nudReps.Value;
            _cts = new CancellationTokenSource();
            SetBenchRunning(true);
            var prog = new Progress<BenchProgress>(p =>
            {
                progress.Maximum = Math.Max(1, p.Total);
                progress.Value = Math.Min(p.Done, progress.Maximum);
                lblBenchInfo.Text = p.Message;
            });

            try
            {
                var sw = Stopwatch.StartNew();
                var token = _cts.Token;
                _results = await Task.Run(() => BenchmarkRunner.Run(data, kinds, reps, prog, token));
                sw.Stop();
                ShowResults();
                var best = _results.OrderBy(x => x.InsertMs + x.SearchMs + x.RemoveMs).First();
                lblBenchInfo.Text = $"Terminado en {sw.Elapsed.TotalSeconds:F1} s.\nMás rápido en total: {best.TreeName}";
                Status($"Benchmark completado: {data.Length:N0} datos × {kinds.Count} árboles × {reps} repeticiones.");
            }
            catch (OperationCanceledException)
            {
                lblBenchInfo.Text = "Cancelado.";
                Status("Benchmark cancelado.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error en el benchmark", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBenchRunning(false);
                _cts.Dispose();
                _cts = null;
            }
        }

        private void SetBenchRunning(bool running)
        {
            btnEjecutar.Enabled = !running;
            btnCancelar.Enabled = running;
            btnExportar.Enabled = !running;
            tabs.TabPages[0].Enabled = !running;
            if (running) progress.Value = 0;
            UseWaitCursor = running;
        }

        private void ShowResults()
        {
            grid.Rows.Clear();
            foreach (var r in _results)
            {
                grid.Rows.Add(r.TreeName, r.N.ToString("N0"), r.Unique.ToString("N0"),
                    r.InsertMs.ToString("F2"), r.SearchMs.ToString("F2"), r.TraverseMs.ToString("F2"), r.RemoveMs.ToString("F2"),
                    r.Height, r.IdealHeight, r.InsertComparisons.ToString("N0"), r.SearchComparisons.ToString("N0"),
                    r.InsertRotations.ToString("N0"));
            }

            // Resalta en verde el mejor valor de cada columna
            var mejores = new (int col, Func<BenchmarkResult, double> f)[]
            {
            (3, x => x.InsertMs), (4, x => x.SearchMs), (5, x => x.TraverseMs), (6, x => x.RemoveMs),
            (7, x => x.Height), (9, x => x.InsertComparisons), (10, x => x.SearchComparisons)
            };
            foreach (var (col, f) in mejores)
            {
                double min = _results.Min(f);
                for (int i = 0; i < _results.Count; i++)
                {
                    if (f(_results[i]) != min) continue;
                    var cell = grid.Rows[i].Cells[col];
                    cell.Style.BackColor = Color.FromArgb(198, 239, 206);
                    cell.Style.ForeColor = Color.Black;
                }
            }
            UpdateChart();
        }

        private static double Metric(BenchmarkResult r, int m) => m switch
        {
            0 => r.InsertMs,
            1 => r.SearchMs,
            2 => r.TraverseMs,
            3 => r.RemoveMs,
            4 => r.Height,
            5 => r.InsertComparisons,
            6 => r.SearchComparisons,
            _ => r.InsertRotations
        };

        private static Color KindColor(TreeKind k) => k switch
        {
            TreeKind.Bst => Color.FromArgb(91, 141, 239),
            TreeKind.Avl => Color.FromArgb(46, 160, 67),
            TreeKind.RedBlack => Color.FromArgb(214, 40, 57),
            _ => Color.FromArgb(255, 159, 28)
        };

        private void UpdateChart()
        {
            if (chart == null || cboMetrica == null) return;
            int m = Math.Max(0, cboMetrica.SelectedIndex);
            chart.Title = Metricas[m] + (_results.Count > 0 ? $"   —   N = {_results[0].N:N0}" : "");
            chart.Items = _results.Select(r => (r.TreeName, Metric(r, m), KindColor(r.Kind))).ToList();
            chart.Invalidate();
        }

        private void ExportarCsv()
        {
            if (_results.Count == 0) { MessageBox.Show(this, "Aún no hay resultados."); return; }
            using var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "benchmark_arboles.csv" };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Arbol,N,Unicos,InsercionMs,BusquedaMs,RecorridoMs,EliminacionMs,Altura,AlturaIdeal,ComparacionesInsercion,ComparacionesBusqueda,RotacionesInsercion");
            foreach (var r in _results)
            {
                sb.AppendLine(string.Join(",", r.TreeName, r.N.ToString(inv), r.Unique.ToString(inv),
                    r.InsertMs.ToString("F3", inv), r.SearchMs.ToString("F3", inv), r.TraverseMs.ToString("F3", inv),
                    r.RemoveMs.ToString("F3", inv), r.Height.ToString(inv), r.IdealHeight.ToString(inv),
                    r.InsertComparisons.ToString(inv), r.SearchComparisons.ToString(inv), r.InsertRotations.ToString(inv)));
            }
            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
            Status($"Resultados exportados: {sfd.FileName}");
        }

        // =====================================================================
        //  MODO OSCURO
        // =====================================================================
        private void ApplyTheme(bool dark)
        {
            Color bg = dark ? Color.FromArgb(32, 32, 34) : SystemColors.Control;
            Color fg = dark ? Color.Gainsboro : SystemColors.ControlText;
            Color input = dark ? Color.FromArgb(48, 48, 52) : SystemColors.Window;
            Color btn = dark ? Color.FromArgb(60, 60, 66) : SystemColors.Control;

            void Apply(Control c)
            {
                switch (c)
                {
                    case TreeCanvas tc: tc.DarkMode = dark; tc.Invalidate(); return;
                    case BarChart bc: bc.DarkMode = dark; bc.Invalidate(); return;
                    case DataGridView dg: StyleGrid(dg, dark); return;
                    case TextBox or ComboBox or NumericUpDown or CheckedListBox:
                        c.BackColor = input; c.ForeColor = fg; return;
                    case Button b:
                        b.FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard;
                        b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 96);
                        b.BackColor = btn; b.ForeColor = fg;
                        b.UseVisualStyleBackColor = !dark;
                        return;
                    default:
                        c.BackColor = bg; c.ForeColor = fg;
                        break;
                }
                foreach (Control ch in c.Controls) Apply(ch);
            }
            Apply(this);
        }

        private static void StyleGrid(DataGridView g, bool dark)
        {
            g.EnableHeadersVisualStyles = !dark;
            g.BackgroundColor = dark ? Color.FromArgb(28, 28, 30) : SystemColors.Window;
            g.GridColor = dark ? Color.FromArgb(70, 70, 75) : SystemColors.ControlDark;
            g.DefaultCellStyle.BackColor = dark ? Color.FromArgb(40, 40, 44) : Color.White;
            g.DefaultCellStyle.ForeColor = dark ? Color.Gainsboro : Color.Black;
            g.DefaultCellStyle.SelectionBackColor = dark ? Color.FromArgb(70, 90, 130) : SystemColors.Highlight;
            g.ColumnHeadersDefaultCellStyle.BackColor = dark ? Color.FromArgb(50, 50, 55) : SystemColors.Control;
            g.ColumnHeadersDefaultCellStyle.ForeColor = dark ? Color.Gainsboro : Color.Black;
        }
    }
}
