/*
Вместо этих вложенных ссылок по-хорошему нужно сделать stack-based environment. Тогда нужно добавить функции возвращения из стека во всякие "for", "closest".
Банально вместо Remove() нужно делать UpStack(), который просто будет убирать все переменные с конца стека определённого уровня вложенности.
Но совершенно точно дебажить удобнее с простым словарём.
 
*/

class Env
{
    Dictionary<string, Value> dict;
    Env enclosing;
    public string name;

    public Env()
    {
        dict = [];
        name = "global";
    }
    public Env(Env enclosing)
    {
        this.enclosing = enclosing;
        dict = [];
        name = "child of " + enclosing.name;
    }
    public Env Copy()
    {
        Env result = new();
        result.name = name;
        result.dict = new(dict);
        return result;
    }
    public (string, Value)[] Array() => dict.Select(kv => (kv.Key, Val: kv.Value)).ToArray();

    public void Reset()
    {
        dict.Clear();
    }

    public void Define(string name, Value value)
    {
        if (!dict.TryAdd(name, value)) throw new Exception($"the environment already contains variable '{name}'");
    }
    public void DefineOrSet(string name, Value value)
    {
        if (!dict.TryAdd(name, value)) dict[name] = value;
    }
    public void Set(string name, Value entity)
    {
        if (dict.ContainsKey(name)) dict[name] = entity;
        else if (enclosing != null) enclosing.Set(name, entity);
        else throw new Exception($"undefined variable '{name}'");
    }

    public Value Get(string name)
    {
        if (dict.TryGetValue(name, out Value entity)) return entity;
        else if (enclosing != null) return enclosing.Get(name);
        else throw new Exception($"undefined variable '{name}'");
    }
    public bool Has(string name, out Value value)
    {
        if (dict.TryGetValue(name, out Value value2))
        {
            value = value2;
            return true;
        }
        else if (enclosing != null)
        {
            //CS.WriteLine($"{this} does not have {name}", ConsoleColor.Blue);
            bool result = enclosing.Has(name, out Value value3);
            value = value3;
            return result;
        }
        else
        {
            //CS.WriteLine($"{this} does not have {name}", ConsoleColor.Blue);
            value = Value.VOID;
            return false;
        }
    }

    public override string ToString()
    {
        string result = $"{name} {{ ";
        int remaining = dict.Count;
        foreach (var kvp in dict)
        {
            result += $"[{kvp.Key}]={kvp.Value}";
            remaining--;
            if (remaining > 0) result += ", ";
        }
        return result + " }";
    }

    public Env Root()
    {
        Env current = this;
        while (current.enclosing != null) current = current.enclosing;
        return current;
    }
}
