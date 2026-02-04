using System.Numerics;
using Raylib_cs;

class GameScreen : Screen
{
    readonly MyRayFont font;
    readonly Action onPause;
    readonly Action onResume;

    Game game;
    State state;
    Camera camera;
    MyShader shader;
    RenderTexture2D target;
    float cameraPitch;
    bool paused;
    int renderWidth;
    int renderHeight;

    public GameScreen(MyRayFont font, Action onPause, Action onResume)
    {
        this.font = font;
        this.onPause = onPause;
        this.onResume = onResume;

        string gametext = File.ReadAllText("game.lisp");
        Expr xgame = Expr.Load($"(ROOT {gametext})");

        game = new Game(xgame);
        state = new State(game, 0);

        camera = CreateBaseCamera(game.size);
        UpdateCameraFromPlayer();

        renderWidth = Settings.WINDOW_WIDTH;
        renderHeight = Settings.WINDOW_HEIGHT;
        shader = new MyShader("resources/shader.c", renderWidth, renderHeight, camera, state.objects, game.size, []);
        target = Raylib.LoadRenderTexture(renderWidth, renderHeight);
        Raylib.SetTextureFilter(target.Texture, TextureFilter.Point);
    }

    public void RenderToTarget()
    {
        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(Color.Orange);
        shader.Draw();
        Raylib.EndTextureMode();
    }

    public override void OnKeyPressed(KeyboardKey key)
    {
        if (key != KeyboardKey.Tab) return;

        Settings.CAMERA_MODE = Settings.CAMERA_MODE == CameraMode.FirstPerson
            ? CameraMode.ThirdPerson
            : CameraMode.FirstPerson;

        UpdateCameraFromPlayer();
        shader.Update(state.objects, game.size, camera);
    }

    public override void Update(float dt)
    {
        _ = dt;
        if (Raylib.IsKeyPressed(KeyboardKey.P) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            TogglePause();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F12))
        {
            TakeScreenshot();
        }

        if (paused) return;

        ApplyMouseLook();

        bool jumpPressed = Raylib.IsKeyPressed(KeyboardKey.Space) ||
            Raylib.IsMouseButtonPressed(MouseButton.Left) ||
            Raylib.IsMouseButtonPressed(MouseButton.Right);
        KeyboardState keyboardState = new(
            Raylib.IsKeyDown(KeyboardKey.W),
            Raylib.IsKeyDown(KeyboardKey.S),
            Raylib.IsKeyDown(KeyboardKey.Left),
            Raylib.IsKeyDown(KeyboardKey.Right),
            Raylib.IsKeyDown(KeyboardKey.D),
            Raylib.IsKeyDown(KeyboardKey.A),
            jumpPressed,
            false, false, false, false, false, false, false);

        state.Step(keyboardState);
        UpdateCameraFromPlayer();
        shader.Update(state.objects, game.size, camera);
    }

    public override void Draw()
    {
        Raylib.DrawTexturePro(
            target.Texture,
            new Rectangle(0, 0, renderWidth, -renderHeight),
            new Rectangle(0, 0, Settings.WINDOW_WIDTH, Settings.WINDOW_HEIGHT),
            Vector2.Zero,
            0.0f,
            Color.White);

        if (Settings.DRAW_DIRS)
        {
            for (int i = 0; i < state.objects.Count; i++)
            {
                Object o = state.objects[i];
                if (o.template.ttype != TTYPE.CREATURE) continue;

                (int x, int y) = camera.ScreenCoord(o.position.Grounded());
                (int xe, int ye) = camera.ScreenCoord(o.position.Grounded() + new Vector3(o.dir, 0f));
                Raylib.DrawLineEx(new Vector2(x, y), new Vector2(xe, ye), 3f, Color.Green);
            }
        }

        DrawObjectNames();
    }

    public override void OnWindowResized(int width, int height)
    {
        renderWidth = width;
        renderHeight = height;

        if (target.Id != 0)
        {
            Raylib.UnloadRenderTexture(target);
        }
        target = Raylib.LoadRenderTexture(renderWidth, renderHeight);
        Raylib.SetTextureFilter(target.Texture, TextureFilter.Point);

        shader.UpdateResolution(renderWidth, renderHeight);
        UpdateCameraFromPlayer();
        shader.Update(state.objects, game.size, camera);
    }

    public void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        UpdateCursorState();
    }

    public void Unload()
    {
        Raylib.UnloadRenderTexture(target);
        shader.Unload();
    }

    void TogglePause()
    {
        paused = !paused;
        UpdateCursorState();
        if (paused) onPause?.Invoke();
        else onResume?.Invoke();
    }

    void UpdateCursorState()
    {
        if (!Settings.MOUSE_CONTROL) return;

        if (paused) Raylib.EnableCursor();
        else Raylib.DisableCursor();
    }

    void TakeScreenshot()
    {
        Image screenshot = Raylib.LoadImageFromScreen();
        string filename = $"{new Random().Next(1000)}.png";
        Raylib.ExportImage(screenshot, filename);
        Raylib.UnloadImage(screenshot);
        Console.WriteLine($"screenshot taken {filename}");
    }

    static Camera CreateBaseCamera(Vector2 sceneSize) =>
        Settings.CAMERA_MODE == CameraMode.TopDown ? new(sceneSize, 0f, 0.0f, 7.245f) : new(sceneSize, 0.6f, 0.1f, 9.5f);

    static Object FindPlayer(State state)
    {
        for (int i = 0; i < state.objects.Count; i++)
        {
            Object o = state.objects[i];
            if (o.isPlayer) return o;
        }
        return null;
    }

    static Vector2 SafeDir(Vector2 dir)
    {
        float lenSq = dir.LengthSquared();
        if (lenSq < 1e-6f) return Vector2.UnitX;
        return dir / MathF.Sqrt(lenSq);
    }

    void UpdateCameraFromPlayer()
    {
        CameraMode mode = Settings.CAMERA_MODE;
        if (mode != CameraMode.FirstPerson && mode != CameraMode.ThirdPerson) return;

        Object player = FindPlayer(state);
        if (player == null) return;

        Vector2 dir2 = SafeDir(player.dir);
        Vector3 up = Vector3.UnitZ;
        float cosPitch = MathF.Cos(cameraPitch);
        float sinPitch = MathF.Sin(cameraPitch);

        if (mode == CameraMode.FirstPerson)
        {
            float eyeOffset = Settings.FIRST_PERSON_EYE_HEIGHT * player.size.Z;
            float forwardOffset = Settings.FIRST_PERSON_FORWARD_OFFSET + player.size.AverageRadius2D();
            Vector3 eye = player.position + new Vector3(0f, 0f, eyeOffset) + new Vector3(dir2.X, dir2.Y, 0f) * forwardOffset;
            Vector3 forward = new(dir2.X * cosPitch, dir2.Y * cosPitch, sinPitch);
            camera.SetLook(eye, forward, up);
        }
        else
        {
            float backDistance = Settings.THIRD_PERSON_DISTANCE + player.size.AverageRadius2D();
            float cameraHeight = Settings.THIRD_PERSON_HEIGHT * player.size.Z;
            float targetHeight = Settings.THIRD_PERSON_TARGET_HEIGHT * player.size.Z;
            Vector3 back = new(-dir2.X, -dir2.Y, 0f);
            Vector3 cameraPos = player.position + back * (backDistance * cosPitch) + new Vector3(0f, 0f, cameraHeight + backDistance * sinPitch);
            Vector3 target = player.position + new Vector3(0f, 0f, targetHeight);
            Vector3 forward = target - cameraPos;
            camera.SetLook(cameraPos, forward, up);
        }
    }

    void ApplyMouseLook()
    {
        if (!Settings.MOUSE_CONTROL) return;

        CameraMode mode = Settings.CAMERA_MODE;
        if (mode != CameraMode.FirstPerson && mode != CameraMode.ThirdPerson) return;

        Object player = FindPlayer(state);
        if (player == null) return;

        Vector2 delta = Raylib.GetMouseDelta();
        if (delta.LengthSquared() < 1e-6f) return;

        float yawDelta = delta.X * Settings.MOUSE_SENSITIVITY;
        float pitchDelta = -delta.Y * Settings.MOUSE_SENSITIVITY;

        if (MathF.Abs(yawDelta) > 0f)
        {
            player.dir = SafeDir(player.dir).Rotated(yawDelta);
        }

        float minPitch = -Settings.MOUSE_MAX_PITCH;
        if (mode == CameraMode.ThirdPerson)
        {
            float backDistance = Settings.THIRD_PERSON_DISTANCE + player.size.AverageRadius2D();
            float cameraHeight = Settings.THIRD_PERSON_HEIGHT * player.size.Z;
            float minCameraZ = player.position.Z + 0.1f * player.size.Z;
            float baseZ = player.position.Z + cameraHeight;
            float minSin = (minCameraZ - baseZ) / MathF.Max(backDistance, 1e-3f);
            minSin = Math.Clamp(minSin, -0.99f, 0.99f);
            minPitch = MathF.Max(minPitch, MathF.Asin(minSin));
        }

        cameraPitch = Clamp(cameraPitch + pitchDelta, minPitch, Settings.MOUSE_MAX_PITCH);
    }

    static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    void DrawObjectNames()
    {
        font.DrawString($"frame = {state.currentFrame}", 0, 0, Color.Black, true);

        for (int i = 0; i < state.objects.Count; i++)
        {
            Object o = state.objects[i];

            int numLines = 1;

            (float u, float v) = camera.Projection(o.position);
            float U = u * Settings.WINDOW_WIDTH;
            float V = (1f - v) * Settings.WINDOW_HEIGHT;

            float x = U - o.name.Length * font.FX / 2f;
            float y = V - (numLines * font.FY) / 2f - 1f;
            const int R = 120;
            if (Settings.DRAW_NAMES)
            {
                Raylib.DrawRectangle((int)(x + 0.5f), (int)(y + 0.5f), o.name.Length * font.FX, font.FY - 1, new Color(R, R, R, 80));
                font.DrawString(o.name, (int)(x + 0.5f), (int)(y + 0.5f), Color.White, true);
            }
        }
    }
}
