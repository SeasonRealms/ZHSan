using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using GameManager;
using Season.Fonts;
using SeasonXNA.Interop;

namespace Zhsan.GameManager;

/// <summary>
/// 启动期菜单字体后台预热：Session.Init 配置字体键后由帧线程调用 <see cref="Begin"/>，
/// 后台加载 TTF、构造 SpriteFont 并注册预烘焙字形包；完成后置 <see cref="Ready"/> 放行
/// 主菜单文字绘制，首次 GetFont 经 PageResourceStore.LoadFont 收养暂存字体——
/// 帧线程零 IO、零解析。
///
/// 语言切换不受影响：预热期间字体键被外部改写（语言切换）时放弃暂存，
/// 新语言首次绘制仍走既有同步加载路径。预热失败同样降级为同步加载
/// （行为与历史一致，不引入静默丢字）。
/// </summary>
internal static class MenuFontWarmup
{
    const int Idle = 0;
    const int Pending = 1;
    const int Completed = 2;
    const int Failed = 3;

    static readonly object Sync = new();

    /// <summary>后台构造完成的字体（键为预热时的 FontPair）；帧线程收养后移除。</summary>
    static readonly Dictionary<FontPair, SpriteFont> Staged = new();

    static int _phase = Idle;
    static FontPair _pair;

    /// <summary>未处于预热中即视为就绪（未启动/完成/失败均放行绘字）——主菜单文字绘制的门控条件。</summary>
    internal static bool Ready => Volatile.Read(ref _phase) != Pending;

    /// <summary>帧线程：启动后台预热。重复调用无效果。</summary>
    internal static void Begin(FontPair pair)
    {
        lock (Sync)
        {
            if (_phase != Idle) return;
            _pair = pair;
            _phase = Pending;
            _ = Task.Run(WarmupAsync);
        }
    }

    /// <summary>帧线程（PageResourceStore.LoadFont）：收养后台已构造的字体（一次性）。</summary>
    internal static bool TryTakeStaged(FontPair pair,
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out SpriteFont font)
    {
        bool taken;
        lock (Sync)
        {
            taken = Staged.TryGetValue(pair, out font);
            if (taken) Staged.Remove(pair);
        }
        if (taken)
        {
            MigrationCheck.Log("menu-font-warmup", $"adopted staged font on the frame thread: {pair.Name}.");
            return true;
        }
        font = null;
        return false;
    }

    static async Task WarmupAsync()
    {
        var watch = Stopwatch.StartNew();
        try
        {
            var fontPath = Path.Combine(AppContext.BaseDirectory, _pair.Name.Replace('\\', '/'));
            var native = await global::Season.Fonts.Font.CreateAsync(fontPath, _pair.Size);
            var font = SeasonResources.BorrowFont(native, _pair.Size);
            font.DefaultCharacter = '*';
            font.LineSpacing = _pair.Size;
            // 先注册字形包再暂存：收养后的首次绘制即命中预烘焙字形，不经过动态光栅化。
            var packPath = fontPath + ".msdf.pack";
            GlyphPackRegistry.Register(packPath);
            _ = GlyphPackRegistry.RegisterAsync(packPath);
            lock (Sync)
            {
                if (_phase == Pending)
                {
                    // 预热期间字体键已被外部改写（语言切换）：不暂存，交回同步加载路径。
                    if (string.Equals(CacheManager.FontPair.Name, _pair.Name, StringComparison.Ordinal))
                        Staged[_pair] = font;
                    _phase = Completed;
                    MigrationCheck.Log("menu-font-warmup", $"staged {_pair.Name} in {watch.ElapsedMilliseconds} ms.");
                }
            }
        }
        catch (Exception error)
        {
            lock (Sync)
            {
                if (_phase == Pending) _phase = Failed;
            }
            MigrationCheck.Log("menu-font-warmup", $"failed: {error.GetType().Name}: {error.Message}");
        }
    }
}
