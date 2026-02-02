using System.Numerics;
using Raylib_cs;

unsafe class MyShader
{
    Shader shader;
    int locData, locNumObjects;
    int locEmoji;

    float[] texelData;
    Texture2D texture;
    //Texture2D emoji;

    const int MAX_OBJECTS = 256, TEXELS_PER_OBJECT = 4;

    //TODO: у меня такое подозрение, что objects сюда не нужно передавать, а можно только объекты
    public MyShader(string filename, int SHX, int SHY, Camera camera, List<Object> objects, Vector2 sceneSize, List<string> emojiNames)
    {
        if (!File.Exists(filename)) throw new FileNotFoundException($"shader file <{filename}> not found");
        shader = Raylib.LoadShader(null, filename);
        if (shader.Id == 0) throw new Exception($"failed to load shader");

        shader.SetVec3("cameraPosition", camera.position);
        shader.SetVec3("cameraCorner", camera.corner);
        shader.SetVec3("cameraHorizontal", camera.horizontal);
        shader.SetVec3("cameraVertical", camera.vertical);

        shader.SetVec2("resolution", new Vector2(SHX, SHY));
        shader.SetVec3("groundColor", VH.Gray(1f));

        locData = Raylib.GetShaderLocation(shader, "data");
        locNumObjects = Raylib.GetShaderLocation(shader, "numObjects");
        shader.SetInt("numTextures", emojiNames.Count);
        shader.SetInt("DRAW_CONES", Settings.DRAW_VISIBILITY_CONES ? 1 : 0);

        //emoji = RaylibHelper.EmojiAtlas(emojiNames);
        locEmoji = Raylib.GetShaderLocation(shader, "emojiTex");

        texelData = new float[MAX_OBJECTS * TEXELS_PER_OBJECT * 4];
        Encode(objects, sceneSize);
        fixed (float* p = texelData)
        {
            Image img = new()
            {
                Data = p,
                Width = MAX_OBJECTS * TEXELS_PER_OBJECT,
                Height = 1,
                Mipmaps = 1,
                Format = PixelFormat.UncompressedR32G32B32A32
            };
            texture = Raylib.LoadTextureFromImage(img);
        }
        Raylib.SetTextureFilter(texture, TextureFilter.Point);
        Raylib.SetShaderValue(shader, locNumObjects, objects.Count + 1, ShaderUniformDataType.Int);
    }

    void Encode(List<Object> objects, Vector2 size)
    {
        int stride = TEXELS_PER_OBJECT * 4;

        texelData[0] = size.X;
        texelData[1] = size.Y;
        texelData[2] = 0f;
        texelData[3] = 0f;

        texelData[4] = 0f;
        texelData[5] = 0f;
        texelData[6] = 0f;
        texelData[7] = 0f;

        const float floorGray = 0.4f;
        texelData[8] = floorGray;
        texelData[9] = floorGray;
        texelData[10] = floorGray;
        texelData[11] = -1f;

        texelData[12] = 0f;
        texelData[13] = 0f;
        texelData[14] = 0f;
        texelData[15] = 0f;

        for (int i = 0; i < objects.Count; i++)
        {
            Object o = objects[i];
            int j = i + 1;
            int baseIndex = j * stride;

            texelData[baseIndex + 0] = o.size.X;
            texelData[baseIndex + 1] = o.size.Y;
            texelData[baseIndex + 2] = o.size.Z;
            texelData[baseIndex + 3] = (int)o.template.shape;

            texelData[baseIndex + 4] = o.position.X;
            texelData[baseIndex + 5] = o.position.Y;
            texelData[baseIndex + 6] = o.position.Z;
            texelData[baseIndex + 7] = MathF.Atan2(o.dir.Y, o.dir.X);

            texelData[baseIndex + 8] = o.color.X;
            texelData[baseIndex + 9] = o.color.Y;
            texelData[baseIndex + 10] = o.color.Z;
            texelData[baseIndex + 11] = 0; //o.template.textureID;

            texelData[baseIndex + 12] = o.template.visionCos;
            texelData[baseIndex + 13] = o.template.hearingRad;
            texelData[baseIndex + 14] = o.template.blocksVision ? 1f : 0f;
            texelData[baseIndex + 15] = 0f;
        }
    }

    public void Update(List<Object> objects, Vector2 size)
    {
        Encode(objects, size);
        fixed (float* p = texelData)
        {
            Raylib.UpdateTexture(texture, p);
        }
        Raylib.SetShaderValue(shader, locNumObjects, objects.Count + 1, ShaderUniformDataType.Int);
    }

    public void Draw()
    {
        Raylib.BeginShaderMode(shader);

        Raylib.SetShaderValueTexture(shader, locData, texture);
        //Raylib.SetShaderValueTexture(shader, locEmoji, emoji);

        Raylib.DrawRectangle(0, 0, Settings.SHX, Settings.SHY, Color.White);
        Raylib.EndShaderMode();
    }

    public void Unload()
    {
        Raylib.UnloadTexture(texture);
        //Raylib.UnloadTexture(emoji);
        Raylib.UnloadShader(shader);
    }
}
