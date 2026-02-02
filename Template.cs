using System.Numerics;

enum TTYPE { ARTIFACT, CREATURE, ZONE };
enum SHAPE { RECT, SPHERE, CYLINDER, CUBOID, CAPSULE, CONE, NONE };

class Template
{
    public string name, displayName;
    public Vector3 size, color;
    public float speed, maxhp, visionCos, hearingRad;
    public bool blocksVision;
    public TTYPE ttype;
    public SHAPE shape;

    public Template(string name, Expr xtemplate)
    {
        this.name = name;
        displayName = name;
        size = Vector3.One;
        color = new Vector3(1f, 0f, 1f);
        speed = 1f;
        maxhp = 1f;
        shape = SHAPE.SPHERE;

        for (int i = 0; i < xtemplate.children.Length; i++)
        {
            Expr xtop = xtemplate.children[i];

            if (xtop.head == "artifact" || xtop.head == "creature" || xtop.head == "zone")
            {
                ttype = xtop.head switch
                {
                    "artifact" => TTYPE.ARTIFACT,
                    "creature" => TTYPE.CREATURE,
                    "zone" => TTYPE.ZONE,
                    _ => throw new Exception($"cannot happen")
                };
                visionCos = ttype == TTYPE.CREATURE ? 0f : 2f;
                hearingRad = ttype == TTYPE.CREATURE ? 0.75f : 0f;
                if (ttype == TTYPE.CREATURE)
                {
                    //onDeath = Expr.Load("(reward -100)", false);
                    //policy = Expr.Load("(turn dir)", false);
                }
            }
            else if (xtop.head == "name") displayName = xtop.children[0].head;
            else if (xtop.head == "size")
            {
                if (xtop.children.Length == 3)
                {
                    size.X = float.Parse(xtop.children[0].head);
                    size.Y = float.Parse(xtop.children[1].head);
                    size.Z = float.Parse(xtop.children[2].head);
                    shape = SHAPE.CUBOID;
                }
                else if (xtop.children.Length == 2)
                {
                    size.X = float.Parse(xtop.children[0].head);
                    if (ttype == TTYPE.ZONE)
                    {
                        size.Y = float.Parse(xtop.children[1].head);
                        size.Z = Settings.ZONE_HEIGHT;
                        shape = SHAPE.CUBOID;
                    }
                    else
                    {
                        size.Y = size.X;
                        size.Z = float.Parse(xtop.children[1].head);
                        shape = size.Z > size.X && ttype == TTYPE.CREATURE ? SHAPE.CAPSULE : SHAPE.CYLINDER;
                    }
                }
                else if (xtop.children.Length == 1)
                {
                    float diameter = float.Parse(xtop.children[0].head);
                    size.X = diameter;
                    size.Y = diameter;
                    if (ttype == TTYPE.ZONE)
                    {
                        size.Z = Settings.ZONE_HEIGHT;
                        shape = SHAPE.CYLINDER;
                    }
                    else
                    {
                        size.Z = diameter;
                        shape = SHAPE.SPHERE;
                    }
                }
                else if (xtop.children.Length == 2 && xtemplate.head == "zone")
                {
                    throw new Exception("zones were deprecated");
                    size.X = float.Parse(xtop.children[0].head);
                    size.Y = float.Parse(xtop.children[1].head);
                    size.Z = Settings.ZONE_HEIGHT;
                    //const float c = 0.5f;
                    //if (size.X > c * game.size.X) size.X = c * game.size.X;
                    //if (size.Y > c * game.size.Y) size.Y = c * game.size.Y;
                }
                else throw new Exception($"wrong number of arguments in {name} size");
            }
            else if (xtop.head == "color") color = CH.VecColorFromHex(xtop.children[0].head);
            else if (xtop.head == "shape")
            {
                throw new Exception($"shape should be automatic in <{xtop}>");
                shape = xtop.children[0].head switch
                {
                    "RECT" => SHAPE.RECT,
                    "SPHERE" => SHAPE.SPHERE,
                    "CYLINDER" => SHAPE.CYLINDER,
                    "CUBOID" => SHAPE.CUBOID,
                    "CAPSULE" => SHAPE.CAPSULE,
                    "CONE" => SHAPE.CONE,
                    _ => throw new Exception($"unknown shape {xtop}")
                };
            }
            else if (xtop.head == "speed") speed = float.Parse(xtop.children[0].head);
            //else if (xtop.head == "seq" || xtop.head == "if" || xtop.head == "turn" || xtop.head == "go" || xtop.head == "goto" || xtop.head == "pressZ")
            //{
            //    if (policy != null) CS.WriteLine($"WARNING: Overwriting policy for {name}", ConsoleColor.DarkYellow);
            //    policybc = Compiler.Run(xtop, game);
            //}
            else if (xtop.head == "visionCos") visionCos = float.Parse(xtop.children[0].head);
            else if (xtop.head == "blocksVision") blocksVision = true;
            else if (xtop.head == "maxhp") maxhp = int.Parse(xtop.children[0].head);
            else throw new Exception($"unknown head <{xtop.head}> in template");
        }
    }

    public override string ToString() => name;
}
