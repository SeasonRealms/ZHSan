using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using GameManager;
using Tools;
using Zhsan.GameManager;

namespace Platforms
{
    /// <summary>
    /// 各平台不同的實現（Windows / WinUI 3）
    /// </summary>
    public class Platform : PlatformBase
    {
        static Platform()
        {
            // 平台標識只保留基類一份存儲（原 new 遮罩字段已移除）。
            PlatformBase.PlatFormType = global::Platforms.PlatFormType.Win;
            PlatformBase.IsMobilePlatForm = false;
            PlatformBase.PreferResolution = "1280*720";
        }

        public Platform()
        {
            // 原 new 字段初始化器中的平台默認值，改寫入基類字段（唯一存儲）。
            // 與基類默認值一致的項（DebugMode/ProcessGameData/AssetsPng/
            // IsGuideVisible/KeyBoardAvailable）不再重複聲明。
            PreferFullMode = "Window";
            QuickTest = true;
            Location = Assembly.GetExecutingAssembly().Location;
            SolutionDir = AppContext.BaseDirectory;
        }

        #region 窗口控制（WinUI AppWindow）

        public override void SetMouseVisible(bool visible)
        {
            // WinUI 窗口未提供光標顯隱 API，保持系統默認（與 Linux 端語義一致）。
        }

        public override void SetWindowAllowUserResizing(bool allow)
        {
            Zhsan.WinUI.App.Dispatcher.TryEnqueue(() =>
            {
                var handle = Game1.WindowHandle;
                if (handle == IntPtr.Zero) return;
                var window = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                    Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle));
                if (window.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    presenter.IsResizable = allow;
            });
        }

        public override void SetFullScreen(bool full)
        {
            Zhsan.WinUI.App.Dispatcher.TryEnqueue(() =>
            {
                var handle = Game1.WindowHandle;
                if (handle == IntPtr.Zero) return;
                var window = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                    Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle));
                window.SetPresenter(full ? Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen
                    : Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
            });
        }

        /// <summary>
        /// MainGame.ToggleFullScreen 的切換入口：與 SetFullScreen 同一實現。
        /// </summary>
        public override void SetFullScreen2(bool full)
        {
            SetFullScreen(full);
        }

        public override void GraphicsApplyChanges()
        {
            Game1?.Resize();
            // 窗口模式下把 Windows 窗口客戶區對齊到遊戲設計分辨率（啟動、分辨率切換、窗口/全屏切換都會走到這裡），
            // 消除畫面四周黑邊；全屏模式與最小化狀態會被宿主層忽略，玩家後續手動拉伸也不再干預。
            if (Game1 != null)
            {
                global::Season.Platforms.Windows.WindowsApp.ApplyWindowClientSize(
                    (int)Game1.DesignResolution.X, (int)Game1.DesignResolution.Y);
            }
        }

        #endregion

        #region 設備信息

        public override string GetDeviceID()
        {
            try
            {
                string addr = "";
                var sts = GetMacByNetworkInterface();
                if (sts != null && sts.Count > 0)
                {
                    addr = sts[0];
                }
                return addr;
            }
            catch
            {
                return "";
            }
        }

        //返回描述本地计算机上的网络接口的对象(网络接口也称为网络适配器)。
        public static NetworkInterface[] NetCardInfo()
        {
            return NetworkInterface.GetAllNetworkInterfaces();
        }

        ///<summary>
        /// 通过NetworkInterface读取网卡Mac
        ///</summary>
        ///<returns></returns>
        public static List<string> GetMacByNetworkInterface()
        {
            List<string> macs = new List<string>();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                macs.Add(ni.GetPhysicalAddress().ToString());
            }
            return macs;
        }

        public override string GetDeviceInfo()
        {
            string hostName = "";
            try
            {
                hostName = global::Season.Basic.Graphics.Instance.GetType().Name + " " + System.Net.Dns.GetHostName();
            }
            catch
            {

            }

            return hostName + " " + Session.Resolution;
        }

        public override string GetSystemInfo()
        {
            return System.Environment.OSVersion.Platform + " " + System.Environment.OSVersion.VersionString;
        }

        #endregion

        #region 用戶文件夾處理

        /// <summary>
        /// 用戶數據目錄（以分隔符結尾）：一般為「我的文檔/WorldOfTheThreeKingdoms」，
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

        #region 商店/連結/退出

        public override void OpenMarket(string key)
        {
            OpenLink(WebTools.WebSite);
        }

        public override void OpenReview(string key)
        {
            OpenLink(WebTools.WebSite);
        }

        public override void Exit()
        {
            try
            {
                global::Season.Platforms.Windows.WindowsApp.RequestExit();
            }
            catch (Exception)
            {
                //退出失敗，當不要緊
            }
        }

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

        public override void ChoosePicture(PlatformTask action)
        {
            if (MigrationCheck.Enabled)
                throw new InvalidOperationException("External file pickers are disabled in the dedicated check build.");

            // 统一走引擎设备层选择图片（Windows 实现为 WinRT FileOpenPicker），不再使用
            // WinForms 的 OpenFileDialog：CommonDialog 属 OLE 组件，要求调用线程处于 STA，
            // 而游戏循环运行在 ThreadPool 工作线程（MTA）上，附加调试器时直接抛
            // "Current thread must be set to single thread apartment (STA) mode..."。
            // PickFiles 内部按 UI 线程亲和性调度到 WinUI 主线程（对照 WindowsDialogService），
            // 渲染线程同步等待结果，阻塞语义与 Linux 端 ChoosePicture 一致。
            string fileName = null;

            try
            {
                var files = global::Season.Basic.DeviceServices.File
                    .PickFiles(global::Season.Basic.FileType.Image,
                        new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }, false, true)
                    .GetAwaiter().GetResult();

                if (files != null && files.Count > 0)
                {
                    fileName = files[0].Name;
                    try { files[0].Stream?.Dispose(); } catch { }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("选择图片失败", "ChoosePicture", ex);
            }

            //判断用户是否正确的选择了文件
            if (!string.IsNullOrEmpty(fileName))
            {
                //获取用户选择文件的后缀名
                string extension = Path.GetExtension(fileName);
                //声明允许的后缀名
                string[] str = new string[] { ".jpeg", ".jpg", ".png", ".gif", ".bmp" };
                if (!str.Contains(extension.ToLower()))
                {
                    _ = global::Season.Basic.DeviceServices.Dialog.ShowMessage("提示", "", ["好的", "取消"], "仅能上传jpg,png,gif,bmp格式的图片！");
                }
                else
                {
                    //获取用户选择的文件，并判断文件大小不能超过5000K，fileInfo.Length是以字节为单位的
                    FileInfo fileInfo = new FileInfo(fileName);
                    if (fileInfo.Length > 5000 * 1024)
                    {
                        _ = global::Season.Basic.DeviceServices.Dialog.ShowMessage("提示", "", ["好的", "取消"], "上传的图片不能大于5000K");
                    }
                    else
                    {
                        byte[] bytes = null;

                        lock (PlatformBase.IoLock)
                        {
                            bytes = File.ReadAllBytes(fileName);
                        }

                        var task = new PlatformTask(() => { });
                        task.OnStartFinish += new AsyncCallback((result) =>
                        {
                            if (action != null)
                            {
                                string avatar = "";
                                if (action.ParamArray != null && action.ParamArray.Length > 0)
                                {
                                    avatar = action.ParamArray[0];
                                }
                                action.ParamArrayResult = new string[] { avatar + extension };
                                action.ParamArrayResultBytes = task.ParamArrayResultBytes;
                                action.Start();
                            }
                        });

                        ResizeImageFile(bytes, 540 - 6, 540 - 6, true, task); //270 - 6);  //, 76 * 2, 91 * 2);
                    }
                }
            }
        }

        static byte[] SavePngFromBitmap(System.Drawing.Bitmap bitmap)
        {
            var imageStream = new MemoryStream();
            using (imageStream)
            {
                // Save bitmap in some format.
                bitmap.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                imageStream.Position = 0;

                // Do something with the memory stream. For example:
                byte[] imageBytes = imageStream.ToArray();
                // Save bytes to the database.
                return imageBytes;
            }
        }

        public override void MirrorPicture(byte[] image, PlatformTask action)
        {
            RotateFlipType flip = RotateFlipType.RotateNoneFlipX;
            using (MemoryStream ms = new MemoryStream(image))
            {
                var img = System.Drawing.Image.FromStream(ms);

                var bmp = new System.Drawing.Bitmap(img);

                using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(bmp))
                {
                    gfx.Clear(System.Drawing.Color.White);
                    gfx.DrawImage(img, 0, 0, img.Width, img.Height);
                }

                bmp.RotateFlip(flip);

                var bytes = SavePngFromBitmap(bmp);

                if (action != null)
                {
                    action.ParamArrayResultBytes = bytes;
                    action.Start();
                }
            }
        }

        public override void RotatePicture(byte[] image, int rotate, PlatformTask action)
        {
            RotateFlipType flip = RotateFlipType.RotateNoneFlipNone;
            if (rotate == 0)
            {

            }
            else if (rotate == 1)
            {
                flip = RotateFlipType.Rotate90FlipNone;
            }
            else if (rotate == 2)
            {
                flip = RotateFlipType.Rotate180FlipNone;
            }
            else if (rotate == 3)
            {
                flip = RotateFlipType.Rotate270FlipNone;
            }
            using (MemoryStream ms = new MemoryStream(image))
            {
                var img = System.Drawing.Image.FromStream(ms);

                var bmp = new System.Drawing.Bitmap(img);

                using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(bmp))
                {
                    gfx.Clear(System.Drawing.Color.White);
                    gfx.DrawImage(img, 0, 0, img.Width, img.Height);
                }

                bmp.RotateFlip(flip);

                var bytes = SavePngFromBitmap(bmp);
                if (action != null)
                {
                    action.ParamArrayResultBytes = bytes;
                    action.Start();
                }
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
        /// <param name="imageFile"></param>
        /// <param name="targetSizeWidth"></param>
        /// <param name="targetSizeHeight"></param>
        /// <param name="sameRatio"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public override void ResizeImageFile(byte[] imageFile, int targetSizeWidth, int targetSizeHeight, bool sameRatio, PlatformTask action)
        {
            byte[] pic = null;

            try
            {
                using (System.Drawing.Image oldImage = System.Drawing.Image.FromStream(new MemoryStream(imageFile)))
                {
                    System.Drawing.Size newSize;

                    if (sameRatio)
                    {
                        float scale = GameTools.AutoSetScale(oldImage.Width, oldImage.Height, targetSizeWidth, targetSizeHeight);
                        newSize = new System.Drawing.Size(Convert.ToInt32(oldImage.Width * scale), Convert.ToInt32(oldImage.Height * scale));
                    }
                    else
                    {
                        newSize = new System.Drawing.Size(Convert.ToInt32(targetSizeWidth), Convert.ToInt32(targetSizeHeight));
                    }

                    using (System.Drawing.Bitmap newImage = new System.Drawing.Bitmap(newSize.Width, newSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    {
                        using (System.Drawing.Graphics canvas = System.Drawing.Graphics.FromImage(newImage))
                        {
                            canvas.SmoothingMode = SmoothingMode.AntiAlias;
                            canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            canvas.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            canvas.DrawImage(oldImage, new System.Drawing.RectangleF(new System.Drawing.PointF(0, 0), newSize));
                            MemoryStream m = new MemoryStream();
                            newImage.Save(m, System.Drawing.Imaging.ImageFormat.Jpeg);
                            pic = m.GetBuffer();
                        }
                    }
                }
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
            try
            {
                byte[] pic = NativeImages.Crop(image, x, y, width, height);

                ResizeImageFile(pic, targetSizeWidth, targetSizeHeight, sameRatio, action);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("裁切缩放失败:" + ex.Message.NullToString(), "ResizeImageFile", ex);
            }
        }

        #region 平台圖像服務（委托 NativeImages / NativeTextEditor）

        public override Texture2D LoadImageBytes(byte[] bytes)
        {
            return NativeImages.LoadBytes(bytes);
        }

        public override Texture2D LoadImageFile(string path, TextureShape shape)
        {
            return NativeImages.LoadFile(path, shape);
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

        public override async Task<string?> EditText(string title, string description, string text, bool password)
        {
            return await NativeTextEditor.Edit(title, description, text, password);
        }

        #endregion
    }
}
