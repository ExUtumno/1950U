using System.Numerics;

class Camera
{
    public Vector3 position, corner, horizontal, vertical; ///lower left corner

    Vector3 forward, up, right;
    const float fov = MathF.PI / 4.0f;

    public Camera(Vector2 scenesize, float axisangle, float rotangle, float reldist)
    {
        float average = 0.5f * (scenesize.X + scenesize.Y);
        float distance = average * reldist / 6.0f;
        float aspectRatio = 1.0f;

        float cos = MathF.Cos(rotangle);
        float sin = MathF.Sin(rotangle);

        position = distance * new Vector3(sin * MathF.Sin(axisangle), cos * MathF.Sin(axisangle), MathF.Cos(axisangle));
        forward = Vector3.Normalize(-position); ///(-cos, -sin)
        up = forward.Z == 0.0f ? new Vector3(0, -1, 0) : Vector3.Normalize(new Vector3(-sin, -cos, (sin * forward.X + cos * forward.Y) / forward.Z));
        right = Vector3.Cross(up, forward);

        float vlength = 1.0f, hlength = aspectRatio * vlength;
        horizontal = hlength * right;
        vertical = vlength * up;

        float tan = MathF.Tan(fov / 2.0f);
        float depth = 0.5f * hlength / tan;
        corner = position + depth * forward - 0.5f * horizontal - 0.5f * vertical;
    }

    public (float, float) Projection(Vector3 point)
    {
        Vector3 relativePoint = point - position;
        Vector3 cameraSpacePoint = new(Vector3.Dot(relativePoint, right), Vector3.Dot(relativePoint, up), Vector3.Dot(relativePoint, forward));

        float aspectRatio = horizontal.Length() / vertical.Length();
        float tan = MathF.Tan(fov / 2.0f);
        float x = cameraSpacePoint.X / (tan * cameraSpacePoint.Z);
        float y = cameraSpacePoint.Y * aspectRatio / (tan * cameraSpacePoint.Z);
        return (0.5f * (x + 1.0f), 0.5f * (y + 1.0f));
    }

    public (int, int) ScreenCoord(Vector3 point)
    {
        (float u, float v) = Projection(point);
        int MX = Settings.SHX * Settings.SCALE, MY = Settings.SHX * Settings.SCALE; //SHY?
        int sx = (int)(u * MX);
        int sy = (int)((1f - v) * MY);
        return (sx, sy);
    }
}
