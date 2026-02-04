using System.Numerics;

//I need a better way to set the camera: position, direction, viewangle?

class Camera
{
    public Vector3 position, corner, horizontal, vertical; ///lower left corner

    Vector3 forward, up, right;
    const float fov = MathF.PI / 4.0f;

    public Camera(Vector2 scenesize, float axisangle, float rotangle, float reldist)
    {
        float average = 0.5f * (scenesize.X + scenesize.Y);
        float distance = average * reldist / 6.0f;
        float cos = MathF.Cos(rotangle);
        float sin = MathF.Sin(rotangle);

        Vector3 pos = distance * new Vector3(sin * MathF.Sin(axisangle), cos * MathF.Sin(axisangle), MathF.Cos(axisangle));
        Vector3 dir = Vector3.Normalize(-pos); ///(-cos, -sin)
        Vector3 upHint = dir.Z == 0.0f ? new Vector3(0, -1, 0) : Vector3.Normalize(new Vector3(-sin, -cos, (sin * dir.X + cos * dir.Y) / dir.Z));
        SetLook(pos, dir, upHint);
    }

    public Camera(Vector3 position, Vector3 forward, Vector3 upHint)
    {
        SetLook(position, forward, upHint);
    }

    public void SetLook(Vector3 newPosition, Vector3 forwardDirection, Vector3 upHint)
    {
        position = newPosition;
        if (forwardDirection.LengthSquared() < 1e-8f) forwardDirection = Vector3.UnitY;
        forward = Vector3.Normalize(forwardDirection);

        if (upHint.LengthSquared() < 1e-8f) upHint = Vector3.UnitZ;
        right = Vector3.Cross(upHint, forward);
        if (right.LengthSquared() < 1e-8f)
        {
            Vector3 fallbackUp = MathF.Abs(Vector3.Dot(forward, Vector3.UnitZ)) > 0.99f ? Vector3.UnitY : Vector3.UnitZ;
            right = Vector3.Cross(fallbackUp, forward);
        }
        right = Vector3.Normalize(right);
        up = Vector3.Normalize(Vector3.Cross(forward, right));

        float vlength = 1.0f;
        float hlength = Settings.WINDOW_HEIGHT == 0 ? vlength : vlength * (Settings.WINDOW_WIDTH / (float)Settings.WINDOW_HEIGHT);
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
        int MX = Settings.WINDOW_WIDTH, MY = Settings.WINDOW_HEIGHT;
        int sx = (int)(u * MX);
        int sy = (int)((1f - v) * MY);
        return (sx, sy);
    }
}
