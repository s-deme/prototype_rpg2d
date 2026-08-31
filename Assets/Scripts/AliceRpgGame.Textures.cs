using UnityEngine;

public sealed partial class AliceRpgGame
{
    private void CreateTextures()
    {
        textures["white"] = Solid(Color.white);
        textures["ground"] = TileTexture(new Color32(180, 203, 143, 255), new Color32(169, 192, 132, 255), false);
        textures["grass"] = TileTexture(new Color32(84, 151, 98, 255), new Color32(69, 132, 87, 255), true);
        textures["water"] = WaterTexture();
        textures["bridge"] = BridgeTexture();
        textures["wall"] = WallTexture();
        textures["shrub"] = ShrubTexture();
        textures["door"] = DoorTexture();
        textures["chest"] = ChestTexture();
        textures["flower"] = FlowerTexture();
        textures["alice"] = CharacterTexture(new Color32(80, 161, 215, 255), new Color32(246, 236, 206, 255), new Color32(236, 190, 83, 255));
        textures["rabbit"] = RabbitTexture();
        textures["hatter"] = CharacterTexture(new Color32(111, 71, 133, 255), new Color32(230, 119, 67, 255), new Color32(58, 42, 68, 255));
        textures["caterpillar"] = CaterpillarTexture();
        textures["cat"] = CatTexture();
        textures["queen"] = CharacterTexture(new Color32(180, 42, 66, 255), new Color32(47, 37, 51, 255), new Color32(246, 195, 68, 255));
        textures["mushroom"] = MushroomTexture();
        textures["card"] = CardTexture();
        textures["shadow"] = ShadowTexture();
        textures["queenBattle"] = QueenBattleTexture();
    }

    private Texture2D Solid(Color color)
    {
        Texture2D texture = NewTexture(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Texture2D TileTexture(Color a, Color b, bool blades)
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, a);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                if ((x * 5 + y * 7) % 23 == 0) t.SetPixel(x, y, b);
        if (blades)
        {
            for (int x = 2; x < 16; x += 5)
            {
                t.SetPixel(x, 3 + x % 7, b);
                t.SetPixel(x + 1, 4 + x % 7, b);
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D WaterTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(56, 132, 181, 255));
        Color foam = new Color32(104, 190, 210, 255);
        for (int y = 3; y < 16; y += 6)
            for (int x = (y / 3) % 2; x < 14; x += 6)
                for (int i = 0; i < 3; i++) t.SetPixel(x + i, y, foam);
        t.Apply();
        return t;
    }

    private Texture2D BridgeTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(139, 91, 57, 255));
        Color line = new Color32(83, 56, 48, 255);
        for (int y = 0; y < 16; y += 4)
            for (int x = 0; x < 16; x++) t.SetPixel(x, y, line);
        t.Apply();
        return t;
    }

    private Texture2D WallTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(54, 77, 66, 255));
        Color leaf = new Color32(37, 103, 65, 255);
        for (int y = 2; y < 15; y += 5)
            for (int x = (y % 3); x < 15; x += 5) Block(t, x, y, 3, 3, leaf);
        t.Apply();
        return t;
    }

    private Texture2D ShrubTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(180, 203, 143, 255));
        Color dark = new Color32(36, 96, 62, 255);
        Color light = new Color32(65, 132, 71, 255);
        Block(t, 2, 5, 12, 8, dark);
        Block(t, 4, 3, 5, 8, light);
        Block(t, 9, 4, 4, 7, light);
        t.Apply();
        return t;
    }

    private Texture2D DoorTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(69, 61, 78, 255));
        Block(t, 3, 1, 10, 15, new Color32(142, 42, 65, 255));
        Block(t, 7, 1, 2, 15, new Color32(246, 195, 68, 255));
        t.Apply();
        return t;
    }

    private Texture2D ChestTexture()
    {
        Texture2D t = NewTexture(16, 16);
        Fill(t, new Color32(180, 203, 143, 255));
        Color wood = new Color32(124, 75, 47, 255);
        Color edge = new Color32(73, 47, 43, 255);
        Block(t, 2, 3, 12, 9, wood);
        Block(t, 2, 11, 12, 2, edge);
        Block(t, 2, 8, 12, 1, gold);
        Block(t, 7, 7, 2, 4, cream);
        t.Apply();
        return t;
    }

    private Texture2D FlowerTexture()
    {
        Texture2D t = NewTexture(5, 5);
        Fill(t, Color.clear);
        Color petal = new Color32(250, 210, 229, 255);
        t.SetPixel(2, 0, petal); t.SetPixel(0, 2, petal); t.SetPixel(4, 2, petal); t.SetPixel(2, 4, petal);
        t.SetPixel(2, 2, gold);
        t.Apply();
        return t;
    }

    private Texture2D CharacterTexture(Color clothes, Color hair, Color accent)
    {
        Texture2D t = NewTexture(16, 18);
        Fill(t, Color.clear);
        Color skin = new Color32(250, 214, 183, 255);
        Block(t, 5, 11, 6, 5, hair);
        Block(t, 6, 10, 4, 5, skin);
        Block(t, 5, 5, 6, 6, clothes);
        Block(t, 3, 2, 10, 4, clothes);
        Block(t, 5, 0, 2, 3, ink);
        Block(t, 9, 0, 2, 3, ink);
        Block(t, 7, 6, 2, 5, accent);
        t.SetPixel(7, 12, ink); t.SetPixel(9, 12, ink);
        t.Apply();
        return t;
    }

    private Texture2D RabbitTexture()
    {
        Texture2D t = NewTexture(16, 18);
        Fill(t, Color.clear);
        Color white = new Color32(242, 238, 222, 255);
        Color pink = new Color32(228, 126, 148, 255);
        Block(t, 4, 12, 3, 6, white); Block(t, 9, 12, 3, 6, white);
        Block(t, 5, 8, 6, 7, white); Block(t, 4, 3, 8, 7, white);
        Block(t, 5, 5, 6, 5, new Color32(62, 113, 166, 255));
        t.SetPixel(6, 12, pink); t.SetPixel(9, 12, pink);
        t.SetPixel(7, 10, ink); t.SetPixel(10, 10, ink);
        t.Apply(); return t;
    }

    private Texture2D CaterpillarTexture()
    {
        Texture2D t = NewTexture(16, 18);
        Fill(t, Color.clear);
        Color body = new Color32(58, 151, 170, 255);
        for (int i = 0; i < 4; i++) Block(t, 3 + i * 3, 3 + (i % 2), 5, 5, body);
        Block(t, 5, 9, 7, 7, body);
        Block(t, 4, 15, 9, 2, new Color32(89, 58, 119, 255));
        t.SetPixel(7, 12, ink); t.SetPixel(10, 12, ink);
        t.Apply(); return t;
    }

    private Texture2D CatTexture()
    {
        Texture2D t = NewTexture(16, 18);
        Fill(t, Color.clear);
        Color purple = new Color32(133, 83, 163, 255);
        Color stripe = new Color32(226, 97, 153, 255);
        Block(t, 4, 9, 8, 7, purple);
        Block(t, 3, 13, 3, 4, purple); Block(t, 10, 13, 3, 4, purple);
        Block(t, 5, 4, 6, 7, purple); Block(t, 3, 1, 10, 5, purple);
        Block(t, 5, 6, 6, 2, stripe);
        Block(t, 6, 10, 5, 1, cream);
        t.SetPixel(6, 13, gold); t.SetPixel(10, 13, gold);
        t.Apply(); return t;
    }

    private Texture2D MushroomTexture()
    {
        Texture2D t = NewTexture(32, 32);
        Fill(t, Color.clear);
        Color cap = new Color32(190, 54, 87, 255);
        Block(t, 5, 16, 22, 9, cap); Block(t, 9, 23, 14, 5, cap);
        Block(t, 12, 5, 8, 13, new Color32(236, 218, 180, 255));
        Block(t, 8, 18, 4, 4, cream); Block(t, 20, 20, 4, 4, cream);
        t.SetPixel(14, 12, ink); t.SetPixel(18, 12, ink);
        t.Apply(); return t;
    }

    private Texture2D CardTexture()
    {
        Texture2D t = NewTexture(32, 32);
        Fill(t, Color.clear);
        Block(t, 8, 4, 16, 24, cream);
        Block(t, 9, 5, 14, 22, new Color32(245, 240, 222, 255));
        Block(t, 5, 8, 4, 3, ink); Block(t, 23, 8, 4, 3, ink);
        Block(t, 10, 0, 3, 6, ink); Block(t, 20, 0, 3, 6, ink);
        Block(t, 14, 13, 5, 5, rose);
        t.Apply(); return t;
    }

    private Texture2D ShadowTexture()
    {
        Texture2D t = NewTexture(32, 32);
        Fill(t, Color.clear);
        Color shadow = new Color32(38, 27, 56, 255);
        Block(t, 4, 5, 24, 18, shadow); Block(t, 7, 21, 18, 7, shadow);
        Block(t, 9, 12, 14, 4, cream);
        Block(t, 11, 13, 10, 2, ink);
        t.SetPixel(10, 19, gold); t.SetPixel(21, 19, gold);
        t.Apply(); return t;
    }

    private Texture2D QueenBattleTexture()
    {
        Texture2D t = NewTexture(32, 32);
        Fill(t, Color.clear);
        Color red = new Color32(180, 42, 66, 255);
        Block(t, 7, 2, 18, 16, red); Block(t, 3, 0, 26, 8, red);
        Block(t, 10, 17, 12, 10, new Color32(250, 214, 183, 255));
        Block(t, 9, 26, 3, 6, gold); Block(t, 15, 28, 3, 4, gold); Block(t, 21, 26, 3, 6, gold);
        Block(t, 12, 20, 3, 2, ink); Block(t, 19, 20, 3, 2, ink);
        Block(t, 14, 17, 6, 3, ink);
        t.Apply(); return t;
    }

    private Texture2D NewTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private void Fill(Texture2D texture, Color color)
    {
        Color[] colors = new Color[texture.width * texture.height];
        for (int i = 0; i < colors.Length; i++) colors[i] = color;
        texture.SetPixels(colors);
    }

    private void Block(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = Mathf.Max(0, y); py < Mathf.Min(texture.height, y + height); py++)
            for (int px = Mathf.Max(0, x); px < Mathf.Min(texture.width, x + width); px++)
                texture.SetPixel(px, py, color);
    }
}
