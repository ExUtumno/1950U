using System.Numerics;
using Raylib_cs;

static class RaylibHelper
{
    public static Texture2D LoadTexture(int[] data, int width, int height)
    {
        if (data.Length != width * height) throw new Exception($"wrong image size: {data.Length} != {width} * {height}");

        Texture2D result;
        unsafe
        {
            Color* pixels = (Color*)Raylib.MemAlloc((uint)(width * height * sizeof(Color)));
            for (int i = 0; i < data.Length; i++)
            {
                var (r, g, b, a) = CH.AllBytesFromInt(data[i]);
                pixels[i] = new Color(r, g, b, a);
            }

            Image image = new() { Data = pixels, Width = width, Height = height, Format = PixelFormat.UncompressedR8G8B8A8, Mipmaps = 1 };
            result = Raylib.LoadTextureFromImage(image);
            Raylib.MemFree(pixels);
        }
        return result;
    }

    /*public static Texture2D EmojiAtlas(List<string> emojiNames)
    {
        const int TX = 72, TY = 72;
        Image atlas = Raylib.GenImageColor(emojiNames.Count * TX, TY, Color.Blank);
        for (int i = 0; i < emojiNames.Count; i++)
        {
            string emojiFileName = EmojiBase.FileName(emojiNames[i]);
            Image image = Raylib.LoadImage(emojiFileName == null ? $"resources/empty.png" : $"resources/emoji/{emojiFileName}"); ///было бы более изящно поставить -1 в качестве индекса. Но так проще

            Raylib.ImageFlipHorizontal(ref image);
            Raylib.ImageDraw(ref atlas, image, new Rectangle(0, 0, TX, TY), new Rectangle(i * TX, 0, TX, TY), Color.White);
            Raylib.UnloadImage(image);
        }
        Texture2D result = Raylib.LoadTextureFromImage(atlas);
        Raylib.SetTextureFilter(result, TextureFilter.Point);
        Raylib.UnloadImage(atlas);
        return result;
    }*/

    public static void SetInt(this Shader shader, string varname, int i)
    {
        int location = Raylib.GetShaderLocation(shader, varname);
        Raylib.SetShaderValue(shader, location, i, ShaderUniformDataType.Int);
    }
    public static void SetVec2(this Shader shader, string varname, Vector2 v)
    {
        int location = Raylib.GetShaderLocation(shader, varname);
        Raylib.SetShaderValue(shader, location, v, ShaderUniformDataType.Vec2);
    }
    public static void SetVec3(this Shader shader, string varname, Vector3 v)
    {
        int location = Raylib.GetShaderLocation(shader, varname);
        Raylib.SetShaderValue(shader, location, v, ShaderUniformDataType.Vec3);
    }
}

class MyRayFont
{
    Texture2D texture;
    public int FX, FY;
    Rectangle[] sources;

    static readonly char[] legend = "ABCDEFGHIJKLMNOPQRSTUVWXYZλ12345abcdefghijklmnopqrstuvwxyz 67890{}[]()<>$*-+=/#_%^@\\&|~?'\"`!,.;:".ToCharArray();
    static Dictionary<char, byte> map;
    static MyRayFont()
    {
        map = [];
        for (int i = 0; i < legend.Length; i++) map.Add(legend[i], (byte)i);
    }

    public MyRayFont(string filename)
    {
        (int[] bitmap, int width, int height) = ImageSharpHelper.LoadBitmap(filename);
        if (width % 32 != 0) throw new Exception($"font {filename} width is not a multiple of 32");
        if (height % 6 != 0) throw new Exception($"font {filename} height is not a multiple of 6");

        FX = width / 32;
        FY = height / 6;

        int background = bitmap[0];
        for (int i = 0; i < bitmap.Length; i++) bitmap[i] = bitmap[i] == background ? 0 : -1;
        texture = RaylibHelper.LoadTexture(bitmap, width, height);

        sources = new Rectangle[32 * 6];
        for (int y = 0; y < 6; y++) for (int x = 0; x < 32; x++) sources[x + y * 32] = new Rectangle(x * FX, y * FY, FX, FY);
    }

    public void DrawString(string s, int x, int y, Color color, bool bold)
    {
        for (int k = 0; k < s.Length; k++)
        {
            byte i = map[s[k]];
            if (bold) i += 96;
            Raylib.DrawTexturePro(texture, sources[i], new Rectangle(x + FX * k, y, FX, FY), Vector2.Zero, 0.0f, color);
        }
    }

    public void Unload()
    {
        Raylib.UnloadTexture(texture);
    }
}
