using System.Numerics;

struct Ray2
{
    public Vector2 origin, vector;

    public Ray2(Vector2 origin, Vector2 vector)
    {
        this.origin = origin;
        this.vector = vector;
    }
}

struct Ray
{
    public Vector3 origin, vector;

    public Ray(Vector3 origin, Vector3 vector)
    {
        this.origin = origin;
        this.vector = vector;
    }
}

static class Graphics
{
    const float MAXT = 1000000f;

    static float SphereHit(Ray ray, Vector3 center, float radius)
    {
        Vector3 tocenter = center - ray.origin;
        float b = Vector3.Dot(ray.vector, tocenter);
        float c = Vector3.Dot(tocenter, tocenter) - radius * radius;

        float D = b * b - c;
        if (D < 0) return MAXT;

        float t = b - MathF.Sqrt(D);
        if (t <= 0) return MAXT;

        return t;
    }

    static float CylinderHit(Ray ray, Vector3 center, float radius, float height)
    {
        Vector2 tocenter = ray.origin.XY() - center.XY();
        float a = Vector2.Dot(ray.vector.XY(), ray.vector.XY());
        float b = Vector2.Dot(ray.vector.XY(), tocenter);
        float c = Vector2.Dot(tocenter, tocenter) - radius * radius;

        float D = b * b - a * c;
        if (D < 0) return MAXT;

        float t = (-b - MathF.Sqrt(D)) / a;
        if (t <= 0) return MAXT;
        if (MathF.Abs(ray.origin.Z + t * ray.vector.Z - center.Z) > 0.5f * height) return MAXT;

        return t;
    }

    static float CuboidHit(Ray ray, Vector3 center, Vector3 halfsize)
    {
        Vector3 tMin = (center - halfsize - ray.origin) / ray.vector;
        Vector3 tMax = (center + halfsize - ray.origin) / ray.vector;

        Vector3 t1 = Vector3.Min(tMin, tMax);
        Vector3 t2 = Vector3.Max(tMin, tMax);

        float tNear = MathF.Max(MathF.Max(t1.X, t1.Y), t1.Z);
        float tFar = MathF.Min(MathF.Min(t2.X, t2.Y), t2.Z);

        if (tNear > tFar || tFar < 0f) return MAXT;
        return (tNear > 0f) ? tNear : tFar;
    }

    static Object Hit(Ray ray, List<Object> objects)
    {
        int minIndex = -1;
        float tmin = MAXT;
        for (int i = 0; i < objects.Count; i++)
        {
            Object o = objects[i];
            //if (o == self) continue;
            //if (o != target)
            //{
            //    if (o.template.ttype != TTYPE.ARTIFACT) continue;
            //    if (self.size.Z > o.size.Z) continue;
            //}

            float t;
            SHAPE shape = o.template.shape;
            if (shape == SHAPE.SPHERE) t = SphereHit(ray, o.position, o.size.AverageRadius());
            else if (shape == SHAPE.CYLINDER || shape == SHAPE.CAPSULE) t = CylinderHit(ray, o.position, o.size.AverageRadius2D(), o.size.Z);
            else if (shape == SHAPE.CUBOID) t = CuboidHit(ray, o.position, 0.5f * o.size);
            else throw new Exception($"unknown shape {shape}");

            if (t < tmin)
            {
                tmin = t;
                minIndex = i;
            }
        }
        return minIndex >= 0 ? objects[minIndex] : null;
    }

    public static bool Visible(Object self, Object target, List<Object> allObjects) //лол, это сейчас использется только в allVisible. Заметим ещё, что эта функция не совпадает с Sees!
    {
        if (self == target) return true;
        if (GH.Near(self, target)) return true;
        if (VH.FlatDistanceSquared(self.position, target.position) <= self.template.hearingRad * self.template.hearingRad) return true;

        Vector2 v = target.position.XY() - self.position.XY();
        float cos = Vector2.Dot(self.dir, v) / v.Length();
        if (cos < self.template.visionCos) return false;

        Ray ray = new(self.position, Vector3.Normalize(target.position - self.position));
        List<Object> objects = allObjects.Where(o => o == target || (o != self && o.template.blocksVision)).ToList();
        Object hit = Hit(ray, objects);
        return hit == null || hit == target;
    }

    static float CircleHit(Ray2 ray, Vector2 center, float radius)
    {
        Vector2 tocenter = center - ray.origin;
        float radiusSq = radius * radius;
        float distSq = Vector2.Dot(tocenter, tocenter);
        if (distSq <= radiusSq) return 0f;

        float b = Vector2.Dot(ray.vector, tocenter);
        float c = distSq - radiusSq;

        float D = b * b - c;
        if (D < 0) return MAXT;

        float t = b - MathF.Sqrt(D);
        if (t <= 0) return MAXT;

        return t;
    }

    static float RectHit(Ray2 ray, Vector2 center, Vector2 halfsize)
    {
        Vector2 local = ray.origin - center;
        if (MathF.Abs(local.X) <= halfsize.X && MathF.Abs(local.Y) <= halfsize.Y) return 0f;

        Vector2 tMin = (center - halfsize - ray.origin) / ray.vector;
        Vector2 tMax = (center + halfsize - ray.origin) / ray.vector;

        Vector2 t1 = Vector2.Min(tMin, tMax);
        Vector2 t2 = Vector2.Max(tMin, tMax);

        float tNear = MathF.Max(t1.X, t1.Y);
        float tFar = MathF.Min(t2.X, t2.Y);

        if (tNear > tFar || tFar < 0f) return MAXT;
        return (tNear > 0f) ? tNear : tFar;
    }

    static float SceneBoundHit2D(Ray2 ray, Vector2 min, Vector2 max)
    {
        if (ray.origin.X < min.X || ray.origin.X > max.X ||
            ray.origin.Y < min.Y || ray.origin.Y > max.Y)
            return 0f;

        Vector2 tMinForSlabs = (min - ray.origin) / ray.vector;
        Vector2 tMaxForSlabs = (max - ray.origin) / ray.vector;

        Vector2 t1 = Vector2.Min(tMinForSlabs, tMaxForSlabs);
        Vector2 t2 = Vector2.Max(tMinForSlabs, tMaxForSlabs);

        float tNear = MathF.Max(t1.X, t1.Y);
        float tFar = MathF.Min(t2.X, t2.Y);

        if (tNear > tFar || tFar < 0f) return MAXT;
        return (tNear > 0f) ? tNear : tFar;
    }

    static (Object o, float t) Hit2D(Ray2 ray, List<Object> allObjects, Vector2 sceneSize)
    {
        int minIndex = -1;
        float tmin = SceneBoundHit2D(ray, -0.5f * sceneSize, 0.5f * sceneSize);

        for (int i = 0; i < allObjects.Count; i++)
        {
            //if (i == selfIndex || i == equippedIndex) continue;
            Object o = allObjects[i];

            float t;
            SHAPE shape = o.template.shape;
            if (shape == SHAPE.CYLINDER || shape == SHAPE.CAPSULE || shape == SHAPE.SPHERE)
                t = CircleHit(ray, o.position.XY(), o.size.AverageRadius2D());
            else if (shape == SHAPE.CUBOID)
                t = RectHit(ray, o.position.XY(), 0.5f * o.size.XY());
            else throw new Exception($"unknown shape {shape}");

            if (t < tmin)
            {
                tmin = t;
                minIndex = i;
            }
        }
        return (minIndex >= 0 ? allObjects[minIndex] : null, tmin);
    }

    public static (Object o, float t) BodyCast(Object self, Vector2 dir, List<Object> allObjects, Vector2 sceneSize)
    {
        Vector2 ndir = Vector2.Normalize(dir);
        float r = self.size.AverageRadius2D() + 1.01f * Settings.NEAR_DISTANCE;

        Ray2 rayL = new(self.position.XY() + r * (ndir + ndir.Left()), ndir);
        Ray2 rayM = new(self.position.XY() + r * ndir, ndir);
        Ray2 rayR = new(self.position.XY() + r * (ndir + ndir.Right()), ndir);

        //int selfIndex = allObjects.IndexOf(self);

        (Object oL, float tL) = Hit2D(rayL, allObjects, sceneSize);
        (Object oM, float tM) = Hit2D(rayM, allObjects, sceneSize);
        (Object oR, float tR) = Hit2D(rayR, allObjects, sceneSize);

        float tmin = MathF.Min(MathF.Min(tL, tM), tR);

        if (tmin == tM) return (oM, tM);
        else if (tmin == tL) return (oL, tL);
        else return (oR, tR);
    }

    public static (Object o, float t) FilteredBodyCast(Object self, Vector2 v, State state)
    {
        Object equipped = null; //self.equippedUID < 0 ? null : state.uiddict[self.equippedUID];
        List<Object> filteredObjects = state.objects.Where(o => o != self && o != equipped).ToList();
        return BodyCast(self, v, filteredObjects, state.game.size);
    }
}
