using System.Diagnostics;
using SimuladorArboles.Trees;

namespace SimuladorArboles.Bench;

public record BenchProgress(int Done, int Total, string Message);

public class BenchmarkResult
{
    public TreeKind Kind;
    public string TreeName;
    public int N, Unique;
    public double InsertMs, SearchMs, TraverseMs, RemoveMs;   // promedios
    public int Height, IdealHeight;
    public long InsertComparisons, SearchComparisons, InsertRotations;
}

/// <summary>
/// Para cada árbol: inserta todos los datos, busca todos (orden aleatorio),
/// hace un recorrido InOrden y elimina todos (orden aleatorio). Repite y promedia.
/// </summary>
public static class BenchmarkRunner
{
    public static List<BenchmarkResult> Run(int[] data, IList<TreeKind> kinds, int reps,
                                            IProgress<BenchProgress> progress, CancellationToken ct)
    {
        var results = new List<BenchmarkResult>();
        int[] searchOrder = (int[])data.Clone();
        int[] removeOrder = (int[])data.Clone();
        NumberFile.Shuffle(searchOrder, new Random(12345));
        NumberFile.Shuffle(removeOrder, new Random(54321));

        progress?.Report(new BenchProgress(0, kinds.Count * reps, "Calentando (JIT)…"));
        Warmup();

        int total = kinds.Count * reps, done = 0;
        var sw = new Stopwatch();

        foreach (TreeKind kind in kinds)
        {
            var r = new BenchmarkResult { Kind = kind, N = data.Length };

            for (int rep = 0; rep < reps; rep++)
            {
                ct.ThrowIfCancellationRequested();
                TreeBase tree = TreeFactory.Create(kind);
                r.TreeName = tree.Name;
                progress?.Report(new BenchProgress(done, total, $"{tree.Name}: repetición {rep + 1}/{reps}…"));

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // 1) Inserción
                sw.Restart();
                for (int i = 0; i < data.Length; i++)
                {
                    tree.Insert(data[i]);
                    if ((i & 4095) == 0) ct.ThrowIfCancellationRequested();
                }
                sw.Stop();
                r.InsertMs += sw.Elapsed.TotalMilliseconds;
                r.InsertComparisons = tree.Comparisons;
                r.InsertRotations = tree.Rotations;
                r.Height = tree.Height();
                r.Unique = tree.Count;
                r.IdealHeight = TreeBase.IdealHeight(tree.Count);

                // 2) Búsqueda
                tree.ResetMetrics();
                sw.Restart();
                for (int i = 0; i < searchOrder.Length; i++)
                {
                    tree.Contains(searchOrder[i]);
                    if ((i & 4095) == 0) ct.ThrowIfCancellationRequested();
                }
                sw.Stop();
                r.SearchMs += sw.Elapsed.TotalMilliseconds;
                r.SearchComparisons = tree.Comparisons;

                // 3) Recorrido
                sw.Restart();
                var list = tree.InOrder();
                sw.Stop();
                r.TraverseMs += sw.Elapsed.TotalMilliseconds;
                GC.KeepAlive(list);

                // 4) Eliminación
                sw.Restart();
                for (int i = 0; i < removeOrder.Length; i++)
                {
                    tree.Remove(removeOrder[i]);
                    if ((i & 4095) == 0) ct.ThrowIfCancellationRequested();
                }
                sw.Stop();
                r.RemoveMs += sw.Elapsed.TotalMilliseconds;

                done++;
                progress?.Report(new BenchProgress(done, total, $"{tree.Name}: repetición {rep + 1}/{reps} lista"));
            }

            r.InsertMs /= reps; r.SearchMs /= reps; r.TraverseMs /= reps; r.RemoveMs /= reps;
            results.Add(r);
        }
        return results;
    }

    private static void Warmup()
    {
        var rnd = new Random(1);
        var d = new int[2000];
        for (int i = 0; i < d.Length; i++) d[i] = rnd.Next(100000);
        foreach (TreeKind k in Enum.GetValues<TreeKind>())
        {
            var t = TreeFactory.Create(k);
            foreach (int x in d) t.Insert(x);
            foreach (int x in d) t.Contains(x);
            t.InOrder();
            foreach (int x in d) t.Remove(x);
        }
    }
}