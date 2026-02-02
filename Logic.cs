/*
думаю о этого избавиться вообще, потому что есть исключения типа (onResource Food (...))
В данном случае Food - не имя переменной.
Довольно естестевенно весь триггер хранить как одно выражение.

Кстати, onFrame, onDeath тоже можно считать триггерами, просто без аргументов. Но не горит.
*/
#if false
class Logic
{
    public List<(string, Expr)> lets;
    public Expr onFrame;
    public ByteCode onCollision0;
    public Trigger onCreate, onCollision1, onCollision2;
    Game game;

    public Logic(Game game)
    {
        lets = [];
        this.game = game;
    }

    public bool Add(Expr x)
    {
        if (x.head == "let") lets.Add((x.children[0].head, x.children[1]));
        else if (x.head == "onCreate")
        {
            (onCreate, var newlets) = OnCreateSugar(x, game);
            lets.AddRange(newlets);
        }
        else if (x.head == "onCollision")
        {
            if (x.children.Length == 1) onCollision0 = Compiler.Run(x.children[0], game);
            else if (x.children.Length == 2) onCollision1 = new Trigger(x, game);
            else if (x.children.Length == 3) onCollision2 = new Trigger(x, game);
            else throw new Exception($"wrong number {x.children.Length} of children in {x.head}");
        }
        else if (x.head == "onFrame") onFrame = x.children[0]; //often has "foreach"
        else return false;

        return true;
    }

    static (Trigger trigger, List<(string, Expr)> lets) OnCreateSugar(Expr xtop, Game game)
    {
        List<(string, Expr)> list = [];
        if (xtop.IsFlatList())
        {
            Expr xnothing = Expr.Load("nothing", false);
            string TA = "";
            string eq = "";
            for (int c = 0; c < xtop.children.Length; c++)
            {
                string varname = xtop.children[c].head;
                list.Add((varname, xnothing));
                TA += $"V{c} ";
                eq += $"(= {varname} V{c}) ";
            }
            Expr triggerExpr = Expr.Load($"(onCreate {TA}(seq {eq[..^1]}))", false);
            return (new Trigger(triggerExpr, game), list);
        }
        else return (new Trigger(xtop, game), []);
    }
}

class Trigger
{
    string[] argnames;
    ByteCode bc;
    //Expr expr;

    public Trigger(Expr x, Game game) ///x включает в себя и объявление переменых
    {
        argnames = new string[x.children.Length - 1];
        for (int i = 0; i < argnames.Length; i++) argnames[i] = x.children[i].head;
        bc = Compiler.Run(x.children[^1], game);
        //expr = x.children[^1];
        //CS.WriteLine($"created a trigger {expr} with argnames {argstring}", ConsoleColor.DarkBlue);
    }

    public void Execute(State state, Env env, params Value[] values)
    {
        Env childenv = new(env); ///заметим, что это дочерний энвайронмент
        if (values.Length != argnames.Length) throw new Exception($"mismatch between amounts of arguments in trigger <{bc}>");
        for (int i = 0; i < argnames.Length; i++) childenv.Define(argnames[i], values[i]);
        //expr.Eval(state, childenv);
        bc.Execute(state, childenv);
    }
}

class Mode
{
    public string name;
    public Logic logic;

    public Mode(string name, Expr root, Game game)
    {
        this.name = name;
        logic = new(game);

        for (int i = 0; i < root.children.Length; i++)
        {
            Expr x = root.children[i];
            if (logic.Add(x)) continue;
            else throw new Exception($"unknown head <{x.head}> in mode {name}");
        }
    }

    public override string ToString() => name;
}

class ModeObject
{
    public Mode mode;
    public Env env;

    public ModeObject(Mode mode)
    {
        this.mode = mode;
        env = new(); ///so this is a global environment, this is fine
        env.name = $"modeenv-{mode.name}";

        //env.Define("self", Value.ObjectValue(this)); //вот с этим вся проблема!
        //я не могу запихнуть ModeObject в Value. Я не могу узнать, принадлежит ли env объекту или моде. Но я могу найти rootenv - это будет некоторый хак пока

        foreach ((string varname, Expr expr) in mode.logic.lets) env.Define(varname, expr.Eval(null, env)); //в lets часто бывают лямбды, так что я это пока не буду заменять на байткоды
    }
}
#endif
