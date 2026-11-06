namespace SimuladorArboles.Bench;

public enum Distribution { Aleatorio, Permutacion, Ascendente, Descendente, CasiOrdenado }

/// <summary>Lectura, generación y guardado de archivos .txt con números enteros.</summary>
public static class NumberFile
{
    public static readonly string[] DistributionNames =
    {
        "Aleatorio (con repetidos)",
        "Permutación sin repetidos",
        "Ascendente (peor caso BST)",
        "Descendente (peor caso BST)",
        "Casi ordenado (5% desordenado)"
    };

    private static readonly char[] Separators = { ' ', '\t', '\r', '\n', ',', ';', '|' };

    /// <summary>Acepta números separados por espacios, saltos de línea, comas o punto y coma.</summary>
    public static (int[] Numbers, int Invalid) Load(string path)
    {
        string[] parts = File.ReadAllText(path).Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<int>(parts.Length);
        int invalid = 0;
        foreach (string p in parts)
        {
            if (int.TryParse(p.Trim(), out int v)) list.Add(v);
            else invalid++;
        }
        return (list.ToArray(), invalid);
    }

    /// <summary>En Permutación/Ascendente/Descendente/Casi ordenado se usan los valores min, min+1, …, min+n-1.</summary>
    public static int[] Generate(int n, Distribution dist, int min, int max, int seed)
    {
        var rnd = new Random(seed);
        var a = new int[n];
        switch (dist)
        {
            case Distribution.Aleatorio:
                if (max < min) (min, max) = (max, min);
                for (int i = 0; i < n; i++) a[i] = rnd.Next(min, max + 1);
                break;
            case Distribution.Permutacion:
                for (int i = 0; i < n; i++) a[i] = min + i;
                Shuffle(a, rnd);
                break;
            case Distribution.Ascendente:
                for (int i = 0; i < n; i++) a[i] = min + i;
                break;
            case Distribution.Descendente:
                for (int i = 0; i < n; i++) a[i] = min + n - 1 - i;
                break;
            case Distribution.CasiOrdenado:
                for (int i = 0; i < n; i++) a[i] = min + i;
                for (int k = 0; k < n / 20; k++)
                {
                    int i = rnd.Next(n), j = rnd.Next(n);
                    (a[i], a[j]) = (a[j], a[i]);
                }
                break;
        }
        return a;
    }

    public static void Shuffle(int[] a, Random rnd)
    {
        for (int i = a.Length - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }

    public static void Save(string path, int[] data)
    {
        using var w = new StreamWriter(path);
        foreach (int x in data) w.WriteLine(x);
    }

    /// <summary>True si más del 90% de los pares consecutivos van en el mismo sentido.</summary>
    public static bool LooksSorted(int[] a)
    {
        if (a.Length < 2) return false;
        int asc = 0;
        for (int i = 1; i < a.Length; i++) if (a[i] >= a[i - 1]) asc++;
        double f = asc / (double)(a.Length - 1);
        return f > 0.9 || f < 0.1;
    }
}