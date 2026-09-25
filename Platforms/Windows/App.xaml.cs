
namespace Zhsan.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    internal static Microsoft.UI.Dispatching.DispatcherQueue Dispatcher { get; private set; }
    public App()
    {
        this.InitializeComponent();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // 注入 Windows 平台实现（参考引擎 DeviceServices 的做法），
        // 必须早于任何共享代码对 PlatformBase.Current 的访问。
        Platforms.PlatformBase.Initialize(new Platforms.Platform());
        Dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        GameManager.MigrationCheck.Initialize();
        if (GameManager.MigrationCheck.Enabled)
            global::Season.Storage.StorageService.DirectoryBase = System.IO.Path.Combine(GameManager.MigrationCheck.RunRoot, "engine");
        try
        {
            // 2D 旧游戏迁移，渲染完全走即时 2D 后端（Image2D / Draw2D），
            // 不依赖 3D 主管线：在此开启即时 2D 兼容模式，令引擎初始化时不再编译 3D 主 PSO、
            // 不创建任何离屏目标与特效资源，每帧只执行 Overlay pass 直接输出到后台缓冲。
            // 该开关是一次性的：必须早于平台初始化（WindowsApp 收到首次 SwapChainPanel
            // SizeChanged 后才执行 InitializeAndRun），运行期不支持切回完整 3D 模式。
            // 注意：用 global:: 前缀避开 同名类 Season 的遮蔽（对照 Android 的 MainActivity）。
            global::Season.Rendering.Immediate2DMode.Enabled = true;

            global::Season.Platforms.Windows.WindowsApp.Run(new Game1());
            await global::Season.Platforms.Windows.WindowsApp.Completion;
        }
        catch (Exception error)
        {
            if (!GameManager.MigrationCheck.Enabled) throw;
            GameManager.MigrationCheck.Log("fatal", error.ToString());
        }
    }
}
