using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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
    /// 各平台不同的實現（Linux / net10.0 TFM，宿主為引擎 Season.Platforms.Linux.LinuxApp）。
    /// 移植基準：PlatformWin.cs；引擎側 Linux 服務（Gtk/SDL/Vulkan/Gdk）承擔平台能力，
    /// 引擎封裝未覆蓋的項（全屏/光標/可縮放）保持無操作。圖像處理統一走 NativeImages
    /// （Linux 無 System.Drawing 可用）。
    /// </summary>
    public class Platform : PlatformBase
    {
        static Platform()
        {
            // 平台標識只保留基類一份存儲（原 new 遮罩字段已移除）。
            PlatformBase.PlatFormType = global::Platforms.PlatFormType.Desktop;
            PlatformBase.IsMobilePlatForm = false;
            PlatformBase.PreferResolution = "1280*720";
        }

        public Platform()
        {
            // 與 Windows 平台一致的啟動默認值（唯一存儲在基類字段）。
            PreferFullMode = "Window";
            QuickTest = true;
            Location = Assembly.GetExecutingAssembly().Location;
            SolutionDir = AppContext.BaseDirectory;
        }

        #region 窗口控制（SDL 封裝未覆蓋的項保持無操作）

        public override void SetMouseVisible(bool visible)
        {
            // SDL 封裝未提供光標顯隱 API，保持系統默認（與 Windows 端語義一致）。
        }

        public override void SetWindowAllowUserResizing(bool allow)
        {
            // SDL 封裝未提供運行時切換 Resizable 的 API，保持窗口創建時的默認（可拉伸）。
        }

        public override void SetFullScreen(bool full)
        {
            // SDL 封裝未提供全屏切換 API，保持窗口模式運行。
        }

        public override void GraphicsApplyChanges()
        {
            Game1?.Resize();
            // 窗口模式下把 SDL 窗口客戶區對齊到遊戲設計分辨率（啟動、分辨率切換、窗口/全屏切換都會走到這裡），
            // 消除畫面兩側黑邊；全屏與最小化狀態會被宿主層忽略。
            if (Game1 != null)
            {
                global::Season.Platforms.Linux.LinuxApp.ApplyWindowClientSize(
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

        ///<summary>
        /// 通過NetworkInterface讀取網卡Mac
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
        /// 用戶數據目錄（以分隔符結尾）：一般為「文檔/WorldOfTheThreeKingdoms」，
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
                // Linux 宿主（LinuxApp.RunLoop）以 BaseApp.Status 非空作為退出信號，
                // 等價於 Windows 側的 WindowsApp.RequestExit。
                global::Season.Basic.DeviceServices.BaseApp.Status ??= "Closed";
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

            // LinuxFileService.PickFiles 內部為 Gtk 原生文件對話框（同步 Run），
            // 阻塞語義與 Windows 端 ChoosePicture 一致。
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

        #region 平台圖像服務（委托 NativeImages）

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

        #endregion
    }
}
