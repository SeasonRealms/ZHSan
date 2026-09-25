using Microsoft.Xna.Framework.Graphics;

namespace Zhsan.GameManager;

/// <summary>
/// B0：共享純色底圖。SeasonXNA 的 Texture2D 只讀（不可 new/SetData），
/// 原「按尺寸程序化生成純色紋理」的路線改為「1x1 純白貼圖 + 繪製時 tint」：
/// <c>batch.Draw(SolidTextures.White, new Rectangle(x, y, w, h), color)</c>。
/// 首次獲取會建立紋理（NativeImages.LoadBytes 要求幀線程且非繪製期），由 MainGame.LoadContent 預熱。
/// </summary>
internal static class SolidTextures
{
    private static Texture2D? _white;

    /// <summary>1x1 不透明白色貼圖；共享引用，調用方不得 Dispose。</summary>
    internal static Texture2D White => _white ??= CreateWhite();

    private static Texture2D CreateWhite()
    {
        using var image = new global::Season.Basic.NativeImageData(1, 1, new byte[] { 255, 255, 255, 255 });
        return NativeImages.LoadBytes(NativeImages.EncodePng(image));
    }
}
