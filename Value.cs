using System.Numerics;

enum VTYPE { VOID, BOOL, INT, FLOAT, OBJ, TEMPLATE, VECTOR, TUPLE, LIST };
enum ACTION { NAA, TURN, GO, BACK, USE, DROP, THROW }; //(!)большой вопрос, нужен ли мне этот enum вообще! Более того, как быть с параметрическими штуками типа EQUIP?

static class ACTIONHelper
{
    static string[] actionNames = Enum.GetNames<ACTION>();
    public static string Name(this ACTION action) => actionNames[(int)action];
}

static class TH
{
    public static string[] typeNames = Enum.GetNames<VTYPE>();
    public static string Name(this VTYPE t) => typeNames[(int)t];
    public static int T = typeNames.Length;

    public static char[] symbols = ['v', 'b', 'i', 'f', 'o', 't', 'v', 'u', 'l'];
    public static Dictionary<char, VTYPE> dict;
    static TH()
    {
        if (symbols.Length != Enum.GetNames<VTYPE>().Length) throw new Exception($"missing type symbols");
        dict = [];
        for (int i = 0; i < symbols.Length; i++) dict[symbols[i]] = (VTYPE)i;
    }

    public static string ObsVecString(this Value[] a)
    {
        string result = "[";
        for (int i = 0; i < a.Length - 1; i++) result += a[i].ToString() + ", ";
        return result + a[^1].ToString() + "]";
    }
}

struct Value
{
    public VTYPE type;

    int i;
    Vector2 v;
    Template t; //можно и в инте это хранить, как с объектами. Это стоит того, потому что value повсеместны
    List<int> l; ///список объектов. Тут важно не иметь указатели на объекты

    Value(VTYPE type, int i, Vector2 v, Template t, List<int> l)
    {
        this.type = type;
        this.i = i;
        this.v = v;
        this.t = t;
        this.l = l;
    }

    public static Value TupleValue(ACTION a, Vector2 v) => new(VTYPE.TUPLE, (int)a, v, null, null);
    public static Value BoolValue(bool b) => new(VTYPE.BOOL, b ? 1 : 0, Vector2.Zero, null, null);
    public static Value IntValue(int i) => new(VTYPE.INT, i, Vector2.Zero, null, null);
    public static Value ObjectValue(Object o) => new(VTYPE.OBJ, o.uid, Vector2.Zero, null, null);
    public static Value ObjectValue(int uid) => new(VTYPE.OBJ, uid, Vector2.Zero, null, null);
    public static Value FloatValue(float f) => new(VTYPE.FLOAT, -1, new Vector2(f, 0f), null, null);
    public static Value TemplateValue(Template t) => new(VTYPE.TEMPLATE, -1, Vector2.Zero, t, null);
    public static Value VectorValue(Vector2 v) => new(VTYPE.VECTOR, -1, v, null, null);
    public static Value ListValue(List<int> l) => new(VTYPE.LIST, -1, Vector2.Zero, null, l);

    public static Value PASS = TupleValue(ACTION.TURN, Vector2.Zero),
                        NONE = new(VTYPE.OBJ, -1, Vector2.Zero, null, null),
                        NOTHING = new(VTYPE.TEMPLATE, -1, Vector2.Zero, null, null),
                        FALSE = BoolValue(false),
                        TRUE = BoolValue(true),
                        ZEROVEC = VectorValue(Vector2.Zero),
                        VOID = new(VTYPE.VOID, -1, Vector2.Zero, null, null);

    public static bool Same(Value v1, Value v2) => v1.type == v2.type && v1.i == v2.i && v1.v == v2.v && v1.t == v2.t && v1.l == v2.l;

    public readonly (ACTION, Vector2) GetTuple()
    {
        if (type != VTYPE.TUPLE) throw new Exception($"{type} is not {VTYPE.TUPLE}");
        return ((ACTION)i, v);
    }
    public readonly bool GetBool()
    {
        if (type != VTYPE.BOOL) throw new Exception($"{type} is not {VTYPE.BOOL}");
        return i > 0;
    }
    public readonly int GetInt()
    {
        if (type != VTYPE.INT) throw new Exception($"{type} is not {VTYPE.INT}");
        return i;
    }
    public readonly float GetFloat()
    {
        if (type != VTYPE.FLOAT) throw new Exception($"{type} is not {VTYPE.FLOAT}");
        return v.X;
    }
    public readonly Object GetObject(State state)
    {
        if (type != VTYPE.OBJ) throw new Exception($"{type} is not {VTYPE.OBJ}");
        //if (i < 0) return null;
        return i >= 0 && state.uiddict.TryGetValue(i, out Object o) ? o : null;
        //else return null;
        //return i >= 0 ? state.uiddict[i] : null;
    }
    public readonly List<int> GetList()
    {
        if (type != VTYPE.LIST) throw new Exception($"{type} is not {VTYPE.LIST}");
        return l;
    }
    public readonly Template GetTemplate()
    {
        if (type != VTYPE.TEMPLATE) throw new Exception($"{type} is not {VTYPE.TEMPLATE}");
        return t;
    }
    public readonly Vector2 GetVector()
    {
        if (type != VTYPE.VECTOR) throw new Exception($"{type} is not {VTYPE.VECTOR}");
        return v;
    }
    /*public readonly Object[] GetList()
    {
        if (type != VTYPE.LIST) throw new Exception($"{type} is not {VTYPE.LIST}");
        return l;
    }*/

    public override readonly string ToString()
    {
        if (type == VTYPE.TUPLE)
        {
            ACTION a = (ACTION)i;
            string result = a.Name();
            if (a == ACTION.GO) result += " " + v.ToString("0.00");
            return result;
        }
        else if (type == VTYPE.BOOL) return i > 0 ? "true" : "false";
        else if (type == VTYPE.INT) return i.ToString();
        else if (type == VTYPE.OBJ) return $"o{i}";
        else if (type == VTYPE.FLOAT) return v.X.ToString("0.##");
        else if (type == VTYPE.VOID) return "VOID";
        else if (type == VTYPE.VECTOR) return v.ToString("0.00");
        else if (type == VTYPE.LIST)
        {
            string result = "[";
            for (int i = 0; i < l.Count; i++)
            {
                result += l[i];
                if (i < l.Count - 1) result += ", ";
            }
            return result + "]";
        }
        else if (type == VTYPE.TEMPLATE) return t == null ? "NOTHING" : t.name;
        else throw new Exception($"unknown type {type}");
    }
}
