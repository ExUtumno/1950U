using System.Numerics;

///хранит общую информацию об игре плюс текущее состояние
class Game
{
    public Vector2 size;
    public List<Template> templates;
    public List<Script> scripts; ///scripts that run at the start of the game

    public Game(Expr xgame)
    {
        size = new Vector2(16, 16);
        templates = [];

        ///это делается для того, чтобы подгрузить объектные файлы
        foreach (Expr xcreate in xgame.Descendants("create"))
        {
            for (int i = 0; i < xcreate.children.Length; i++) ///for cases like (create Button Cheese) - but need to be careful with cases like (create Wall 0.5 4.5)
            {
                string templateName = xcreate.children[i].head;
                if (float.TryParse(templateName, out _)) continue; ///мы не можем пока ничего эвалюейтить, потому что у нас ещё нет ни state, ни game

                if (templates.FirstOrDefault(t => t.name == templateName) != null) continue;
                
                string filepath = $"objects/{templateName}.lisp";
                if (!File.Exists(filepath)) throw new Exception($"file <{filepath}> does not exist");
                string filetext = File.ReadAllText(filepath);
                Expr xtemplate = Expr.Load($"(ROOT {filetext})");

                Template template = new(templateName, xtemplate);
                templates.Add(template);
            }
        }

        for (int t = 0; t < templates.Count; t++)
        {
            Template template = templates[t];
            if (template.color.Same(1f, 0f, 1f)) template.color = CH.VecColorFromInt(CH.PICO_NOBLACK[(t + 2) % CH.PICO_NOBLACK.Length]);
        }

        ///мы не можем их сразу выполнить, потому что у нас нет состояния
        scripts = [];
        foreach (Expr x in xgame.children)
        {
            Script script = Compiler.Run(x, templates);
            scripts.Add(script);
        }
    }
}
