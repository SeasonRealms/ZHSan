using System;
using System.IO;
using System.Security.Cryptography;
using GameManager;
using Microsoft.Xna.Framework.Graphics;
using SeasonXNA.Interop;
using Services = global::Season.Basic.DeviceServices;
using ImageData = global::Season.Basic.NativeImageData;

namespace Zhsan.GameManager;

/// <summary>CPU preparation before upload. Never reads pixels back from a GPU texture.</summary>
internal static class NativeImages
{
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "WorldOfTheThreeKingdoms",
        $"{Environment.ProcessId}-{Guid.NewGuid():N}");
    internal static string TemporaryDirectory => CacheRoot;

    public static void Cleanup()
    {
        if (Directory.Exists(CacheRoot)) Directory.Delete(CacheRoot, recursive: true);
    }

    internal static byte[] EncodePng(global::Season.Basic.INativeImageDecoder image)
    {
        ArgumentNullException.ThrowIfNull(image);
        // Android 側由 Bitmap PNG 壓縮承擔編碼（RGBA8 → ARGB8888 → Png）：
        // 它按 straight RGBA8 語義保存，與 CPU 資產像素一致（區別於 Windows 截圖管線的預乘假設）。
        return Services.Image.SaveImage(image, global::Season.Basic.ImageFormat.Png);
    }

    public static Texture2D LoadBytes(byte[] bytes)
    {
        Platforms.PlatformBase.Game1.AssertResourceThread();
        ArgumentNullException.ThrowIfNull(bytes);
        Directory.CreateDirectory(CacheRoot);
        string path = Path.Combine(CacheRoot, Convert.ToHexString(SHA256.HashData(bytes)) + ".png");
        if (!File.Exists(path))
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var decoded = Services.Image.GetImageFromStream(stream, "");
            File.WriteAllBytes(path, EncodePng(decoded));
        }
        return SeasonResources.LoadTexture(path);
    }

    public static Texture2D LoadFile(string path, TextureShape shape)
    {
        Platforms.PlatformBase.Game1.AssertResourceThread();
        MigrationCheck.CheckFile(path);
        // 內容資產打包在 APK assets 內，不是真實文件：統一歸一化為 assets 相對路徑
        // （"Content/..."），由引擎經 AndroidDevice.Assets.Open + 本地緩存解碼；
        // 真實文件（用戶照片、暫存文件）仍走原絕對路徑。
        string assetPath = ToAssetPath(path);
        if (shape == TextureShape.None) return SeasonResources.LoadTexture(File.Exists(path) ? path : assetPath);
        if (shape != TextureShape.Circle) throw new NotSupportedException($"Image mask {shape} is not supported.");
        using var stream = File.Exists(path) ? File.OpenRead(path) : Platforms.Platform.Activity1.Assets.Open(assetPath);
        using var decoded = Services.Image.GetImageFromStream(stream, Path.GetExtension(assetPath));
        byte[] pixels = new byte[checked(decoded.Width * decoded.Height * 4)];
        for (int y = 0; y < decoded.Height; y++)
            decoded.PixelSpan.Slice(y * decoded.Stride, decoded.Width * 4).CopyTo(pixels.AsSpan(y * decoded.Width * 4));
        int cx = decoded.Width / 2, cy = decoded.Height / 2;
        long radius2 = (long)cx * cx;
        for (int y = 0; y < decoded.Height; y++)
            for (int x = 0; x < decoded.Width; x++)
                if ((long)(x - cx) * (x - cx) + (long)(y - cy) * (y - cy) > radius2)
                    pixels.AsSpan((y * decoded.Width + x) * 4, 4).Clear();
        using var masked = new ImageData(decoded.Width, decoded.Height, pixels);
        return LoadBytes(EncodePng(masked));
    }

    /// <summary>
    /// 將 Windows 慣例書寫的資源路徑（如 AppContext.BaseDirectory 拼出的
    /// ".../Content/Textures/MainMenu\Background.jpg"）歸一化為 APK assets 相對路徑。
    /// </summary>
    private static string ToAssetPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        int index = normalized.IndexOf("/Content/", StringComparison.Ordinal);
        if (index >= 0) return normalized.Substring(index + 1);
        if (normalized.StartsWith("Content/", StringComparison.Ordinal)) return normalized;
        return normalized;
    }

    public static byte[] Crop(byte[] bytes, int x, int y, int width, int height)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var source = Services.Image.GetImageFromStream(stream, "");
        if (x < 0 || y < 0 || width <= 0 || height <= 0
            || (long)x + width > source.Width || (long)y + height > source.Height)
            throw new ArgumentOutOfRangeException(nameof(width));
        var pixels = new byte[checked(width * height * 4)];
        for (int row = 0; row < height; row++)
            source.PixelSpan.Slice((y + row) * source.Stride + x * 4, width * 4)
                .CopyTo(pixels.AsSpan(row * width * 4));
        using var cropped = new ImageData(width, height, pixels);
        return EncodePng(cropped);
    }

    public static Texture2D Circle(int radius, Microsoft.Xna.Framework.Color color)
    {
        Platforms.PlatformBase.Game1.AssertResourceThread();
        if (radius <= 0 || radius > 4096) throw new ArgumentOutOfRangeException(nameof(radius));
        if (color.R > color.A || color.G > color.A || color.B > color.A)
            throw new ArgumentException("Circle color requires RGB <= Alpha.", nameof(color));
        int size = checked(radius * 2);
        var pixels = new byte[checked(size * size * 4)];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if ((long)(x - radius) * (x - radius) + (long)(y - radius) * (y - radius) <= (long)radius * radius)
                {
                    int offset = (y * size + x) * 4;
                    if (color.A > 0)
                    {
                        pixels[offset] = (byte)(color.R * 255 / color.A);
                        pixels[offset + 1] = (byte)(color.G * 255 / color.A);
                        pixels[offset + 2] = (byte)(color.B * 255 / color.A);
                    }
                    pixels[offset + 3] = color.A;
                }
        using var image = new ImageData(size, size, pixels);
        return LoadBytes(EncodePng(image));
    }

    /// <summary>Decodes straight RGBA8 rows and flattens alpha onto white (matches the Windows preview pipeline).</summary>
    private static byte[] DecodeFlat(byte[] bytes, out int width, out int height)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var source = Services.Image.GetImageFromStream(stream, "");
        width = source.Width;
        height = source.Height;
        var pixels = new byte[checked(width * height * 4)];
        for (int y = 0; y < height; y++)
            source.PixelSpan.Slice(y * source.Stride, width * 4).CopyTo(pixels.AsSpan(y * width * 4));
        for (int i = 0; i < pixels.Length; i += 4)
        {
            int a = pixels[i + 3];
            if (a == 255) continue;
            pixels[i] = (byte)((pixels[i] * a + 255 * (255 - a) + 127) / 255);
            pixels[i + 1] = (byte)((pixels[i + 1] * a + 255 * (255 - a) + 127) / 255);
            pixels[i + 2] = (byte)((pixels[i + 2] * a + 255 * (255 - a) + 127) / 255);
            pixels[i + 3] = 255;
        }
        return pixels;
    }

    /// <summary>Horizontal mirror (equivalent to RotateNoneFlipX).</summary>
    public static byte[] Mirror(byte[] bytes)
    {
        byte[] src = DecodeFlat(bytes, out int width, out int height);
        var pixels = new byte[checked(width * height * 4)];
        for (int y = 0; y < height; y++)
        {
            var row = src.AsSpan(y * width * 4, width * 4);
            var dst = pixels.AsSpan(y * width * 4, width * 4);
            for (int x = 0; x < width; x++)
                row.Slice((width - 1 - x) * 4, 4).CopyTo(dst.Slice(x * 4, 4));
        }
        using var image = new ImageData(width, height, pixels);
        return EncodePng(image);
    }

    /// <summary>Clockwise rotation in 90-degree steps (rotate: 0-3), matching RotateFlipType.Rotate90/180/270.</summary>
    public static byte[] Rotate(byte[] bytes, int rotate)
    {
        rotate = ((rotate % 4) + 4) % 4;
        byte[] src = DecodeFlat(bytes, out int width, out int height);
        int outW = rotate % 2 == 0 ? width : height;
        int outH = rotate % 2 == 0 ? height : width;
        var pixels = new byte[checked(outW * outH * 4)];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int dx, dy;
                if (rotate == 0) { dx = x; dy = y; }
                else if (rotate == 1) { dx = height - 1 - y; dy = x; }
                else if (rotate == 2) { dx = width - 1 - x; dy = height - 1 - y; }
                else { dx = y; dy = width - 1 - x; }
                src.AsSpan((y * width + x) * 4, 4).CopyTo(pixels.AsSpan((dy * outW + dx) * 4, 4));
            }
        using var image = new ImageData(outW, outH, pixels);
        return EncodePng(image);
    }

    /// <summary>Bilinear CPU resize into a JPEG (same pipeline role as the Windows ResizeImageFile).</summary>
    public static byte[] Resize(byte[] bytes, int targetWidth, int targetHeight, bool sameRatio)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var source = Services.Image.GetImageFromStream(stream, "");
        int outW, outH;
        if (sameRatio)
        {
            float scale = Tools.GameTools.AutoSetScale(source.Width, source.Height, targetWidth, targetHeight);
            outW = Math.Max(1, (int)Math.Round(source.Width * scale));
            outH = Math.Max(1, (int)Math.Round(source.Height * scale));
        }
        else
        {
            outW = Math.Max(1, targetWidth);
            outH = Math.Max(1, targetHeight);
        }

        var src = source.PixelSpan;
        int stride = source.Stride;
        int inW = source.Width, inH = source.Height;
        var pixels = new byte[checked(outW * outH * 4)];
        float sx = (float)inW / outW;
        float sy = (float)inH / outH;
        for (int y = 0; y < outH; y++)
        {
            float fy = Math.Min((y + 0.5f) * sy - 0.5f, inH - 1);
            if (fy < 0) fy = 0;
            int y0 = (int)fy;
            int y1 = Math.Min(y0 + 1, inH - 1);
            float wy = fy - y0;
            for (int x = 0; x < outW; x++)
            {
                float fx = Math.Min((x + 0.5f) * sx - 0.5f, inW - 1);
                if (fx < 0) fx = 0;
                int x0 = (int)fx;
                int x1 = Math.Min(x0 + 1, inW - 1);
                float wx = fx - x0;
                int o = (y * outW + x) * 4;
                for (int c = 0; c < 4; c++)
                {
                    float v00 = src[y0 * stride + x0 * 4 + c];
                    float v10 = src[y0 * stride + x1 * 4 + c];
                    float v01 = src[y1 * stride + x0 * 4 + c];
                    float v11 = src[y1 * stride + x1 * 4 + c];
                    float v = (v00 * (1 - wx) + v10 * wx) * (1 - wy) + (v01 * (1 - wx) + v11 * wx) * wy;
                    pixels[o + c] = (byte)Math.Clamp((int)MathF.Round(v), 0, 255);
                }
            }
        }
        using var image = new ImageData(outW, outH, pixels);
        return Services.Image.SaveImage(image, global::Season.Basic.ImageFormat.Jpeg);
    }
}
