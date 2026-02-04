using System.Numerics;
using Raylib_cs;

class MenuScreen : Screen
{
    static readonly string[] Items = { "Play", "Quit" };

    readonly Font font;
    readonly Action onPlay;
    readonly Action onQuit;
    int selectionIndex;

    public MenuScreen(Font font, Action onPlay, Action onQuit)
    {
        this.font = font;
        this.onPlay = onPlay;
        this.onQuit = onQuit;
        selectionIndex = 0;
    }

    public override void OnKeyPressed(KeyboardKey key)
    {
        if (key == KeyboardKey.Up || key == KeyboardKey.W) MoveSelection(-1);
        else if (key == KeyboardKey.Down || key == KeyboardKey.S) MoveSelection(1);
        else if (key == KeyboardKey.Enter || key == KeyboardKey.Space) ActivateSelection();
        else if (key == KeyboardKey.P || key == KeyboardKey.Escape)
        {
            selectionIndex = 0;
            ActivateSelection();
        }
    }

    public override void Draw()
    {
        int width = Settings.WINDOW_WIDTH;
        int height = Settings.WINDOW_HEIGHT;

        Raylib.DrawRectangle(0, 0, width, height, new Color(8, 8, 10, 170));

        float titleSize = font.BaseSize * 1.6f;
        DrawCenteredText("Paused", height * 0.15f, titleSize, Color.White);

        float itemSize = font.BaseSize * 1.1f;
        float startY = height * 0.32f;
        float lineGap = itemSize * 1.6f;

        for (int i = 0; i < Items.Length; i++)
        {
            bool selected = i == selectionIndex;
            string label = selected ? $"> {Items[i]}" : $"  {Items[i]}";
            Color color = selected ? new Color(240, 220, 140, 255) : new Color(210, 210, 210, 255);
            DrawCenteredText(label, startY + i * lineGap, itemSize, color);
        }

        float infoSize = font.BaseSize * 0.6f;
        float infoY = height * 0.6f;
        float infoGap = infoSize * 1.5f;

        DrawCenteredText("Controls", infoY, infoSize, new Color(220, 220, 220, 255));
        DrawCenteredText("W/A/S/D: move", infoY + infoGap, infoSize, new Color(200, 200, 200, 255));
        DrawCenteredText("Arrow Left/Right: turn", infoY + 2 * infoGap, infoSize, new Color(200, 200, 200, 255));
        DrawCenteredText("Mouse: look", infoY + 3 * infoGap, infoSize, new Color(200, 200, 200, 255));
        DrawCenteredText("Tab: switch camera", infoY + 4 * infoGap, infoSize, new Color(200, 200, 200, 255));
        DrawCenteredText("Space or Mouse Button: jump", infoY + 5 * infoGap, infoSize, new Color(200, 200, 200, 255));
        DrawCenteredText("P or Esc: resume", infoY + 6 * infoGap, infoSize, new Color(200, 200, 200, 255));
    }

    void MoveSelection(int delta)
    {
        selectionIndex = (selectionIndex + delta + Items.Length) % Items.Length;
    }

    void ActivateSelection()
    {
        if (selectionIndex == 0) onPlay?.Invoke();
        else onQuit?.Invoke();
    }

    void DrawCenteredText(string text, float y, float fontSize, Color color)
    {
        Vector2 size = Raylib.MeasureTextEx(font, text, fontSize, 2);
        float x = (Settings.WINDOW_WIDTH - size.X) * 0.5f;
        Raylib.DrawTextEx(font, text, new Vector2(x, y), fontSize, 2, color);
    }
}
