using Foundation;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UIKit;
using GameManager;
using Tools;
using Zhsan.GameManager;

namespace Platforms
{
    /// <summary>
    /// 各平台不同的實現（Apple：net10.0-ios / net10.0-maccatalyst 共享編譯，運行時分流，
    /// 宿主分別為引擎 Season.Platforms.iOS.iOSApp 與 Season.Platforms.MacCatalyst.MacCatalystApp）。
    /// 移植基準：SeasoniOS.cs（Apple 端）+ PlatformWin.cs（桌面端契約）。
    /// 資源（MauiAsset）以真實文件落盤於 NSBundle.MainBundle.ResourcePath/Content（iOS 為 .app 根；
    /// Mac Catalyst 為 Contents/Resources，而 AppContext.BaseDirectory 指向 Contents/MonoBundle，
    /// 需要這層換根）；用戶數據走 MigrationCheck.UserRoot（文檔/WorldOfTheThreeKingdoms）；
    /// 圖像處理統一走 NativeImages（Apple 無 System.Drawing 可用）。
    /// </summary>
    public class Platform : PlatformBase
    {
        static Platform()
        {
            // 平台標識只保留基類一份存儲：Mac Catalyst 是桌面窗口（Desktop），iOS 為移動端。
            // 兩者共享同一份源碼，在運行時分流（對照 Linux/iOS 的 PlatFormType 路徑分支）。
            PlatformBase.PlatFormType = OperatingSystem.IsMacCatalyst()
                ? global::Platforms.PlatFormType.Desktop
                : global::Platforms.PlatFormType.iOS;
            PlatformBase.IsMobilePlatForm = !OperatingSystem.IsMacCatalyst();
            // 首選分辨率的權威值在實例構造與 PreparePhone 中由 UIScreen 度量寫入，此處僅保底。
            PlatformBase.PreferResolution = "925*520";
        }

        public Platform()
        {
            // 與 SanguoSeason Apple 一致的啟動默認值（唯一存儲在基類字段）。
            // Channel 保持空串：未上架 App Store，OpenMarket 統一退回官網（見下）。
            KeyBoardAvailable = false;
            PlatformBase.PreferResolution = ComputePreferResolution();
            Location = Assembly.GetExecutingAssembly().Location;
            SolutionDir = AppContext.BaseDirectory;
        }

        #region 屏幕度量與窗口控制

        /// <summary>
        /// 讀取屏幕物理像素度量（寬/高已按寬≥高排序）。返回 false 表示度量不可用
        /// （UIScreen 在極早期時序下訪問失敗），調用方回退。
        /// </summary>
        private static bool TryGetScreenMetrics(out int width, out int height)
        {
            width = 0;
            height = 0;
            try
            {
                double scale = (double)UIScreen.MainScreen.Scale;
                int w = (int)Math.Round((double)UIScreen.MainScreen.Bounds.Width * scale);
                int h = (int)Math.Round((double)UIScreen.MainScreen.Bounds.Height * scale);
                if (w <= 0 || h <= 0)
                {
                    return false;
                }
                width = Math.Max(w, h);
                height = Math.Min(w, h);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 由屏幕尺寸與縮放計算首選分辨率（"寬*高" 整數文本）。
        /// Game1.ParsePreferredResolution 以 int.TryParse 解析，非整數文本（如 "1170.0*2532.0"）
        /// 會使解析整體失敗並回退 925*520，故必須先取整。
        /// </summary>
        private static string ComputePreferResolution()
        {
            return TryGetScreenMetrics(out int width, out int height) ? width + "*" + height : "925*520";
        }

        public override void PreparePhone()
        {
            if (OperatingSystem.IsMacCatalyst())
            {
                // Mac Catalyst 為桌面窗口：走桌面分支（Session.ChangeDisplay:371 依 Setting/
                // PreferResolution），fullScreenDestination 與 Win/Linux 一致保持宿主默認
                // （:472 RealScale 的除法消費點當前處於 InputManager 的 #if false 死代碼內，
                // 與現行 Windows 版行為相同）。
                return;
            }

            // iOS 移動分支（Session.ChangeDisplay:390-395）以 fullScreenDestination 為邏輯度量
            // （滑向比 slope>=1.5 判定 "1000*620"/"1024*768"），且 :472 RealScale 無條件讀取它，
            // 故必須在首個 MainGame 構造時寫入物理像素。
            // 調用點（MainGame ctor:100）在 Platform.MainGame = this（:93）之後，引用安全。
            if (TryGetScreenMetrics(out int width, out int height))
            {
                PreferResolution = width + "*" + height;
                if (MainGame != null)
                {
                    MainGame.fullScreenDestination = new Microsoft.Xna.Framework.Rectangle(0, 0, width, height);
                }
            }
        }

        public override void SetGraphicsWidthHeight(int width, int height)
        {
            // iOS：畫布即物理屏幕分辨率（引擎 iOSApp 管理），尺寸不可變更（對照 SeasoniOS 的空實現）。
            if (OperatingSystem.IsMacCatalyst())
            {
                // Mac Catalyst 為桌面窗口：沿用基類寫入設計分辨率，窗口尺寸由宿主套用。
                base.SetGraphicsWidthHeight(width, height);
            }
        }

        public override void GraphicsApplyChanges()
        {
            if (OperatingSystem.IsMacCatalyst())
            {
                base.GraphicsApplyChanges();
            }
            // iOS：交換鏈尺寸由引擎宿主跟隨屏幕變化自行處理（對照 PlatformAndroid）。
        }

        public override void SetMouseVisible(bool visible)
        {
            // iOS 無光標概念；Mac Catalyst 的光標由引擎宿主管理，此處保持無操作。
        }

        public override void SetWindowAllowUserResizing(bool allow)
        {
            // iOS 無窗口縮放；Mac Catalyst 引擎封裝未提供運行時切換 API，保持無操作。
        }

        public override void SetFullScreen(bool full)
        {
            // iOS/Mac Catalyst 均以系統窗口策略全屏（Session:350 傳入 Setting.DisplayMode），
            // 無程序化全屏切換接口，保持無操作（對照 SeasoniOS）。
        }

        /// <summary>
        /// MainGame.ToggleFullScreen 的切換入口：與 SetFullScreen 同一實現。
        /// </summary>
        public override void SetFullScreen2(bool full)
        {
            SetFullScreen(full);
        }

        public override void SetTimerDisabled(bool timerDisabled)
        {
            try
            {
                UIApplication.SharedApplication.IdleTimerDisabled = timerDisabled;
            }
            catch (Exception)
            {
                // 屏幕常亮控制失敗不影響遊戲主流程。
            }
        }

        public override void SetBarStyle()
        {
            // MainGame ctor:138 無條件調用：隱藏狀態欄。Mac Catalyst 無狀態欄
            // （UIKit 視圖直接嵌入窗口），Selector 不存在時靜默跳過。
            try
            {
                if (UIApplication.SharedApplication.RespondsToSelector(new ObjCRuntime.Selector("setStatusBarHidden:withAnimation:")))
                {
                    UIApplication.SharedApplication.SetStatusBarHidden(true, UIStatusBarAnimation.Fade);
                }
                else
                {
                    UIApplication.SharedApplication.SetStatusBarHidden(true, true);
                }
            }
            catch (Exception)
            {
                // 狀態欄控制失敗不影響遊戲主流程。
            }
        }

        public override void SetOrientations()
        {
            // iOS（iPhone/iPad）限定橫屏：Info.plist 的 UISupportedInterfaceOrientations（含 ~ipad）
            // 已收斂為僅橫向，這裡再主動請求一次橫向幾何更新，確保啟動瞬間（模擬器/設備停在
            // 縱向時）也會轉為橫屏；Mac Catalyst 為桌面窗口，不參與屏幕旋轉，故排除。
            if (OperatingSystem.IsMacCatalyst())
            {
                return;
            }

            try
            {
                var scene = UIApplication.SharedApplication.ConnectedScenes
                    .ToArray().FirstOrDefault(cs => cs is UIWindowScene) as UIWindowScene;

                scene?.RequestGeometryUpdate(new UIWindowSceneGeometryPreferencesIOS(UIInterfaceOrientationMask.Landscape), null);
            }
            catch (Exception)
            {
                // 幾何更新失敗不影響啟動（Info.plist 已聲明橫屏）。
            }
        }

        #endregion

        #region 設備信息

        public override string GetDeviceID()
        {
            return GetDeviceInfo().Replace(" ", "");
        }

        public override string GetDeviceInfo()
        {
            try
            {
                return UIDevice.CurrentDevice.Model + " " + UIDevice.CurrentDevice.SystemName + " "
                    + UIDevice.CurrentDevice.SystemVersion + " " + UIDevice.CurrentDevice.Name;
            }
            catch
            {
                return "";
            }
        }

        public override string GetSystemInfo()
        {
            return System.Environment.OSVersion.Platform + " " + System.Environment.OSVersion.VersionString;
        }

        public override float GetUIScale()
        {
            try
            {
                return (float)UIScreen.MainScreen.Scale;
            }
            catch
            {
                return 1f;
            }
        }

        #endregion

        #region 用戶文件夾處理

        /// <summary>
        /// 用戶數據目錄（以分隔符結尾）：「文檔/WorldOfTheThreeKingdoms」（iOS 為沙盒 Documents），
        /// 遷移檢查構建下切換到隔離目錄（MigrationCheck.UserRoot）。
        /// </summary>
        protected override string UserApplicationDataPath
        {
            get
            {
                string path = MigrationCheck.UserRoot + Path.DirectorySeparatorChar;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        #endregion

        #region 商店/連結

        public override void OpenMarket(string key)
        {
            // 未上架 App Store（無 itms-apps 商品 ID）：統一退回官網，
            // 與 Android 非商店渠道的策略一致。
            OpenLink(WebTools.WebSite);
        }

        public override void OpenReview(string key)
        {
            OpenLink(WebTools.WebSite);
        }

        public override void OpenLink(string link)
        {
            try
            {
                using (var url = new NSUrl(link))
                {
                    UIApplication.SharedApplication.OpenUrl(url);
                }
            }
            catch (Exception ex)
            {
                WebTools.SendErrMsg("OpenUrl打開Web出錯：", ex);
            }
        }

        // Exit 不覆蓋：iOS 平台慣例不允許程序化退出（退回桌面由系統手勢完成），
        // 保持基類的無操作實現。

        #endregion

        #region 輸入捕獲（原 IME 字符流）

        private static readonly List<Character> Chars = new List<Character>();

        public override List<Character> GetChars()
        {
            return Chars;
        }

        public override void ClearChars()
        {
            Chars.Clear();
        }

        #endregion

        #region 加載資源文件（MauiAsset 真實文件）

        /// <summary>
        /// 歸一化資源路徑分隔符：遊戲數據沿用 Windows 習慣書寫路徑（如 Content\Data\tishiText.txt），
        /// 反斜線在 Apple 平台上不是分隔符，故在平台邊界統一轉為 '/'。
        /// </summary>
        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.Replace('\\', '/');
        }

        /// <summary>
        /// MauiAsset 的 Content 根目錄。NSBundle.MainBundle.ResourcePath 對兩個 Apple 平台都正確：
        /// iOS 為 .app 根（AppContext.BaseDirectory 亦指向此處），Mac Catalyst 為 Contents/Resources
        /// （而 AppContext.BaseDirectory 指向 Contents/MonoBundle，需要這層換根）。
        /// </summary>
        private static string ContentRoot => Path.Combine(NSBundle.MainBundle.ResourcePath, "Content");

        /// <summary>
        /// 把調用方給出的資源路徑換根到 MauiAsset 落點：文件已存在則原樣返回；
        /// 否則取 "Content/" 之後的相對路徑重新拼接到 ContentRoot（修復 Mac Catalyst 上
        /// PageResources 以 AppContext.BaseDirectory 拼出的 MonoBundle 路徑）。
        /// ZHSan 調用方普遍帶 "Content/" 前綴（如 "Content/Data/tishiText.txt"），
        /// 相對 SeasoniOS 的裸相對路徑語義擴展了一個前綴分支。
        /// </summary>
        private static string ResolveContentPath(string path)
        {
            string normalized = NormalizePath(path);
            if (File.Exists(normalized)) return normalized;
            const string marker = "/Content/";
            int index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            string relative;
            if (index >= 0)
            {
                relative = normalized.Substring(index + marker.Length);
            }
            else if (normalized.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
            {
                relative = normalized.Substring("Content/".Length);
            }
            else
            {
                relative = normalized.TrimStart('/');
            }
            string candidate = Path.Combine(ContentRoot, relative);
            return File.Exists(candidate) ? candidate : normalized;
        }

        /// <summary>
        /// 加載資源文本
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public override string LoadText(string res)
        {
            lock (PlatformBase.IoLock)
            {
                return File.ReadAllText(ResolveContentPath(res));
            }
        }

        /// <summary>
        /// 加載資源文本行
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public override string[] LoadTexts(string res)
        {
            lock (PlatformBase.IoLock)
            {
                return File.ReadAllLines(ResolveContentPath(res));
            }
        }

        /// <summary>
        /// 加載資源文件
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public override byte[] LoadFile(string res)
        {
            lock (PlatformBase.IoLock)
            {
                using (var dest = new MemoryStream())
                {
                    using (Stream stream = File.OpenRead(ResolveContentPath(res)))
                    {
                        stream.CopyTo(dest);
                        return dest.ToArray();
                    }
                }
            }
        }

        #endregion

        #region 圖片選擇與圖像處理（委托 NativeImages）

        public override void TakePhoto(PlatformTask action)
        {
            if (MigrationCheck.Enabled)
                throw new InvalidOperationException("External file pickers are disabled in the dedicated check build.");

            try
            {
                if (!UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera))
                {
                    WebTools.TakeWarnMsg("当前设备不支持拍照", "TakePhoto",
                        new Exception("Camera source unavailable"));
                    return;
                }
                ChooseTakePictureBase(action, UIImagePickerControllerSourceType.Camera);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("拍照失败", "TakePhoto", ex);
            }
        }

        public override void ChoosePicture(PlatformTask action)
        {
            if (MigrationCheck.Enabled)
                throw new InvalidOperationException("External file pickers are disabled in the dedicated check build.");

            try
            {
                ChooseTakePictureBase(action, UIImagePickerControllerSourceType.PhotoLibrary);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("选择图片失败", "ChoosePicture", ex);
            }
        }

        /// <summary>
        /// 相冊/相機選擇基線（照 SeasoniOS 的 ChooseTakePictureBase）：
        /// 選中圖像 → JPEG 字節 → 統一走 ResizeImageFile/ParamArrayResult 鏈，
        /// 出參語義與桌面端 ChoosePicture 完全一致（ParamArrayResult = avatar+擴展名）。
        /// </summary>
        private void ChooseTakePictureBase(PlatformTask action, UIImagePickerControllerSourceType type)
        {
            // UIKit 呈現必須在主線程（遊戲渲染線程不在主線程）；picker 事件回調亦在主線程觸發，
            // 圖像縮放經 PlatformTask 後台線程執行，不阻塞 UI。
            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                var imagePicker = new UIImagePickerController();
                imagePicker.SourceType = type;
                imagePicker.MediaTypes = UIImagePickerController.AvailableMediaTypes(type);

                imagePicker.FinishedPickingMedia += (sender, e) =>
                {
                    try
                    {
                        string mediaType = e.Info?[UIImagePickerController.MediaType]?.ToString();
                        if (mediaType == "public.image")
                        {
                            UIImage originalImage = e.Info[UIImagePickerController.OriginalImage] as UIImage;
                            UIImage editedImage = e.Info[UIImagePickerController.EditedImage] as UIImage;
                            UIImage selected = editedImage ?? originalImage;
                            if (action != null && selected != null)
                            {
                                byte[] bytes = null;
                                using (var jpeg = selected.AsJPEG())
                                {
                                    bytes = jpeg?.ToArray();
                                }
                                if (bytes != null && bytes.Length > 0)
                                {
                                    // 統一走 ResizeImageFile/ParamArrayResult 鏈，語義與桌面端一致。
                                    const string extension = ".jpg";
                                    var task = new PlatformTask(() => { });
                                    task.OnStartFinish += new AsyncCallback((result) =>
                                    {
                                        string avatar = "";
                                        if (action.ParamArray != null && action.ParamArray.Length > 0)
                                        {
                                            avatar = action.ParamArray[0];
                                        }
                                        action.ParamArrayResult = new string[] { avatar + extension };
                                        action.ParamArrayResultBytes = task.ParamArrayResultBytes;
                                        action.Start();
                                    });
                                    ResizeImageFile(bytes, 540 - 6, 540 - 6, true, task); //270 - 6);  //, 76 * 2, 91 * 2);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WebTools.TakeWarnMsg("读取相册图片失败", "ChoosePicture", ex);
                    }
                    imagePicker.DismissModalViewController(true);
                };

                imagePicker.Canceled += (sender, e) =>
                {
                    imagePicker.DismissModalViewController(true);
                };

                var gameController = UIApplication.SharedApplication.KeyWindow?.RootViewController;
                gameController?.PresentViewController(imagePicker, true, null);
            });
        }

        public override void MirrorPicture(byte[] image, PlatformTask action)
        {
            byte[] bytes = null;
            try
            {
                bytes = NativeImages.Mirror(image);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("镜像图像失败:" + ex.Message.NullToString(), "MirrorPicture", ex);
            }
            if (action != null)
            {
                action.ParamArrayResultBytes = bytes;
                action.Start();
            }
        }

        public override void RotatePicture(byte[] image, int rotate, PlatformTask action)
        {
            byte[] bytes = null;
            try
            {
                bytes = NativeImages.Rotate(image, rotate);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("旋转图像失败:" + ex.Message.NullToString(), "RotatePicture", ex);
            }
            if (action != null)
            {
                action.ParamArrayResultBytes = bytes;
                action.Start();
            }
        }

        public override void CropPicture(byte[] image, int x, int y, int width, int height, PlatformTask action)
        {
            var bytes = NativeImages.Crop(image, x, y, width, height);
            if (action != null)
            {
                action.ParamArrayResultBytes = bytes;
                action.Start();
            }
        }

        /// <summary>
        /// 壓縮圖片
        /// </summary>
        public override void ResizeImageFile(byte[] imageFile, int targetSizeWidth, int targetSizeHeight, bool sameRatio, PlatformTask action)
        {
            byte[] pic = null;

            try
            {
                pic = NativeImages.Resize(imageFile, targetSizeWidth, targetSizeHeight, sameRatio);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("缩放图像失败:" + ex.Message.NullToString(), "ResizeImageFile", ex);
            }

            if (action != null)
            {
                action.ParamArrayResultBytes = pic;
                action.Start();
            }
        }

        public override void CropResizePicture(byte[] image, int x, int y, int width, int height, int targetSizeWidth, int targetSizeHeight, bool sameRatio, PlatformTask action)
        {
            byte[] pic = null;
            try
            {
                pic = NativeImages.Crop(image, x, y, width, height);

                ResizeImageFile(pic, targetSizeWidth, targetSizeHeight, sameRatio, action);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("裁切缩放失败:" + ex.Message.NullToString(), "ResizeImageFile", ex);
            }
        }

        #endregion

        #region 平台圖像服務（委托 NativeImages）

        public override Texture2D LoadImageBytes(byte[] bytes)
        {
            return NativeImages.LoadBytes(bytes);
        }

        public override Texture2D LoadImageFile(string path, TextureShape shape)
        {
            // Apple NativeImages 要求真實文件路徑（無資產回退）：先換根到 MauiAsset 落點
            // （基類 LoadTexture 以 ResolvePath(res) 給出的 Mac Catalyst 路徑指向
            // Contents/MonoBundle，必須改寫；iOS 路徑已存在時原樣直通）。
            return NativeImages.LoadFile(ResolveContentPath(path), shape);
        }

        public override byte[] CropImage(byte[] bytes, int x, int y, int width, int height)
        {
            return NativeImages.Crop(bytes, x, y, width, height);
        }

        public override byte[] EncodePng(global::Season.Basic.INativeImageDecoder image)
        {
            return NativeImages.EncodePng(image);
        }

        public override Texture2D CreateCircle(int radius, Microsoft.Xna.Framework.Color color)
        {
            return NativeImages.Circle(radius, color);
        }

        public override void CleanupImageCache()
        {
            NativeImages.Cleanup();
        }

        public override string TemporaryImageDirectory
        {
            get
            {
                return NativeImages.TemporaryDirectory;
            }
        }

        #endregion
    }
}
