using SeasonXNA.Hosting;
using Platforms;
using Zhsan.GameManager;
using GameManager;

namespace Zhsan;

/// <summary>
/// SeasonXNA 宿主：拥有引擎生命周期（Create/Update/Draw2D/Dispose），
/// 把逻辑游戏 <see cref="MainGame"/> 驱动起来。旧版 MonoGame Game 子类已拆分为
/// 「宿主 Game1 + 逻辑 MainGame」两层，宿主只关心帧线程、时钟与绘制上下文。
/// </summary>
public sealed class Game1 : global::Season.Basic.BaseApp
{
    public MainGame coreGame;
    private readonly DrawContext _context = new();
    private readonly FixedStepClock _clock = new();
    // 後台線程登記、Update 階段排空的幀線程動作（紋理創建/釋放等）。
    private readonly Queue<Action> _frameActions = new();
    private readonly object _frameActionsSync = new();
    private int _frameThread;
    private bool _disposed;

    internal void AssertResourceThread()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_frameThread == 0 || _frameThread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Prepare and release application images on the frame thread.");
        if (coreGame?.IsDrawing == true)
            throw new InvalidOperationException("Prepare application images outside drawing.");
    }

    /// <summary>
    /// 當前線程是否可立即創建/釋放紋理（幀線程且非繪製期）。
    /// 繪製期與後台線程的紋理請求應走延遲機制
    /// （CacheManager.LoadTexture 登記 → Update 階段 DrainPendingTextures）。
    /// </summary>
    internal bool CanPrepareImagesNow =>
        _frameThread != 0 &&
        _frameThread == Environment.CurrentManagedThreadId &&
        coreGame?.IsDrawing != true;

    /// <summary>
    /// 登記需在幀線程上執行的動作（紋理創建/釋放等）；已在幀線程時直接執行，
    /// 否則入隊，由 Update 階段的 Drain 執行。
    /// </summary>
    internal void RunOnFrameThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_frameThread != 0 && _frameThread == Environment.CurrentManagedThreadId)
        {
            action();
            return;
        }
        lock (_frameActionsSync)
        {
            _frameActions.Enqueue(action);
        }
    }

    /// <summary>排空後台線程登記的幀線程動作；必須在繪製區間外（Update 階段）調用。</summary>
    private void DrainFrameActions()
    {
        while (true)
        {
            Action action;
            lock (_frameActionsSync)
            {
                if (_frameActions.Count == 0) return;
                action = _frameActions.Dequeue();
            }
            try
            {
                action();
            }
            catch (Exception error)
            {
                MigrationCheck.Log("frame-action", $"Frame-thread action failed: {error.Message}");
            }
        }
    }

    public Game1()
    {
        PlatformBase.Game1 = this;
        Title = "中华三国志(v1.25.1)";
#if SEASON_MIGRATION_CHECK
        Title = $"ZhsanNew CHECK - offline [{Environment.ProcessId}]";
#endif
        RenderDomain = global::Season.Controls.RenderDomain.Overlay;
        DesignResolution = ParsePreferredResolution();
        BasicResolution = DesignResolution;
        BackgroundColor = new(0, 0, 0, 1);
    }

    /// <summary>解析平台偏好分辨率（如 "1280*720"）；缺失或非法时退回 925*520。</summary>
    private static System.Numerics.Vector2 ParsePreferredResolution()
    {
        var parts = (PlatformBase.PreferResolution ?? string.Empty).Split('*');
        if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            return new(width, height);
        return new(925, 520);
    }

    public IntPtr WindowHandle => FindWindow(null, Title);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string title);

    public override void Create()
    {
        base.Create();
        if (global::Season.Basic.Graphics.Instance?.Immediate2D is null)
            throw new PlatformNotSupportedException("Zhsan requires an Immediate2D backend.");
    }

    public override bool Update(float time, float? alpha = null, float? posX = null,
        float? posY = null, float? posZ = null, float? width = null,
        float? height = null, float? depth = null)
    {
        if (_frameThread == 0) _frameThread = Environment.CurrentManagedThreadId;
        bool result = base.Update(time, alpha, posX, posY, posZ, width, height, depth);
        if (coreGame is null)
        {
            coreGame = new MainGame();
            coreGame.LoadContent(_context);
            coreGame.Init();
        }
        _clock.Advance(time, coreGame.Update);
        coreGame.PrepareCommonResources();
        // 排空繪製期登記的動態材質請求（繪製區間外完成加載）。
        DynamicTextureCache.Drain();
        // 排空繪製期/後台線程登記的緩存材質延遲加載請求（CacheManager.LoadTexture 未命中時登記）。
        CacheManager.DrainPendingTextures();
        // 排空後台線程登記的幀線程動作（如圖片選擇/裁切回調中的紋理更新）。
        DrainFrameActions();
        return result;
    }

    public override void Draw2D(global::Season.Rendering.Draw2D canvas)
    {
        if (coreGame == null) return;
        using var frame = _context.Bind(canvas);
        coreGame.IsDrawing = true;
        try { coreGame.Draw(); }
        catch
        {
            _context.CancelPendingBatches();
            throw;
        }
        finally
        {
            coreGame.IsDrawing = false;
        }
    }

    public override void Dispose()
    {
        if (_disposed) return;
        if (_frameThread != 0 && _frameThread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Dispose the application on its frame thread.");
        _disposed = true;
        var errors = new List<Exception>();
        void Cleanup(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
        if (coreGame != null)
        {
            Cleanup(coreGame.Dispose);
            if (coreGame.SpriteBatch != null) Cleanup(coreGame.SpriteBatch.Dispose);
        }
        Cleanup(DynamicTextureCache.Dispose);
        Cleanup(PlatformBase.Current.CleanupImageCache);
        Cleanup(() => base.Dispose());
        if (errors.Count != 0) throw new AggregateException("Application shutdown failed.", errors);
    }
}
