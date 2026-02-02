using System.Numerics;

struct KeyboardState
{
    public bool UP, DOWN, LEFT, RIGHT, USE, DROP, THROW;
    public bool[] zkeys;

    public KeyboardState(bool UP, bool DOWN, bool LEFT, bool RIGHT, bool USE, bool DROP, bool THROW, bool Z, bool X, bool C, bool V, bool B, bool N, bool M)
    {
        this.UP = UP;
        this.DOWN = DOWN;
        this.LEFT = LEFT;
        this.RIGHT = RIGHT;
        this.USE = USE;
        this.DROP = DROP;
        this.THROW = THROW;
        zkeys = [Z, X, C, V, B, N, M];
    }

    public static KeyboardState None = new(false, false, false, false, false, false, false, false, false, false, false, false, false, false);
}

class State
{
    public Game game;
    public List<Object> objects, toDestroy;

    public int currentFrame;
    public Random random;

    public int uidCounter;
    public Dictionary<int, Object> uiddict; ///целочисленные указатели на игровые объекты

    public State(Game game, int seed)
    {
        this.game = game;
        objects = [];
        toDestroy = [];
        uiddict = [];
        random = new Random(seed);
        foreach (Script script in game.scripts) script.Execute(this, null);
    }

    public int NewUID()
    {
        uidCounter++;
        if (uidCounter > Settings.MAXUID) throw new Exception("MAXUID exceeded");
        return uidCounter - 1;
    }

    float BodyDistanceToClosestObject(Object self)
    {
        float min = 1000000f;
        for (int i = 0; i < objects.Count; i++)
        {
            Object o = objects[i];
            if (o == self) continue;
            float bodyDistance = GH.BodyDistance(o, self);
            if (bodyDistance < min) min = bodyDistance;
        }
        return min;
    }
    float BodyDistanceToScene(Object self)
    {
        (float minx, float miny) = self.MinXY();
        (float maxx, float maxy) = self.MaxXY();
        Vector2 sceneMin = -0.5f * game.size, sceneMax = 0.5f * game.size;
        return MathF.Min(MathF.Min(minx - sceneMin.X, sceneMax.X - maxx), MathF.Min(miny - sceneMin.Y, sceneMax.Y - maxy));
    }
    public void SetRandomFreePosition(Object self, int tries, float minAllowedDistance)
    {
        self.position = GH.RandomPosition(self.size, game.size, random);
        for (int t = 0; t < tries; t++)
        {
            float toObject = BodyDistanceToClosestObject(self);
            float toScene = BodyDistanceToScene(self);
            float distance = MathF.Min(toObject, toScene);

            if (distance >= minAllowedDistance) return;
            else self.position = GH.RandomPosition(self.size, game.size, random);
        }
    }

    public Object ClosestObject(Object self, Func<Object, bool> p, List<int> objs) //тут естественнее передавать список объектов
    {
        Object argmin = null;
        float minDistance = 1000f;
        for (int i = 0; i < objs.Count; i++)
        {
            int uid = objs[i];
            Object o = uiddict[uid];
            if (o == self) continue;
            if (p != null && !p(o)) continue;

            float distance = GH.BodyDistance(self, o);
            if (argmin != null && distance > minDistance) continue;

            if (distance < minDistance)
            {
                minDistance = distance;
                argmin = o;
            }
        }
        return argmin;
    }

    static void Go(Object self, Vector2 dir)
    {
        ///потом тут ещё будет участвовать система коллизий, и в итоге функция будет не-статичной
        Vector2 xy = self.position.XY() + self.template.speed * Settings.SPEED * dir;
        self.position += new Vector3(xy, 0f);
    }

    public void Step(KeyboardState keyboard)
    {
        CS.WriteLine($"\n{currentFrame}", ConsoleColor.DarkGray);
        currentFrame++;

        for (int i = 0; i < objects.Count; i++)
        {
            Object o = objects[i];
            CS.WriteLine(o.ToString(), ConsoleColor.DarkGray);

            float rotspeed = Settings.ROTSPEED * o.template.speed;
            if (o.isPlayer)
            {
                CS.WriteLine($"player is {o}", ConsoleColor.Gray);
                if (keyboard.RIGHT) o.dir = o.dir.Rotated(rotspeed);
                if (keyboard.LEFT) o.dir = o.dir.Rotated(-rotspeed);

                if (keyboard.UP) Go(o, o.dir);
                else if (keyboard.DOWN) Go(o, -o.dir);
            }
            else
            {
                throw new Exception("policies not implemented yet");
            }
        }
    }

    /*public float SDF(Vector2 p, Object self) //ага, это не BodyDistance. Тут self наоборот исключается
    {
        float min = game.SceneSDF(p);
        if (min <= 0f) return min;
        for (int i = 0; i < objects.Count; i++)
        {
            Object o = objects[i];
            if (o == self) continue;
            float sdf = Object.BodyDistance(o, p);
            if (sdf <= 0f) return sdf;
            if (sdf < min) min = sdf;
        }
        return min;
    }
    public bool CollidesWithAnything(Object self) //эту функцию выразить через предыдущую, конечно. Всё согласовано
    {
        if (game.SceneSDF(self) <= 0) return true;
        for (int i = 0; i < objects.Count; i++)
        {
            Object o = objects[i];
            if (o.uid == self.uid) continue;
            if (Object.Near(o, self)) return true;
        }
        return false;
    }*/
}
