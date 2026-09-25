
using Android.App;
using Android.Content.PM;
using Android.OS;
using Season.Platforms.Android;
using Season.Rendering;

namespace Zhsan;

// [Activity] 属性不会从跨程序集的 BaseActivity 继承到清单（实测生成的 AndroidManifest 中
// configChanges 等均缺失），故在此显式声明与 BaseActivity 一致的旋转/任务栈配置，
// 避免设备旋转时 Activity 被销毁重建。屏幕方向限定为横屏（SensorLandscape：仅横屏，
// 支持左右两个横向方向跟随传感器，与该游戏横屏 UI 契约一致）。
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    LaunchMode = LaunchMode.SingleTop, AlwaysRetainTaskState = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : BaseActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        // 引擎会话已关闭（HostLifetime、Vulkan 設備與 Immediate2D 後端均為一次性）：
        // 同進程缓存的进程无法再启动新会话（AndroidApp.Run 会直接抛例外），此前的表现是
        // 「重开一直黑屏」（Surface 回调被 _closed 拦下）。改为在新进程冷启动，与首次启动完全一致。
        if (AndroidApp.IsClosed)
        {
            AndroidApp.RelaunchInFreshProcess(this);
            return;
        }

        // Initialize App and DeviceServices only on the first run (Activity will be recreated when the screen orientation changes,
        // However, static Vulkan states and user resources need to be preserved—soft reboot paths reuse the Instance/ Device / Pipeline / texture).
        if (!AndroidApp.IsInitialized)
        {
            // 2D 旧游戏迁移，渲染完全走即时 2D 后端（Image2D / Draw2D），
            // 不依赖 3D 主管线：在此开启即时 2D 兼容模式，令引擎初始化时不再编译 3D 主 PSO、
            // 不创建任何离屏目标与特效资源，每帧只执行 Overlay pass 直接输出到后台缓冲。
            // 该开关是一次性的：必须早于平台初始化（AndroidApp.Run / 首次 SurfaceCreated），
            // 运行期不支持切回完整 3D 模式。
            // 注意：此处不能用 Season.Rendering 限定，游戏内同名类 Season 会遮蔽全局命名空间。
            Immediate2DMode.Enabled = true;

            // 注入 Android 平台實現（對照 Windows 的 App.xaml.cs 與 Linux 的 Linux.cs），
            // 必須早於任何共享代碼對 PlatformBase.Current 的訪問。
            Platforms.PlatformBase.Initialize(new Platforms.Platform());
            AndroidApp.Run(new Game1());
        }

        // Activity 重建時會換新實例，平台側讀取屏幕度量的 PreparePhone/OpenLink 等均經 Activity1，
        // 故每次 OnCreate 都刷新引用。
        if (Platforms.PlatformBase.Current is Platforms.Platform platform)
        {
            platform.SetActivity(this);
        }

        base.OnCreate(savedInstanceState);
    }
}
