
namespace Zhsan;

/// <summary>
/// Linux（net10.0 TFM）入口：宿主為引擎 Season.Platforms.Linux.LinuxApp（SDL3 + Vulkan + Gtk），
/// 接線方式對照 Windows 的 App.xaml.cs（WinUI 宿主）與引擎 Apps/Engine/Platforms/Linux/Linux.cs。
/// </summary>
internal class Program
{
    static void Main(string[] args)
    {
        // 注入 Linux 平台實現（參考引擎 DeviceServices 的做法），
        // 必須早於任何共享代碼對 PlatformBase.Current 的訪問。
        Platforms.PlatformBase.Initialize(new Platforms.Platform());

        GameManager.MigrationCheck.Initialize();
        if (GameManager.MigrationCheck.Enabled)
            global::Season.Storage.StorageService.DirectoryBase = System.IO.Path.Combine(GameManager.MigrationCheck.RunRoot, "engine");

        AppDomain.CurrentDomain.UnhandledException += Platforms.PlatformBase.ExceptionHandler;

        try
        {
            // 2D 旧游戏迁移，渲染完全走即时 2D 后端（Image2D / Draw2D），
            // 不依赖 3D 主管线：在此开启即时 2D 兼容模式，令引擎初始化时不再编译 3D 主 PSO、
            // 不创建任何离屏目标与特效资源，每帧只执行 Overlay pass 直接输出到后台缓冲。
            // 该开关是一次性的：必须早于平台初始化（LinuxApp.Run → InitializeVulkan），
            // 运行期不支持切回完整 3D 模式（对照 Windows 的 App.xaml.cs 与 Android 的 MainActivity）。
            // 注意：此处不能省略 global:: 前缀，游戏内同名类 Season 会遮蔽全局命名空间。
            global::Season.Rendering.Immediate2DMode.Enabled = true;

            global::Season.Platforms.Linux.LinuxApp.Run(new Game1());
        }
        catch (Exception error)
        {
            if (!GameManager.MigrationCheck.Enabled) throw;
            GameManager.MigrationCheck.Log("fatal", error.ToString());
        }
    }
}
