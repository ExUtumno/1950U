using Raylib_cs;

abstract class Screen
{
    public virtual void Update(float dt) { }
    public virtual void Draw() { }
    public virtual void OnKeyPressed(KeyboardKey key) { }
    public virtual void OnKeyDown(KeyboardKey key) { }
    public virtual void OnMouseButtonPressed(MouseButton button) { }
    public virtual void OnMouseButtonDown(MouseButton button) { }
    public virtual void OnWindowResized(int width, int height) { }
}

class ScreenManager
{
    static readonly KeyboardKey[] KeysToPoll =
    {
        KeyboardKey.W,
        KeyboardKey.A,
        KeyboardKey.S,
        KeyboardKey.D,
        KeyboardKey.Up,
        KeyboardKey.Down,
        KeyboardKey.Left,
        KeyboardKey.Right,
        KeyboardKey.Space,
        KeyboardKey.Tab,
        KeyboardKey.Enter,
        KeyboardKey.Escape,
        KeyboardKey.P
    };

    readonly List<Screen> screens = new();
    bool requestQuit;

    public bool ShouldQuit => requestQuit;
    public Screen Top => screens.Count == 0 ? null : screens[^1];

    public void RequestQuit()
    {
        requestQuit = true;
    }

    public void Push(Screen screen)
    {
        if (screen == null) return;
        screens.Add(screen);
    }

    public void Pop()
    {
        if (screens.Count == 0) return;
        screens.RemoveAt(screens.Count - 1);
    }

    public void Switch(Screen screen)
    {
        screens.Clear();
        if (screen != null) screens.Add(screen);
    }

    public void Update(float dt)
    {
        if (screens.Count == 0) return;
        screens[^1].Update(dt);
    }

    public void Draw()
    {
        for (int i = 0; i < screens.Count; i++)
        {
            screens[i].Draw();
        }
    }

    public void HandleInput()
    {
        if (screens.Count == 0) return;
        Screen top = screens[^1];

        for (int i = 0; i < KeysToPoll.Length; i++)
        {
            KeyboardKey pollKey = KeysToPoll[i];
            if (Raylib.IsKeyPressed(pollKey)) top.OnKeyPressed(pollKey);
            if (Raylib.IsKeyDown(pollKey)) top.OnKeyDown(pollKey);
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left)) top.OnMouseButtonPressed(MouseButton.Left);
        if (Raylib.IsMouseButtonPressed(MouseButton.Right)) top.OnMouseButtonPressed(MouseButton.Right);
        if (Raylib.IsMouseButtonDown(MouseButton.Left)) top.OnMouseButtonDown(MouseButton.Left);
        if (Raylib.IsMouseButtonDown(MouseButton.Right)) top.OnMouseButtonDown(MouseButton.Right);
    }

    public void OnWindowResized(int width, int height)
    {
        for (int i = 0; i < screens.Count; i++)
        {
            screens[i].OnWindowResized(width, height);
        }
    }
}
