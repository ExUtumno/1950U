/*
TODO 1: jumping

TODO 2: don't forget there should be grass texture on the floor! Поскольку у нас всё равно рейкастинг, размер этой текстуры может быть произвольным

TODO 3: first person camera
don't forget not-to-draw the player itself






*/

using System.Numerics;
using Raylib_cs;

static class Settings
{
    public const int SCALE = 1;
    ///public const int SHX = 960, SHY = 960;
    public const int SHX = 1200, SHY = 1200; //поскольку это игра, то настройки должны быть вынесены в лисп-файл в итоге
    ///public const int SHX = 1600, SHY = 1600;

    public const bool DRAW_NAMES = true, DRAW_VARS = false, DRAW_DIRS = true, DRAW_POLICY_VECTORS = false, DRAW_VISIBILITY_CONES = false;
    public const bool ISO_CAMERA = true;
    public const int FPS = 60;
    public const int MAXUID = 256;

    public const int NUM_RAYS = 32;

    public const float SPEED = 1.0f * 0.04f; //хотелось бы иметь скорость в осмысленных единицах измерения. Просто где-то нужно явно поделить на FPS
    public const float ROTSPEED = 1.0f * 0.05f;

    public const float ZONE_HEIGHT = 0.05f, NEAR_DISTANCE = 0.05f;
}

static class Program
{
    static void Main()
    {
        string gametext = File.ReadAllText("game.lisp");
        Expr xgame = Expr.Load($"(ROOT {gametext})");

        Game game = new(xgame);
        State state = new(game, 0);

        Camera camera = Settings.ISO_CAMERA ? new(game.size, 0.6f, 0.1f, 9.5f) : new(game.size, 0f, 0.0f, 7.245f);

        ///Raylib.SetTraceLogLevel(TraceLogLevel.Error);
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint); ///useful for smoother lines
        Raylib.InitWindow(Settings.SHX * Settings.SCALE, Settings.SHY * Settings.SCALE, "1950U");
        MyShader shader = new("resources/shader.c", Settings.SHX, Settings.SHY, camera, state.objects, game.size, []); //тот класс нужно немного упростить

        MyRayFont tamzen = new("resources/Tamzen10x20.png");
        Font jetBrainsMono = Raylib.LoadFontEx("resources/JetBrainsMono-Regular.ttf", 64, null, 0);

        RenderTexture2D target = Raylib.LoadRenderTexture(Settings.SHX, Settings.SHY);

        bool PAUSED = false;
        Raylib.SetTargetFPS(Settings.FPS); //TODO: тут нужно подучить теорию. Но давайте пока проверим, что он компилируется

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.P)) PAUSED = !PAUSED;
            if (Raylib.IsKeyPressed(KeyboardKey.S))
            {
                Image screenshot = Raylib.LoadImageFromScreen();
                string filename = $"{new Random().Next(1000)}.png";
                Raylib.ExportImage(screenshot, filename);
                Raylib.UnloadImage(screenshot);
                Console.WriteLine($"screenshot taken {filename}");
            }

            if (!PAUSED || Raylib.IsKeyPressed(KeyboardKey.G) || Raylib.IsKeyDown(KeyboardKey.One))
            {
                KeyboardState keyboardState = new(Raylib.IsKeyDown(KeyboardKey.Up), Raylib.IsKeyDown(KeyboardKey.Down), Raylib.IsKeyDown(KeyboardKey.Left), Raylib.IsKeyDown(KeyboardKey.Right), Raylib.IsKeyDown(KeyboardKey.A), Raylib.IsKeyDown(KeyboardKey.D), Raylib.IsKeyDown(KeyboardKey.T), Raylib.IsKeyPressed(KeyboardKey.Z), Raylib.IsKeyPressed(KeyboardKey.X), Raylib.IsKeyPressed(KeyboardKey.C), Raylib.IsKeyPressed(KeyboardKey.V), Raylib.IsKeyPressed(KeyboardKey.B), Raylib.IsKeyPressed(KeyboardKey.N), Raylib.IsKeyPressed(KeyboardKey.M));
                state.Step(keyboardState);
                shader.Update(state.objects, game.size);
            }

            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(Color.Orange);
            shader.Draw();
            Raylib.EndTextureMode();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkPurple);
            Raylib.DrawTexturePro(target.Texture, new Rectangle(0, 0, Settings.SHX, -Settings.SHY), new Rectangle(0, 0, Settings.SHX * Settings.SCALE, Settings.SHY * Settings.SCALE), new Vector2(0, 0), 0.0f, Color.White); ///тут мы шейдерную текстуру рисуем на основном экране

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

            DrawObjectNames(state, camera, tamzen);
            /*for (int e = Emoji.emojis.Count - 1; e >= 0; e--)
            {
                Emoji emoji = Emoji.emojis[e];
                (int XS, int YS) = camera.ScreenCoord(emoji.position);
                Raylib.DrawTexturePro(emojiTexture, new Rectangle(emoji.code * 72, 0, 72, 72), new Rectangle(XS - 36, YS - 72 - emoji.frame, 72, 72), Vector2.Zero, 0f, Color.White);
                if (!PAUSED && emoji.Step()) Emoji.emojis.RemoveAt(e);
            }
            if (game.player != null) DrawAvailableAbilities(state, jetBrainsMono);

            if (game.agentTemplate != null)
            {
                float score = state.Score(game.agentTemplate);
                string scoreString = $"{game.agentTemplate.name} score = {score:0.#}";
                float scoreWidth = Raylib.MeasureTextEx(jetBrainsMono, scoreString, jetBrainsMono.BaseSize, 0).X;
                Raylib.DrawTextEx(jetBrainsMono, scoreString, new Vector2((Settings.SHX * Settings.SCALE - scoreWidth) / 2, jetBrainsMono.BaseSize / 2), jetBrainsMono.BaseSize, 0, Color.DarkBlue);
            }*/
            Raylib.EndDrawing();
        }

        tamzen.Unload();
        Raylib.UnloadFont(jetBrainsMono);
        shader.Unload();
        Raylib.CloseWindow();
    }
    
    static void DrawObjectNames(State state, Camera camera, MyRayFont font)
    {
        font.DrawString($"frame = {state.currentFrame}", 0, 0, Color.Black, true);

        for (int i = 0; i < state.objects.Count; i++)
        {
            Object o = state.objects[i];

            int numLines = 1;

            //отрефакторить - потому что мы сделали функцию ScreenCoordinates()
            (float u, float v) = camera.Projection(o.position);
            float U = u * Settings.SHX * Settings.SCALE;
            float V = (1f - v) * Settings.SHY * Settings.SCALE;

            float x = U - o.name.Length * font.FX / 2f;
            float y = V - (numLines * font.FY) / 2f - 1f;
            const int R = 120;
            if (Settings.DRAW_NAMES)
            {
                Raylib.DrawRectangle((int)(x + 0.5f), (int)(y + 0.5f), o.name.Length * font.FX, font.FY - 1, new Color(R, R, R, 80));
                font.DrawString(o.name, (int)(x + 0.5f), (int)(y + 0.5f), Color.White, true);
            }
            /*if (Settings.DRAW_VARS)
            {
                (string varname, Value value)[] vars = o.env.Array();
                for (int n = 0; n < vars.Length; n++)
                {
                    (string varname, Value value) = vars[n];
                    if (varname == "self" || value.type == VTYPE.VECTOR) continue;
                    string line;
                    if (value.type == VTYPE.OBJ) line = $"{varname} = {Object.MyString(value.GetObject(state))}";
                    else line = $"{varname} = {value}";
                    x = u * Settings.SHX * Settings.SCALE - line.Length * font.FX / 2.0f;
                    y += font.FY;
                    font.DrawString(line, (int)(x + 0.5f), (int)(y + 0.5f), Color.DarkBrown, false);
                }
            }*/
        }
    }
}

/*
Подозреваю, что эмоджи-система нам таки нужна. Это важное свойство игр.
Но тут можно придумать и что-то более художественое. Типа рисовать более выразительные деревья с нормальной текстурой. А не просто эмоджи дерева.
Пока что можно начать бех эмоджи, а потом посмотрим.


 
*/
