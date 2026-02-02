using System.Numerics;

static class ByteCodeHelper
{
    public static Value Execute(this Script bytecode, State state, Env env)
    {
        //CS.WriteLine($"execute: {bytecode}", ConsoleColor.Blue);
        if (bytecode.data.Length % 3 != 0) throw new Exception($"bytecode length {bytecode.data.Length} should be divisible by 3");

        Stack<Value> stack = new();
        for (int index = 0; index < bytecode.data.Length;)
        {
            byte opcode = bytecode.data[index];
            byte opvariant = bytecode.data[index + 1];
            byte numargs = bytecode.data[index + 2];

            if (opcode == Script.JMP) index += opvariant;
            else if (opcode == Script.JMPF)
            {
                Value value = stack.Pop();
                index += value.GetBool() ? 3 : opvariant;
            }
            else
            {
                NODE id = (NODE)opcode;
                if (id == NODE.FLOAT)
                {
                    float f = bytecode.floats[opvariant];
                    Value value = Value.FloatValue(f);
                    stack.Push(value);
                }
                else if (id == NODE.STRING) throw new Exception($"strings are not implemented yet");
                else if (id == NODE.TEMP)
                {
                    Template template = state.game.templates[opvariant];
                    stack.Push(Value.TemplateValue(template));
                }
                else if (id == NODE.VAR) ///нужно эту переменную найти в энвайронменте и положить в стек
                {
                    string varname = bytecode.vars[opvariant];
                    Value value = env.Get(varname);
                    stack.Push(value);
                }
                ///это инструкция присваивания
                else if (id == NODE.EQ) ///не является общим случаем, потому что есть opvariant, а в общем случае его нет
                {
                    throw new Exception("assignments not implemented yet");
                    if (numargs != 1) throw new Exception($"wrong number {numargs} of arguments in {Node.nts[id]}");

                    string varname = bytecode.vars[opvariant];
                    Value value = stack.Pop();

                    bool hasself = env.Has("self", out Value selfValue);
                    if (hasself)
                    {
                        Object self = selfValue.GetObject(state);
                        //self.env.Set(varname, value);!!!
                    }
                    else env.Root().Set(varname, value); ///this is used only for modes, it's a hack. В идеале я бы хотел иметь сплошной стек
                }
                else if (id == NODE.PEQ || id == NODE.MEQ)
                {
                    if (numargs != 1) throw new Exception($"wrong number {numargs} of arguments in {Node.nts[id]}");

                    string varname = bytecode.vars[opvariant];
                    float increment = stack.Pop().GetFloat();
                    float currentValue = env.Get(varname).GetFloat();
                    float newValue = id == NODE.PEQ ? currentValue + increment : currentValue - increment;

                    env.Set(varname, Value.FloatValue(newValue));
                }
                else
                {
                    Value[] args = new Value[numargs];
                    for (int i = args.Length - 1; i >= 0; i--) args[i] = stack.Pop();
                    Value value = ExecuteNode(id, args, state, env);
                    if (!Value.Same(value, Value.VOID)) stack.Push(value); //наверняка можно сделать более изящно
                }

                //CS.WriteLine($"executed {bytecode.Codon(index)}", ConsoleColor.Red);
                //CS.WriteLine("Current stack:", ConsoleColor.DarkRed);
                //foreach (Value v in stack) CS.WriteLine($"{v} ", ConsoleColor.DarkRed);

                index += 3;
            }
        }

        if (stack.Count == 1) return stack.Pop();
        else if (stack.Count == 0) return Value.PASS;
        else throw new Exception($"strange, stack.Count = {stack.Count}");
    }

    static Value ExecuteNode(NODE id, Value[] args, State state, Env env)
    {
        string nodename = Node.nts[id];
        switch (id)
        {
            case NODE.PLAYER:
                {
                    if (args.Length != 1) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    args[0].GetObject(state).isPlayer = true;
                    return Value.VOID;
                }
            case NODE.DAMAGE: ///(damage target 1 self)
                {
                    if (args.Length != 3) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");

                    Object target = args[0].GetObject(state);
                    float damage = args[1].GetFloat();
                    Object source = args[2].GetObject(state);

                    target.hp -= damage;
                    if (target.hp <= 0) state.toDestroy.Add(target);
                    return Value.VOID; //we don't have to return VOID here - make a file search
                }
            case NODE.DESTROY:
                {
                    if (args.Length != 1) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    Object target = args[0].GetObject(state);
                    state.toDestroy.Add(target);
                    return Value.VOID;
                }
            case NODE.CREATE:
                {
                    if (args.Length == 0) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    if (state.uidCounter >= Settings.MAXUID) return Value.NONE;

                    Template template = args[0].GetTemplate();
                    int amount = state.objects.Where(o => o.template == template).Count();
                    string finalName = amount == 0 ? template.displayName : $"{template.displayName} {amount + 1}";

                    Object o = new(template, finalName, state);

                    state.SetRandomFreePosition(o, 1000, o.template.shape == SHAPE.CUBOID ? 0f : Settings.NEAR_DISTANCE + 0.01f);
                    o.dir = VH.Unit(state.random.NextSingle());
                    state.objects.Add(o);
                    return Value.ObjectValue(o);
                }
            case NODE.REWARD:
                {
                    Object o;
                    float reward;

                    if (args.Length == 0)
                    {
                        o = env.Get("self").GetObject(state);
                        reward = 1f;
                    }
                    else if (args.Length == 1)
                    {
                        o = env.Get("self").GetObject(state);
                        reward = args[^1].GetFloat();
                    }
                    else if (args.Length == 2)
                    {
                        o = args[0].GetObject(state);
                        reward = args[^1].GetFloat();
                    }
                    else throw new Exception($"wrong number {args.Length} of arguments in {nodename}");

                    o.score += reward; //state.Reward(o, reward);
                    return Value.VOID;
                }
            case NODE.TURN:
                if (args.Length != 1) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                return Value.TupleValue(ACTION.TURN, args[0].GetVector());
            case NODE.GO:
                {
                    Vector2 v;

                    if (args.Length == 0) v = env.Get("self").GetObject(state).dir;
                    else if (args.Length == 1) v = args[0].GetVector();
                    else throw new Exception($"wrong number {args.Length} of arguments in {nodename}");

                    return Value.TupleValue(v == Vector2.Zero ? ACTION.TURN : ACTION.GO, v);
                }
            case NODE.BACK:
                if (args.Length != 0) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                return Value.TupleValue(ACTION.BACK, Vector2.Zero);
            case NODE.GOTO:
                {
                    if (args.Length != 1) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");

                    Object self = env.Get("self").GetObject(state);
                    Value goalValue = args[0];
                    if (goalValue.type == VTYPE.OBJ)
                    {
                        Object goal = goalValue.GetObject(state);
                        if (goal == null) return Value.PASS;
                        else
                        {
                            Vector2 v = (goal.position - self.position).XY();
                            return Value.TupleValue(v == Vector2.Zero || !self.CanReach(goal) ? ACTION.TURN : ACTION.GO, v);
                        }
                    }
                    else if (goalValue.type == VTYPE.VECTOR) //странно, но это же работало нормально. А, пардон, позиция же возвращает отнсительные координаты? Я так специально задизайнил
                    {
                        Vector2 toGoal = goalValue.GetVector();
                        Vector2 goal = self.position.XY() + toGoal;
                        return Value.TupleValue(GH.BodyDistance(self, goal) <= 0f || !self.CanReach(goal) ? ACTION.TURN : ACTION.GO, toGoal);
                    }
                    else throw new Exception($"unknown goal type in {nodename}");
                }
            /*case NODE.USE:
                {
                    if (args.Length != 0) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    Object self = env.Get("self").GetObject(state);                 
                    return self.template.onUse.Execute(state, env);
                }*/
            case NODE.FALSE: return Value.FALSE;
            case NODE.TRUE: return Value.TRUE;
            case NODE.NOT: return args[0].GetBool() ? Value.FALSE : Value.TRUE;
            case NODE.EEQ:
            case NODE.NEQ:
                if (args.Length != 2) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                return (id == NODE.EEQ) == Value.Same(args[0], args[1]) ? Value.TRUE : Value.FALSE;
            case NODE.NEAR:
                {
                    if (args.Length != 2) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    Value v1 = args[0];
                    Value v2 = args[1];
                    if (v1.type == VTYPE.OBJ && v2.type == VTYPE.OBJ)
                    {
                        Object o1 = v1.GetObject(state);
                        Object o2 = v2.GetObject(state);
                        if (o1 == null || o2 == null) return Value.FALSE;
                        return GH.Near(o1, o2) ? Value.TRUE : Value.FALSE;
                    }
                    else if (v1.type == VTYPE.OBJ && v2.type == VTYPE.VECTOR)
                    {
                        Object o = v1.GetObject(state);
                        if (o == null) return Value.FALSE;
                        Vector2 v = v2.GetVector();
                        return GH.Near(o, v) ? Value.TRUE : Value.FALSE;
                    }
                    else throw new Exception("not implemented yet");
                }
            case NODE.AND:
                {
                    for (int i = 0; i < args.Length - 1; i++) if (!args[i].GetBool()) return Value.FALSE;
                    return args[^1];
                }
            case NODE.OR:
                {
                    for (int i = 0; i < args.Length - 1; i++) if (args[i].GetBool()) return Value.TRUE;
                    return args[^1];
                }
            case NODE.LESS:
            case NODE.LESSEQ:
            case NODE.GR:
            case NODE.GREQ:
                {
                    float l = args[0].GetFloat();
                    float r = args[1].GetFloat();
                    bool b = id switch
                    {
                        NODE.LESS => l < r,
                        NODE.LESSEQ => l <= r,
                        NODE.GR => l > r,
                        NODE.GREQ => l >= r,
                        _ => throw new Exception("cannot happen")
                    };
                    return b ? Value.TRUE : Value.FALSE;
                }
            case NODE.SEES:
                {
                    Object o0, o1;
                    if (args.Length == 1)
                    {
                        o0 = env.Get("self").GetObject(state);
                        o1 = args[0].GetObject(state);
                    }
                    else if (args.Length == 2)
                    {
                        o0 = args[0].GetObject(state);
                        o1 = args[1].GetObject(state);
                    }
                    else throw new Exception($"wrong number {args.Length} of arguments in {nodename}");

                    if (o0 == null || o1 == null) return Value.FALSE;
                    if (o0.template.ttype != TTYPE.CREATURE) return Value.FALSE;

                    Vector2 v = Vector2.Normalize((o1.position - o0.position).XY());
                    if (Vector2.Dot(o0.dir, v) < o0.template.visionCos) return Value.FALSE;
                    Object hit = Graphics.FilteredBodyCast(o0, v, state).o;
                    return hit == o1 ? Value.TRUE : Value.FALSE;
                }
            case NODE.DOTPRODUCT:
                {
                    if (args.Length != 2) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    Vector2 v1 = args[0].GetVector();
                    Vector2 v2 = args[1].GetVector();
                    return Value.FloatValue(Vector2.Dot(v1, v2));
                }
            case NODE.PLUS:
            case NODE.MINUS:
            case NODE.MULT:
            case NODE.DIV:
                {
                    Value lv = args[0];
                    Value rv = args[1];
                    if (lv.type == VTYPE.FLOAT && rv.type == VTYPE.FLOAT)
                    {
                        float result = lv.GetFloat();
                        for (int i = 1; i < args.Length; i++)
                        {
                            float r = (i == 1 ? rv : args[i]).GetFloat();
                            result = id switch
                            {
                                NODE.PLUS => result + r,
                                NODE.MINUS => result - r,
                                NODE.MULT => result * r,
                                NODE.DIV => result / r,
                                _ => throw new Exception("cannot happen")
                            };
                        }
                        return Value.FloatValue(result);
                    }
                    else if (lv.type == VTYPE.VECTOR && rv.type == VTYPE.VECTOR)
                    {
                        Vector2 l = lv.GetVector();
                        Vector2 r = rv.GetVector();
                        Vector2 result = id switch
                        {
                            NODE.PLUS => l + r,
                            NODE.MINUS => l - r,
                            _ => throw new Exception("cannot multiply or divide vectors")
                        };
                        return Value.VectorValue(result);
                    }
                    else throw new Exception("wrong typing in an arithmetic operator");
                }
            case NODE.DISTANCE:
                {
                    if (args.Length != 2) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    Object o1 = args[0].GetObject(state);
                    Object o2 = args[1].GetObject(state);
                    return Value.FloatValue(o1 == null || o2 == null ? 0f : VH.FlatDistance(o1.position, o2.position));
                }
            case NODE.RAYDIST:
                {
                    if (args.Length < 1 || args.Length > 2) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    float maxDist = args.Length == 2 ? args[1].GetFloat() : 10000f;

                    Object self = env.Get("self").GetObject(state);
                    Vector2 v = args[0].GetVector();

                    Object equipped = null; //self.equippedUID < 0 ? null : state.uiddict[self.equippedUID];
                    List<Object> objects = [];
                    for (int i = 0; i < state.objects.Count; i++)
                    {
                        Object o = state.objects[i];
                        if (o != self && o != equipped && o.template.blocksVision) objects.Add(o);
                    }

                    float dist = MathF.Min(maxDist, Graphics.BodyCast(self, v, objects, state.game.size).t);
                    return Value.FloatValue(dist);
                }
            case NODE.GAP:
                {
                    if (args.Length != 1) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");

                    Vector2 v = Vector2.Normalize(args[0].GetVector());
                    Object self = env.Get("self").GetObject(state);

                    const float DA = MathF.Tau / (2 * Settings.NUM_RAYS);
                    Vector2 l = v.Rotated(-DA);
                    Vector2 r = v.Rotated(DA);

                    Object equipped = null; //self.equippedUID < 0 ? null : state.uiddict[self.equippedUID];
                    List<Object> objects = state.objects.Where(o => o != self && o != equipped).ToList();

                    float distv = Graphics.BodyCast(self, v, objects, state.game.size).t;
                    float distl = Graphics.BodyCast(self, l, objects, state.game.size).t;
                    float distr = Graphics.BodyCast(self, r, objects, state.game.size).t;

                    float gap = MathF.Max(MathF.Abs(distv - distl), MathF.Abs(distv - distr));
                    return Value.FloatValue(gap);
                }
            case NODE.SELF: return env.Get("self");
            case NODE.NONE: return Value.NONE;
            case NODE.DIR:
                {
                    if (args.Length == 0)
                    {
                        Object self = env.Get("self").GetObject(state);
                        return Value.VectorValue(self.dir);
                    }
                    else if (args.Length == 1)
                    {
                        Object o = args[0].GetObject(state);
                        return o == null ? Value.ZEROVEC : Value.VectorValue(o.dir);
                    }
                    else throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                }
            case NODE.LEFT:
            case NODE.RIGHT:
                {
                    if (args.Length == 0)
                    {
                        Vector2 z = env.Get("self").GetObject(state).dir;
                        return Value.VectorValue(id == NODE.LEFT ? z.Left() : z.Right());
                    }
                    else if (args.Length == 1)
                    {
                        Vector2 z = args[0].GetVector();
                        return Value.VectorValue(id == NODE.LEFT ? z.Left() : z.Right());
                    }
                    else throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                }
            case NODE.TO:
                {
                    if (args.Length != 1) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");

                    Object self = env.Get("self").GetObject(state);
                    Object goal = args[0].GetObject(state);

                    if (goal == null /*|| goal.uid == self.equippedUID*/) return Value.ZEROVEC;

                    Vector2 v = (goal.position - self.position).XY(); ///просто возвращаем разницу координат
                    if (v.Length() < 0.01f) return Value.ZEROVEC;
                    return Value.VectorValue(v);
                }
            case NODE.FROM: //надо бы слить в одну функцию с TO. Но это лучше делать в основном проекте
                {
                    if (args.Length != 1) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");

                    Object self = env.Get("self").GetObject(state);
                    Object goal = args[0].GetObject(state);

                    if (goal == null /*|| goal.uid == self.equippedUID*/) return Value.ZEROVEC;

                    Vector2 v = (goal.position - self.position).XY();
                    float length = v.Length();
                    if (length < 0.01f) return Value.VectorValue(self.dir); ///важное отличие от TO: если мы уже стоим на месте цели, то нужно просто продолжать идти в текущем направлении
                    return Value.VectorValue(-v / length); ///а тут мы возвращаем не просто разницу, а норимированную разницу. И вроде как это сделано специально
                }
            case NODE.RANDOM_DIRECTION:
                {
                    float angle = 2f * MathF.PI * state.random.NextSingle();
                    Vector2 vec = new(MathF.Cos(angle), MathF.Sin(angle));
                    return Value.VectorValue(vec);
                }
            case NODE.TEMPLATE:
                {
                    if (args.Length != 1) throw new Exception($"wrong number {args.Length} of arguments in {nodename}");
                    Object o = args[0].GetObject(state);
                    if (o == null) throw new Exception("cannot be null");
                    return Value.TemplateValue(o.template);
                }
            default:
                throw new Exception($"unknown node <{nodename}>");
        }
    }
}
