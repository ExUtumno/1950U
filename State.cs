using System.Numerics;

struct KeyboardState
{
    public bool FORWARD, BACK, TURN_LEFT, TURN_RIGHT, STRAFE_LEFT, STRAFE_RIGHT, JUMP;
    public bool[] zkeys;

    public KeyboardState(bool FORWARD, bool BACK, bool TURN_LEFT, bool TURN_RIGHT, bool STRAFE_LEFT, bool STRAFE_RIGHT, bool JUMP, bool Z, bool X, bool C, bool V, bool B, bool N, bool M)
    {
        this.FORWARD = FORWARD;
        this.BACK = BACK;
        this.TURN_LEFT = TURN_LEFT;
        this.TURN_RIGHT = TURN_RIGHT;
        this.STRAFE_LEFT = STRAFE_LEFT;
        this.STRAFE_RIGHT = STRAFE_RIGHT;
        this.JUMP = JUMP;
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
        Vector2 shift = self.template.speed * Settings.SPEED * dir;
        self.position += new Vector3(shift, 0f);
    }

    static Vector2 SafeDir(Vector2 dir)
    {
        float lenSq = dir.LengthSquared();
        if (lenSq < 1e-6f) return Vector2.Zero;
        return dir / MathF.Sqrt(lenSq);
    }

    static float GroundZ(Object self) => 0.5f * self.size.Z;
    static bool IsOnGround(Object self) => self.position.Z <= GroundZ(self) + Settings.GROUND_EPS;
    static void ApplyJumpAndGravity(Object self, bool jumpPressed)
    {
        if (jumpPressed && IsOnGround(self))
        {
            self.velocity.Z = Settings.JUMP_SPEED;
        }

        self.velocity.Z -= Settings.GRAVITY;
        self.position.Z += self.velocity.Z;

        float groundZ = GroundZ(self);
        if (self.position.Z < groundZ)
        {
            self.position.Z = groundZ;
            if (self.velocity.Z < 0f) self.velocity.Z = 0f;
        }
    }

    public void Step(KeyboardState keyboard)
    {
        //CS.WriteLine($"\n{currentFrame}", ConsoleColor.DarkGray);
        currentFrame++;

        for (int i = 0; i < objects.Count; i++)
        {
            Object o = objects[i];
            //CS.WriteLine(o.ToString(), ConsoleColor.DarkGray);

            float rotspeed = Settings.ROTSPEED * o.template.speed;
            if (o.isPlayer)
            {
                //CS.WriteLine($"player is {o}", ConsoleColor.Gray);
                if (keyboard.TURN_RIGHT) o.dir = o.dir.Rotated(rotspeed);
                if (keyboard.TURN_LEFT) o.dir = o.dir.Rotated(-rotspeed);

                Vector2 move = Vector2.Zero;
                if (keyboard.FORWARD) move += o.dir;
                if (keyboard.BACK) move -= o.dir;
                if (keyboard.STRAFE_LEFT) move += o.dir.Left();
                if (keyboard.STRAFE_RIGHT) move += o.dir.Right();

                Vector2 moveDir = SafeDir(move);
                if (moveDir != Vector2.Zero) Go(o, moveDir);

                ApplyJumpAndGravity(o, keyboard.JUMP);
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
