using System.Numerics;

class Object
{
    public Template template;
    public Vector3 position, size, velocity, color;
    public Vector2 dir;
    public string name;
    public float hp, score;
    public int uid; ///нужны для values и для системы коллизий (чтобы делать логарифмический поиск)
    public bool isPlayer;

    //public Env env; //у каждого объекта должна быть кастомная память. Но можно попробовать её организовать без странных вложенных конструкций

    public Object(Template template, string name, State state)
    {
        this.template = template;
        this.name = name;
        size = template.size;
        color = template.color;
        dir = Vector2.One;
        hp = template.maxhp;

        uid = state.NewUID();
        state.uiddict.Add(uid, this);

        //result.env = new(state.env);
        //result.env.Define("self", Value.ObjectValue(result));
        //foreach ((string varname, Expr let) in template.logic.lets) result.env.Define(varname, let.Eval(state, result.env));
    }

    public Vector3 Min() => position - 0.5f * size;
    public Vector3 Max() => position + 0.5f * size;
    public float MinZ() => position.Z - 0.5f * size.Z;
    public float MaxZ() => position.Z + 0.5f * size.Z;
    public (float, float) MinXY() => (position.X - 0.5f * size.X, position.Y - 0.5f * size.Y);
    public (float, float) MaxXY() => (position.X + 0.5f * size.X, position.Y + 0.5f * size.Y);

    public float Area() => size.X * size.Y;
    public float Volume()
    {
        if (template.shape == SHAPE.CUBOID) return size.X * size.Y * size.Z;
        else if (template.shape == SHAPE.SPHERE)
        {
            float R = size.AverageRadius();
            return 4f / 3f * MathF.PI * R * R * R;
        }
        else if (template.shape == SHAPE.CYLINDER)
        {
            float R = size.AverageRadius2D();
            return MathF.PI * R * R * size.Z;
        }
        else if (template.shape == SHAPE.CAPSULE)
        {
            float R = size.AverageRadius2D();
            return MathF.PI * R * R * (size.Z - 2f / 3f * R);
        }
        else throw new Exception($"unknown shape <{template.shape}>");
    }
    public float LinearSize() => (size.X + size.Y + size.Z) / 3.0f;

    public override string ToString() => name;
    public static string MyString(Object o) => o == null ? "none" : o.name;

    //эти 2 функции должны вызывать одну общую приватную функцию, просто с разным радиусом
    public bool CanReach(Vector2 goal)
    {
        float R = Settings.SPEED / Settings.ROTSPEED; ///делить на template.speed не нужно, если скорость поворота тоже пропорциональна базовой скорости
        float RSmall = R - size.AverageRadius2D();
        if (RSmall <= 0f) return true;

        Vector2 v = goal - position.XY();
        Vector2 ort = R * dir.Left();
        return ((v + ort).LengthSquared() >= RSmall * RSmall) && ((v - ort).LengthSquared() >= RSmall * RSmall);
    }
    public bool CanReach(Object goal)
    {
        float R = Settings.SPEED / Settings.ROTSPEED; ///делить на template.speed не нужно, если скорость поворота тоже пропорциональна базовой скорости
        float RSmall = R - (size.AverageRadius2D() + goal.size.AverageRadius2D());
        if (RSmall <= 0f) return true;

        Vector2 v = goal.position.XY() - position.XY();
        Vector2 ort = R * dir.Left();
        return ((v + ort).LengthSquared() >= RSmall * RSmall) && ((v - ort).LengthSquared() >= RSmall * RSmall);
    }
}
