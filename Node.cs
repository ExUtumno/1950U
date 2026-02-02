/*
Тут остаётся только кодировать все мои ноды. А грамматика будет задана ниже. Значит ли это, что можно обойтись одним типом выражений? Точно нет.

Нужно соответствие между энамами и именами. Проще построить 2 словаря, я думаю.
 

 
*/



enum NODE
{
    FLOAT, STRING, TEMP, VAR, IGNORE,
    LET, SEQ, EQ, PEQ, MEQ, DOT, FOR, FOREACH, RANDOM, SETSIZE, /*DO,*/ EQUIP, DAMAGE, POLICY, SPEED, PRINT, PLAYER, MODE, ///special
    PUTON, DROP, DESTROY, THROW, CREATE, POSITION, STUN, REWARD, /*BUMP,*/ ///return Void
    TURN, GO, BACK, GOTO, IF, USE, ///return Tuple
    FALSE, TRUE, COIN, NOT, EEQ, NEQ, NEAR, HAS_VELOCITY, AND, OR, LESS, LESSEQ, GR, GREQ, SEES, IS_RESOURCE, IS_CREATURE, PROXIMITY_SENSOR, WHISKER_FORWARD, WHISKER_LEFT, WHISKER_RIGHT, ///return Bool
    HEX, ///return Int
    NUM, DOTPRODUCT, ALIGNMENT, PLUS, MINUS, MULT, DIV, POWER, UNIFORM, AREA, X, Y, MASS, DISTANCE, RAYDIST, GAP, SQUARE, ///return Float
    SELF, NONE, EQUIPPED, HOLDER, CLOSEST, BEAMCAST, ///return Object
    VEC, DIR, LEFT, RIGHT, TO, FROM, MAX_DIRECTION, BEST_DIRECTION, AVERAGE_DIRECTION, RANDOM_DIRECTION, POS, SCENE_MIN, SCENE_SIZE, MIN, MAX, SIZE, CONE_CLAMP, ///return Vector2
    TEMPLATE, NOTHING, ///return Template
    ALL, ALL_VISIBLE, ///return List
};

class Node ///GraphNode - это по сути NODEID вместе с информацией о инпутах и оутпутах (чтобы знать, как композировать в перебое выражений)
{
    public NODE id;
    public string name;
    public int otype;
    public int[] itypes;

    public static Dictionary<NODE, string> nts = new()
    {
        [NODE.IGNORE] = "ignore",
        [NODE.LET] = "let",
        [NODE.SEQ] = "seq",
        [NODE.EQ] = "=",
        [NODE.PEQ] = "+=",
        [NODE.MEQ] = "-=",
        [NODE.DOT] = "dot",
        [NODE.FOR] = "for",
        [NODE.FOREACH] = "foreach",
        [NODE.RANDOM] = "random",
        [NODE.SETSIZE] = "setsize",
        //[NODE.DO] = "do",
        [NODE.EQUIP] = "equip",
        [NODE.DAMAGE] = "damage",
        [NODE.POLICY] = "policy",
        [NODE.SPEED] = "speed",
        [NODE.PRINT] = "print",
        [NODE.PLAYER] = "player",
        [NODE.MODE] = "mode",

        ///return Void
        [NODE.PUTON] = "putOn",
        [NODE.DROP] = "drop",
        [NODE.DESTROY] = "destroy",
        [NODE.THROW] = "throw",
        [NODE.CREATE] = "create",
        [NODE.POSITION] = "position",
        [NODE.STUN] = "stun",
        [NODE.REWARD] = "reward",
        //[NODE.BUMP] = "bump",

        ///return Tuple
        [NODE.TURN] = "turn",
        [NODE.GO] = "go",
        [NODE.BACK] = "back",
        [NODE.GOTO] = "goto",
        [NODE.IF] = "if",
        [NODE.USE] = "use",

        ///return Bool
        [NODE.FALSE] = "false",
        [NODE.TRUE] = "true",
        [NODE.COIN] = "coin",
        [NODE.NOT] = "not",
        [NODE.EEQ] = "==",
        [NODE.NEQ] = "!=",
        [NODE.NEAR] = "near",
        [NODE.HAS_VELOCITY] = "hasVelocity",
        [NODE.AND] = "and",
        [NODE.OR] = "or",
        [NODE.LESS] = "<",
        [NODE.LESSEQ] = "<=",
        [NODE.GR] = ">",
        [NODE.GREQ] = ">=",
        [NODE.SEES] = "sees",
        [NODE.IS_RESOURCE] = "isResource",
        [NODE.IS_CREATURE] = "isCreature",
        [NODE.PROXIMITY_SENSOR] = "proximitySensor",
        [NODE.WHISKER_FORWARD] = "wf",
        [NODE.WHISKER_LEFT] = "wl",
        [NODE.WHISKER_RIGHT] = "wr",
        //[NODE.COLLIDES] = "collides",

        [NODE.HEX] = "hex",

        ///return Float
        [NODE.NUM] = "num",
        [NODE.DOTPRODUCT] = "dotproduct",
        [NODE.ALIGNMENT] = "alignment",
        [NODE.PLUS] = "+",
        [NODE.MINUS] = "-",
        [NODE.MULT] = "*",
        [NODE.DIV] = "/",
        [NODE.POWER] = "^",
        [NODE.UNIFORM] = "uniform",
        [NODE.AREA] = "area",
        [NODE.X] = "x",
        [NODE.Y] = "y",
        [NODE.MASS] = "mass",
        [NODE.DISTANCE] = "distance",
        [NODE.RAYDIST] = "raydist",
        //[NODE.OPENNESS] = "openness",
        [NODE.GAP] = "gap",
        [NODE.SQUARE] = "square",

        ///return Object
        [NODE.SELF] = "self",
        [NODE.NONE] = "none",
        [NODE.EQUIPPED] = "equipped",
        [NODE.HOLDER] = "holder",
        [NODE.CLOSEST] = "closest",
        [NODE.BEAMCAST] = "beamcast",

        ///return Vector2
        [NODE.VEC] = "vec",
        [NODE.DIR] = "dir",
        [NODE.LEFT] = "left",
        [NODE.RIGHT] = "right",
        [NODE.TO] = "to",
        [NODE.FROM] = "from",
        [NODE.MAX_DIRECTION] = "maxDirection",
        [NODE.BEST_DIRECTION] = "bestDirection",
        //[NODE.OPEN_DIRECTION] = "openDirection",
        [NODE.AVERAGE_DIRECTION] = "averageDirection",
        [NODE.RANDOM_DIRECTION] = "randomDirection",
        [NODE.POS] = "pos",
        [NODE.SCENE_MIN] = "scenemin",
        [NODE.SCENE_SIZE] = "scenesize",
        [NODE.MIN] = "min",
        [NODE.MAX] = "max",
        [NODE.SIZE] = "size",
        [NODE.CONE_CLAMP] = "coneClamp",

        [NODE.TEMPLATE] = "template",
        [NODE.NOTHING] = "nothing",

        [NODE.ALL] = "all",
        [NODE.ALL_VISIBLE] = "allVisible",
    };
    public static Dictionary<string, byte> stn;

    static Node()
    {
        stn = [];
        foreach (var pair in nts) stn.Add(pair.Value, (byte)pair.Key);
    }

    Node(NODE id, string ostr, string istr)
    {
        this.id = id;
        if (ostr.Length != 1) throw new Exception($"a node should have exactly one output, not {ostr.Length}");
        otype = (int)TH.dict[ostr[0]];
        itypes = istr.Select(c => (int)TH.dict[c]).ToArray();
        name = nts[id];
    }
    Node(string sensorName, VTYPE otype)
    {
        id = NODE.VAR; //hope it's correct
        this.otype = (int)otype;
        itypes = [];
        name = sensorName;
    }

    public Node(string templateName, bool func)
    {
        if (func) ///(Cheese beamcast)
        {
            id = NODE.TEMP; //думаю, что это неважно здесь
            otype = (int)VTYPE.BOOL;
            itypes = [(int)VTYPE.OBJ];
            name = templateName;
        }
        else ///(closest Cheese a) <- в идеале нужно сделать (Cheese #0), и тогда эти штуки можно объединить, но будет непонятно с перебором выражений тогда
        {
            id = NODE.TEMP;
            otype = (int)VTYPE.TEMPLATE;
            itypes = [];
            name = templateName;
        }
    }

    public VTYPE OType() => (VTYPE)otype;

    static Node[] basicNodes =
    [
        new Node(NODE.TURN, "u", "v"),
        new Node(NODE.GO, "u", "v"),
        new Node(NODE.GOTO, "u", "o"),

        new Node(NODE.EEQ, "b", "oo"),
        new Node(NODE.NEAR, "b", "oo"),
        new Node(NODE.LESS, "b", "ff"),

        new Node(NODE.DISTANCE, "f", "oo"),

        new Node(NODE.SELF, "o", ""),
        new Node(NODE.NONE, "o", ""),

        new Node(NODE.DIR, "v", "o"),
        new Node(NODE.LEFT, "v", "v"),
        new Node(NODE.RIGHT, "v", "v"),
        //new Node(NODE.RANDOM_DIRECTION, "v", ""),
        //new Node(NODE.AVERAGE_DIRECTION, "v", ""),
        new Node(NODE.TO, "v", "o"),
        new Node(NODE.FROM, "v", "o"),
    ];
    static Node[] logicNodes =
    [
        new Node(NODE.FALSE, "b", ""),
        new Node(NODE.TRUE, "b", ""),
        new Node(NODE.IF, "u", "buu"),
        new Node(NODE.NOT, "b", "b"),
    ];
    static Node[] arithmeticNodes =
    [
        new Node(NODE.MINUS, "v", "vv"),
    ];
    public static Node[] sensorNodes =
    [
        new Node(NODE.RAYDIST, "f", "v"),
        new Node(NODE.SEES, "b", "o"),
    ];

    ///state передавать обязательно, чтобы можно было эвалюировать сенсоры - и понять, какого они типа. Без эвалюации выражения тип определить сложно, к сожалению
    public static Node[] Grammar(bool addArithmeticNodes, bool addLogicNodes, Template agentTemplate, State state, NODE[] bannedIDs)
    {
        if (agentTemplate == null) throw new Exception("agentTemplate should not be null");

        List<Node> result = [];
        result.AddRange(basicNodes);
        if (addArithmeticNodes) result.AddRange(arithmeticNodes);
        if (addLogicNodes) result.AddRange(logicNodes);

        foreach (NODE id in bannedIDs)
        {
            Node node = result.FirstOrDefault(node => node.id == id);
            if (node != null) result.Remove(node);
        }

        Object agent = state.objects.FirstOrDefault(o => o.template == agentTemplate);
        if (agent == null) throw new Exception("strange that there is no agent");
        /*foreach (Node node in agentTemplate.functions) result.Add(node);
        foreach (var (sensorName, sensorExpr) in agentTemplate.sensors)
        {
            Value v = sensorExpr.Eval(state, agent.env);
            Node node = new(sensorName, v.type);
            result.Add(node);
        }*/
        return result.ToArray();
    }

    public static string GrammarString(Node[] grammar)
    {
        string result = "";
        foreach (Node node in grammar)
        {
            string name = nts[node.id];
            if (node.itypes.Length == 0) result += $"{TH.typeNames[node.otype]} ::= {name}";
            else
            {
                string join = string.Join(' ', node.itypes.Select(t => TH.typeNames[t]));
                result += $"{TH.typeNames[node.otype]} ::= ({name} {join})";
            }
            result += '\n';
        }
        return result;
    }

    public override string ToString()
    {
        if (id == NODE.VAR) return name;
        else return nts[id];
    }
}
