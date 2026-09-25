using System;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using GameManager;
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
        int rowBytes = checked(image.Width * 4);
        if (image.Width <= 0 || image.Height <= 0 || image.Stride < rowBytes
            || image.PixelSpan.Length < checked(image.Stride * image.Height))
            throw new ArgumentException("Expected a complete straight RGBA8 image.", nameof(image));
        byte[] pixels = new byte[checked(rowBytes * image.Height)];
        for (int y = 0; y < image.Height; y++)
            image.PixelSpan.Slice(y * image.Stride, rowBytes).CopyTo(pixels.AsSpan(y * rowBytes, rowBytes));

        using var output = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        var encoder = Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
            Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, output).GetAwaiter().GetResult();
        // The engine screenshot encoder assumes premultiplied input; CPU asset pixels are straight.
        encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Rgba8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Straight, (uint)image.Width, (uint)image.Height,
            96, 96, pixels);
        encoder.FlushAsync().GetAwaiter().GetResult();
        output.Seek(0);
        using var input = output.AsStreamForRead();
        using var bytes = new MemoryStream();
        input.CopyTo(bytes);
        return bytes.ToArray();
    }

    public static Texture2D LoadBytes(byte[] bytes)
    {
        PlatformBase.Game1.AssertResourceThread();
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
        PlatformBase.Game1.AssertResourceThread();
        MigrationCheck.CheckFile(path);
        if (shape == TextureShape.None) return SeasonResources.LoadTexture(path);
        if (shape != TextureShape.Circle) throw new NotSupportedException($"Image mask {shape} is not supported.");
        using var stream = File.OpenRead(path);
        using var decoded = Services.Image.GetImageFromStream(stream, Path.GetExtension(path));
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
        PlatformBase.Game1.AssertResourceThread();
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
}
