using System.Numerics;

static class GH
{
    public static Vector3 RandomPosition(Vector3 oSize, Vector2 sceneSize, Random random)
    {
        Vector2 sceneMin = -0.5f * sceneSize, sceneMax = 0.5f * sceneSize;
        return new(random.Uniform(sceneMin.X + 0.5f * oSize.X, sceneMax.X - 0.5f * oSize.X), random.Uniform(sceneMin.Y + 0.5f * oSize.Y, sceneMax.Y - 0.5f * oSize.Y), 0.5f * oSize.Z);
    }

    public static float BodyDistance(Object o1, Object o2)
    {
        if (o1.template.shape != SHAPE.CUBOID && o2.template.shape != SHAPE.CUBOID)
        {
            return VH.FlatDistance(o1.position, o2.position) - o1.size.AverageRadius2D() - o2.size.AverageRadius2D();
        }
        else if (o1.template.shape == SHAPE.CUBOID && o2.template.shape == SHAPE.CUBOID)
        {
            (float minx1, float miny1) = o1.MinXY();
            (float maxx1, float maxy1) = o1.MaxXY();
            (float minx2, float miny2) = o2.MinXY();
            (float maxx2, float maxy2) = o2.MaxXY();
            return MathF.Max(MathF.Max(minx2 - maxx1, minx1 - maxx2), MathF.Max(miny2 - maxy1, miny1 - maxy2));
        }
        else
        {
            Object circle = o1.template.shape == SHAPE.CUBOID ? o2 : o1;
            Object rect = o1.template.shape == SHAPE.CUBOID ? o1 : o2;

            (float minx, float miny) = rect.MinXY();
            (float maxx, float maxy) = rect.MaxXY();

            float closestX = Math.Clamp(circle.position.X, minx, maxx);
            float closestY = Math.Clamp(circle.position.Y, miny, maxy);

            float dx = circle.position.X - closestX;
            float dy = circle.position.Y - closestY;
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            return distance - circle.size.AverageRadius2D();
        }
    }
    public static float BodyDistance(Object o, Vector2 p)
    {
        if (o.template.shape == SHAPE.CUBOID)
        {
            (float minx, float miny) = o.MinXY();
            (float maxx, float maxy) = o.MaxXY();

            float distToLeftEdge = minx - p.X;
            float distToRightEdge = p.X - maxx;
            float distToBottomEdge = miny - p.Y;
            float distToTopEdge = p.Y - maxy;

            return MathF.Max(MathF.Max(distToLeftEdge, distToRightEdge), MathF.Max(distToBottomEdge, distToTopEdge));
        }
        else
        {
            float dx = o.position.X - p.X;
            float dy = o.position.Y - p.Y;
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            return distance - o.size.AverageRadius2D();
        }
    }

    //need to clarify the sign
    public static float SceneOverlapSDF(Object o, Vector2 sceneMin, Vector2 sceneMax)
    {
        (float minx, float miny) = o.MinXY();
        (float maxx, float maxy) = o.MaxXY();

        float left = minx - sceneMin.X;
        float right = sceneMax.X - maxx;
        float bottom = miny - sceneMin.Y;
        float top = sceneMax.Y - maxy;

        return MathF.Min(MathF.Min(left, right), MathF.Min(bottom, top));
    }
    public static bool Near(Object o1, Object o2) => o1 == o2 || BodyDistance(o1, o2) <= Settings.NEAR_DISTANCE;
    public static bool Near(Object o, Vector2 p) => BodyDistance(o, p) <= Settings.NEAR_DISTANCE;

    ///это всего лишь расстояние от точки до границы сцены
    //public static float SceneSDF(Vector2 p, Vector2 sceneSize) => 
    //    MathF.Min(MathF.Min(0.5f * sceneSize.X + p.X, 0.5f * sceneSize.X - p.X), MathF.Min(0.5f * sceneSize.Y + p.Y, 0.5f * sceneSize.Y - p.Y));

    //расстояние от объекта до границы сцены?
    /*public float SceneSDF(Object o)
    {
        Vector2 p = o.position.XY();
        if (o.template.shape == SHAPE.CUBOID)
        {
            return MathF.Min(MathF.Min(0.5f * size.X + p.X, 0.5f * size.X - p.X) - 0.5f * o.size.X, MathF.Min(0.5f * size.Y + p.Y, 0.5f * size.Y - p.Y) - 0.5f * o.size.Y);
        }
        else if (o.template.shape == SHAPE.SPHERE || o.template.shape == SHAPE.CYLINDER || o.template.shape == SHAPE.CAPSULE)
        {
            return MathF.Min(MathF.Min(0.5f * size.X + p.X, 0.5f * size.X - p.X), MathF.Min(0.5f * size.Y + p.Y, 0.5f * size.Y - p.Y)) - o.size.AverageRadius2D();
        }
        else throw new Exception($"unknown shape <{o.template.shape}>");
    }*/
}
