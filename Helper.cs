using System.Text;
using System.Globalization;

static class Helper
{
    public static int MyIndex<T>(this List<T> list, Predicate<T> p)
    {
        for (int i = 0; i < list.Count; i++) if (p(list[i])) return i;
        return -1;
    }

    public static string MaxSubstring(this string s, int maxlength)
    {
        if (s.Length <= maxlength) return s;
        int lastIndex = maxlength;
        while (lastIndex > 0 && s[lastIndex] != ' ') lastIndex--;
        return lastIndex > 0 ? s[..lastIndex] : s[..maxlength];
    }

    public static bool[] ConvertToBoolVector(int i, int n)
    {
        bool[] boolVector = new bool[n];
        for (int bit = 0; bit < n; bit++) boolVector[bit] = (i & (1 << bit)) != 0;
        return boolVector;
    }

    public static int ConvertToInt(bool[] boolVector)
    {
        int result = 0;
        for (int bit = 0; bit < boolVector.Length; bit++) if (boolVector[bit]) result |= (1 << bit);
        return result;
    }

    public static string BoolJoin(this bool[] a)
    {
        string result = "";
        for (int i = 0; i < a.Length; i++) result += a[i] ? '1' : '0';
        return result;
    }
    public static string FloatJoin(this float[] a)
    {
        string result = "";
        for (int i = 0; i < a.Length - 1; i++) result += $"{a[i]:0.##}, ";
        return result + a[^1].ToString("0.##");
    }
    /*public static string FloatJoin(this float[] a, int[] multiplicities)
    {
        string result = "";
        for (int i = 0; i < a.Length - 1; i++)
        {
            int m = multiplicities[i];
            result += a[i].ToString("0.##");
            if (m != 0) result += $" ({m})";
            result += ", ";
        }
        result += a[^1].ToString("0.##");
        if (multiplicities[^1] != 0) result += $" ({multiplicities[^1]})";
        return result;
    }*/
    public static string FloatJoin(this float[] a, int[] multiplicities, int itemWidth = 10)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (multiplicities == null) throw new ArgumentNullException(nameof(multiplicities));
        if (a.Length != multiplicities.Length) throw new ArgumentException("Arrays must have the same length.");

        var sb = new StringBuilder(a.Length * (itemWidth + 2));
        for (int i = 0; i < a.Length; i++)
        {
            string field = a[i].ToString("0.##", CultureInfo.InvariantCulture);
            int m = multiplicities[i];
            if (m != 0) field += $" ({m})";

            if (field.Length < itemWidth) field = field.PadLeft(itemWidth);
            sb.Append(field);
            if (i < a.Length - 1) sb.Append(", ");
        }
        return sb.ToString();
    }

    public static bool Same(bool[] a1, bool[] a2)
    {
        if (a1.Length != a2.Length) return false;
        for (int i = 0; i < a1.Length; i++) if (a1[i] != a2[i]) return false;
        return true;
    }

    public static int ArgMax(this float[] a, Random random)
    {
        float max = -1000000f;
        List<int> candidates = [];
        for (int i = 0; i < a.Length; i++)
        {
            float v = a[i];
            if (v >= max)
            {
                if (v > max)
                {
                    candidates.Clear();
                    max = v;
                }
                candidates.Add(i);
            }
        }
        return candidates[random.Next(candidates.Count)];
    }

    public static int LessThan(this int[] a, int M, Random random)
    {
        List<int> candidates = [];
        for (int i = 0; i < a.Length; i++) if (a[i] < M) candidates.Add(i);
        return candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : -1;
    }

    public static string MyJoin<T>(this T[] a) => string.Join(' ', a);

    //чтобы сделать баунды, эту функцию потом придётся модифицировать. Она будет принимать на вход 2 массива длины k
    public static List<int[]> Partitions(int N, int k)
    {
        if (k <= 0) throw new Exception($"wrong k = {k}");
        if (N < k) return [];
        if (k == 1) return [[N]];

        var result = new List<int[]>();
        int r = k - 1;                       // number of dividers
        int[] div = new int[r];              // positions of the dividers (1-based)
        for (int i = 0; i < r; i++) div[i] = i + 1;  // initial combination 1,2,…,k-1

        while (true)
        {
            // convert current divider positions to the actual k parts
            int[] parts = new int[k];
            parts[0] = div[0];
            for (int i = 1; i < r; i++) parts[i] = div[i] - div[i - 1];
            parts[k - 1] = N - div[r - 1];
            result.Add(parts);

            //next combination of dividers, find right-most divider that can still move to the right
            int j = r - 1;
            while (j >= 0 && div[j] == N - (r - j)) j--;
            if (j < 0) break;                       // last combination reached

            div[j]++;                                                // move it
            for (int t = j + 1; t < r; t++) div[t] = div[t - 1] + 1; // and reset the tail
        }

        return result;
    }

    public static int[] AllSubstrings(this string s, string sub)
    {
        if (string.IsNullOrEmpty(sub)) return [];

        var hits = new List<int>();
        for (int idx = 0; (idx = s.IndexOf(sub, idx)) != -1; idx += sub.Length) hits.Add(idx);
        return hits.ToArray();
    }

    public static string ReplaceAtIndices(this string s, int[] indices, int matchLength, string replacement)
    {
        if (indices.Length == 0) return s;

        var sb = new StringBuilder(s.Length - indices.Length * matchLength + indices.Length * replacement.Length);
        int srcPos = 0;
        foreach (int hit in indices)
        {            
            sb.Append(s, srcPos, hit - srcPos); //copy text that comes before the match            
            sb.Append(replacement); //insert replacement            
            srcPos = hit + matchLength; //advance past the matched segment
        }        
        sb.Append(s, srcPos, s.Length - srcPos); //copy any trailing text
        return sb.ToString();
    }

    //public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

    public static T[] Flatten<T>(this List<T>[] bags)
    {
        List<T> result = [];
        for (int l = 0; l < bags.Length; l++) result.AddRange(bags[l]);
        return result.ToArray();
    }

    //можно написать и получше - например, цикл тут стоит начинать с 1, а не 0
    public static T ArgMax<T>(this List<T> list, Func<T, float> f)
    {
        float max = f(list[0]);
        T argmax = list[0];
        for (int i = 0; i < list.Count; i++)
        {
            T item = list[i];
            float value = f(item);
            if (value > max)
            {
                max = value;
                argmax = item;
            }
        }
        return argmax;
    }
}
