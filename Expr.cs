class Expr
{
    public string head;
    public bool isWord; ///means that this thing was a keyword, and was not surrounded by quotes
    public Expr[] children;

    public Expr(string head, Expr[] children)
    {
        this.head = head;
        this.children = children;
    }

    public static Expr Load(string text)//, bool addRoot)
    {
        //if (addRoot) text = $"(ROOT {text}\n)";
        Lexer lexer = new(text);
        Token[] tokens = lexer.Run();
        return Parser.Parse(tokens);
    }
    /*public static Expr LoadFromFile(string filepath)
    {
        if (File.Exists(filepath))
        {
            string filetext = File.ReadAllText(filepath);
            return Load(filetext);
        }
        else throw new Exception($"no file <{filepath}>");
    }*/

    public Expr Tail() => new(children[0].head, children[1..]);
    public Expr FindChild(string childhead)
    {
        for (int i = 0; i < children.Length; i++)
        {
            Expr child = children[i];
            if (child.head == childhead) return child;
        }
        return null;
    }

    public string TallString(int indent)
    {
        string result = new string(' ', 2 * indent) + head + Environment.NewLine;
        foreach (Expr child in children) result += child.TallString(indent + 1);
        return result;
    }
    public override string ToString() => children.Length == 0 ? head : $"({head} {string.Join(' ', children.Select(c => c.ToString()))})";

    public bool IsFlatList()
    {
        for (int i = 0; i < children.Length; i++) if (children[i].children.Length > 0) return false;
        return true;
    }

    public List<Expr> Children(params string[] heads)
    {
        List<Expr> result = [];
        for (int c = 0; c < children.Length; c++)
        {
            Expr child = children[c];
            if (heads.Contains(child.head)) result.Add(child);
        }
        return result;
    }
    public List<Expr> Descendants(params string[] heads)
    {
        List<Expr> result = [];
        for (int c = 0; c < children.Length; c++)
        {
            Expr child = children[c];
            if (heads.Contains(child.head)) result.Add(child);
            List<Expr> childList = child.Descendants(heads);
            for (int i = 0; i < childList.Count; i++) result.Add(childList[i]);
        }
        return result;
    }
}

#if false
static class ExprHelper
{
    //проще их объединить в одно выражение, а потом найти детей имеющейся функцией
    public static List<Expr> Children(this List<Expr> xlist, params string[] heads)
    {
        List<Expr> result = [];
        for (int i = 0; i < xlist.Count; i++)
        {
            Expr expr = xlist[i];
            List<Expr> partial = expr.Children(heads);
            for (int j = 0; j < partial.Count; j++) result.Add(partial[j]);
        }
        return result;
    }
}
#endif
