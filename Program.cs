/*
TODO 1: jumping

TODO 2: don't forget there should be grass texture on the floor! Поскольку у нас всё равно рейкастинг, размер этой текстуры может быть произвольным

SCREENS: same controls, but one screen shows first-person, another third-person, top-down, standard.

Aha, the AO bug is actually not related to shadow!

---

//GetFrameTime() -> delta time in seconds
//SetTargetFPS(fps) -> caps frame rate

const float FIXED_DT = 1.0f / 60.0f;
float accumulator = 0.0f;

while (!WindowShouldClose())
{
    float dt = GetFrameTime();
    accumulator += dt;

    while (accumulator >= FIXED_DT)
    {
        FixedUpdate(FIXED_DT); // physics, simulation
        accumulator -= FIXED_DT;
    }

    BeginDrawing();
        Draw();
    EndDrawing();
}





*/

using Raylib_cs;

enum CameraMode { FirstPerson, ThirdPerson, Iso, TopDown }

static class Settings
{
    public const int SCALE = 1;
    ///public const int SHX = 960, SHY = 960;
    public const int SHX = 1600, SHY = 1200; //поскольку это игра, то настройки должны быть вынесены в лисп-файл в итоге
    ///public const int SHX = 1600, SHY = 1600;
    public static int WINDOW_WIDTH = SHX * SCALE;
    public static int WINDOW_HEIGHT = SHY * SCALE;

    public const bool DRAW_NAMES = true, DRAW_VARS = false, DRAW_DIRS = false, DRAW_POLICY_VECTORS = false, DRAW_VISIBILITY_CONES = false;
    public static CameraMode CAMERA_MODE = CameraMode.ThirdPerson;
    public const bool MOUSE_CONTROL = true;
    public const float MOUSE_SENSITIVITY = 0.0025f; //radians per pixel
    public const float MOUSE_MAX_PITCH = 1.2f; //radians

    public const int FPS = 60;
    public const int MAXUID = 256;

    public const int NUM_RAYS = 32;

    public const float SPEED = 1.0f * 0.04f; //хотелось бы иметь скорость в осмысленных единицах измерения. Просто где-то нужно явно поделить на FPS
    public const float ROTSPEED = 1.0f * 0.05f;
    public const float GRAVITY = 0.02f;
    public const float JUMP_SPEED = 0.25f;
    public const float GROUND_EPS = 0.001f;

    public const float ZONE_HEIGHT = 0.05f, NEAR_DISTANCE = 0.05f;

    public const float FIRST_PERSON_EYE_HEIGHT = 0.4f; //fraction of size.Z above object center
    public const float FIRST_PERSON_FORWARD_OFFSET = 0.05f;
    public const float THIRD_PERSON_DISTANCE = 5.5f; //added to object radius
    public const float THIRD_PERSON_HEIGHT = 0.8f; //fraction of size.Z above object center
    public const float THIRD_PERSON_TARGET_HEIGHT = 0.2f; //fraction of size.Z above object center
}

static class Program
{
    static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(Settings.SHX * Settings.SCALE, Settings.SHY * Settings.SCALE, "1950U");
        Raylib.SetExitKey(KeyboardKey.Null);

        Settings.WINDOW_WIDTH = Raylib.GetScreenWidth();
        Settings.WINDOW_HEIGHT = Raylib.GetScreenHeight();

        if (Settings.MOUSE_CONTROL && (Settings.CAMERA_MODE == CameraMode.FirstPerson || Settings.CAMERA_MODE == CameraMode.ThirdPerson))
        {
            Raylib.DisableCursor();
        }

        MyRayFont tamzen = new("resources/Tamzen10x20.png");
        Font jetBrainsMono = Raylib.LoadFontEx("resources/JetBrainsMono-Regular.ttf", 64, null, 0);

        ScreenManager screens = new();
        MenuScreen menuScreen = null;

        GameScreen gameScreen = new(
            tamzen,
            onPause: () => screens.Push(menuScreen),
            onResume: () =>
            {
                if (screens.Top is MenuScreen) screens.Pop();
            });

        menuScreen = new MenuScreen(
            jetBrainsMono,
            onPlay: () =>
            {
                gameScreen.SetPaused(false);
                if (screens.Top is MenuScreen) screens.Pop();
            },
            onQuit: screens.RequestQuit);

        gameScreen.SetPaused(true);
        screens.Push(gameScreen);
        screens.Push(menuScreen);

        Raylib.SetTargetFPS(Settings.FPS);

        while (!Raylib.WindowShouldClose() && !screens.ShouldQuit)
        {
            float dt = Raylib.GetFrameTime();

            if (Raylib.IsWindowResized())
            {
                int width = Raylib.GetScreenWidth();
                int height = Raylib.GetScreenHeight();
                Settings.WINDOW_WIDTH = width;
                Settings.WINDOW_HEIGHT = height;
                screens.OnWindowResized(width, height);
            }

            screens.HandleInput();
            screens.Update(dt);

            gameScreen.RenderToTarget();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkPurple);
            screens.Draw();
            Raylib.EndDrawing();
        }

        gameScreen.Unload();
        tamzen.Unload();
        Raylib.UnloadFont(jetBrainsMono);
        Raylib.CloseWindow();
    }
}

/*
Подозреваю, что эмоджи-система нам таки нужна. Это важное свойство игр.
Но тут можно придумать и что-то более художественое. Типа рисовать более выразительные деревья с нормальной текстурой. А не просто эмоджи дерева.
Пока что можно начать бех эмоджи, а потом посмотрим.


 
*/
