using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using GameManager;
using Platforms;
using Season.Fonts;
using SeasonXNA.Interop;

namespace Zhsan.GameManager;

internal readonly record struct ImageRequest(string Name, bool User = false, TextureShape Shape = TextureShape.None);

/// <summary>
/// 頁面資源加載入口：把 ImageRequest 解析為磁盤路徑並委託平台圖像服務。
/// 字體部分待字體管線移植後再併入，這裡只覆蓋動態材質緩存所需的圖像路徑。
/// </summary>
internal static class PageResourceStore
{
    internal static Texture2D LoadImage(ImageRequest request)
    {
        string path;
        if (request.User)
            path = MigrationCheck.UserPath(request.Name);
        else
        {
            string name = request.Name;
            if (!Path.HasExtension(name))
            {
                var matches = Session.TextureRecs.Where(p => p.Key.Split('#')[0] == name).Select(p => p.Value.Ext).Distinct().ToArray();
                if (matches.Length != 1 || string.IsNullOrWhiteSpace(matches[0]))
                    throw new InvalidDataException($"Missing or ambiguous texture declaration: {name}");
                name += "." + matches[0];
            }
            path = Path.Combine(AppContext.BaseDirectory, "Content", "Textures", name);
        }
        return PlatformBase.Current.LoadImageFile(path, request.Shape);
    }

    /// <summary>B0：按 FontPair 加載引擎 SpriteFont（TTF 直讀 + MSDF 字形包盡力註冊）。</summary>
    internal static SpriteFont LoadFont(FontPair pair)
    {
        // 啟動期後台預熱已構造好字體：直接收養，幀線程零 IO、零解析。
        if (MenuFontWarmup.TryTakeStaged(pair, out var staged))
        {
            return staged;
        }
        // 資源聲明沿用 Windows 路徑慣例（如 "Content\Font\FZLB_GBK.TTF"），
        // 統一為正斜槓以便在 POSIX 主機解析。
        var fontPath = Path.Combine(AppContext.BaseDirectory, pair.Name.Replace('\\', '/'));
        var font = SeasonResources.LoadFont(fontPath, pair.Size);
        // 與字體同目錄的預烘焙 MSDF 字形包（SGPK v1）：註冊為盡力而為，
        // 缺失或被拒絕時代之以動態字形光柵化兜底。
        var packPath = fontPath + ".msdf.pack";
        GlyphPackRegistry.Register(packPath);
        _ = GlyphPackRegistry.RegisterAsync(packPath);
        // 缺字回退 '*'，與舊緩存路線一致，避免整行繪製失敗。
        font.DefaultCharacter = '*';
        font.LineSpacing = pair.Size;
        return font;
    }
}
