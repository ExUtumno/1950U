class Script
{
    public byte[] data;
    public float[] floats;
    public string[] vars, strings;

    public const byte JMP = 0xfd, JMPF = 0xfe;

    public Script(byte[] data, float[] floats, string[] vars, string[] strings)
    {
        this.data = data;
        this.floats = floats;
        this.vars = vars;
        this.strings = strings;
    }

    public string Codon(int index)
    {
        byte opcode = data[index];
        byte opvariant = data[index + 1];
        byte numargs = data[index + 2];

        string result = "";
        if (opcode == JMP) result += $"JMP+{opvariant / 3}";
        else if (opcode == JMPF) result += $"JMPF+{opvariant / 3}";
        else
        {
            NODE id = (NODE)opcode;
            if (id == NODE.FLOAT) result += $"{floats[opvariant]}f";
            else if (id == NODE.STRING) result += $"\"{strings[opvariant]}\"";
            else if (id == NODE.TEMP) result += opvariant;
            else if (id == NODE.VAR) result += vars[opvariant];
            else result += $"{Node.nts[id]} {numargs}";
        }
        return result;
    }

    public override string ToString()
    {
        if (data.Length % 3 != 0) throw new Exception($"bytecode length {data.Length} should be divisible by 3");
        string result = "";
        for (int i = 0; i < data.Length; i += 3)
        {
            result += Codon(i);
            if (i + 3 < data.Length) result += ", ";
        }
        return result;
    }
}

class Compiler
{
    List<byte> datalist;
    List<float> floatlist;
    List<string> varlist, stringlist;

    List<Template> templates;

    Compiler()
    {
        datalist = [];
        floatlist = [];
        varlist = [];
        stringlist = [];
    }

    public static Script Run(Expr expr, List<Template> templates)
    {
        Compiler compiler = new();
        compiler.templates = templates;
        compiler.Compile(expr);
        return new Script(compiler.datalist.ToArray(), compiler.floatlist.ToArray(), compiler.varlist.ToArray(), compiler.stringlist.ToArray());
    }

    private void Compile(Expr expr)
    {
        if (expr.head == "if") //well, we actually have multichild if blocks. Но что-нибудь там можно придумать
        {
            Compile(expr.children[0]); ///compile condition            
            int jumpFalseIndex = Emit(Script.JMPF, 0, 0); ///emit "Jump if False" (We don't know where to yet, so use 0 placeholder)
            Compile(expr.children[1]); ///compile "Then" block            
            int jumpEndIndex = Emit(Script.JMP, 0, 0); ///emit "Jump" (To skip the Else block)

            PatchJump(jumpFalseIndex, datalist.Count); ///patch: now we know where the Else block starts
            if (expr.children.Length > 2) Compile(expr.children[2]); ///compile "Else" block
            PatchJump(jumpEndIndex, datalist.Count); ///patch: now we know where the End is
        }
        else if (expr.head == "seq")
        {
            foreach (var child in expr.children) Compile(child);
        }
        else if (expr.head == "=" || expr.head == "+=" || expr.head == "-=")
        {
            if (expr.children.Length != 2) throw new Exception($"weird = operator <{expr}>");
            Compile(expr.children[1]);
            varlist.Add(expr.children[0].head);
            NODE id = expr.head == "=" ? NODE.EQ : (expr.head == "+=" ? NODE.PEQ : NODE.MEQ);
            Emit((byte)id, (byte)(varlist.Count - 1), 1);
        }
        else
        {
            foreach (var child in expr.children) Compile(child); ///для EQ это уже не справедливо - потому что там только имя переменной, а не значение переменной

            byte opcode = 0xff;
            byte opvariant = 0xff;

            int templateIndex = templates.MyIndex(t => t.name == expr.head);

            if (float.TryParse(expr.head, out float f))
            {
                opcode = (byte)NODE.FLOAT;
                opvariant = (byte)floatlist.Count;
                floatlist.Add(f);
            }
            else if (Node.stn.TryGetValue(expr.head, out byte code))
            {
                //CS.WriteLine($"{expr.head} is a normal operation {code}", ConsoleColor.Cyan);
                opcode = code;
                opvariant = 0;
            }
            else if (templateIndex >= 0)
            {
                //CS.WriteLine($"{expr.head} is a TEMP", ConsoleColor.Cyan);
                opcode = (byte)NODE.TEMP;
                opvariant = (byte)templateIndex;
            }
            else if (!expr.isWord)
            {
                //CS.WriteLine($"{expr.head} is a STRING", ConsoleColor.Cyan);
                opcode = (byte)NODE.STRING;
                opvariant = (byte)stringlist.Count;
                stringlist.Add(expr.head);
            }
            else //тут тоже сохранять только уникальные имена
            {
                //CS.WriteLine($"{expr.head} is a VAR", ConsoleColor.Cyan);
                opcode = (byte)NODE.VAR;
                opvariant = (byte)varlist.Count;
                varlist.Add(expr.head);
            }
            if (opcode == 0xff || opvariant == 0xff) throw new Exception("should not happen, erroneous compiler logic");

            Emit(opcode, opvariant, (byte)expr.children.Length);
        }
    }

    private int Emit(byte opcode, byte variant, byte length)
    {
        int index = datalist.Count;
        datalist.Add(opcode);
        datalist.Add(variant);
        datalist.Add(length);
        return index; ///return address of the opcode
    }
    private void PatchJump(int instructionIndex, int targetIndex)
    {
        int jumpDistance = targetIndex - instructionIndex; ///спорное решение, нужно аккуратно обработать в Eval
        if (jumpDistance > 255) throw new Exception("jump too far for byte");
        datalist[instructionIndex + 1] = (byte)jumpDistance; ///указатель на ленту хранится в opvariant
    }
}
