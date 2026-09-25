using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameManager;
using Tools;
using Zhsan.GameManager;

namespace Platforms
{
    /// <summary>
    /// 各平台不同的實現（Android / net10.0-android TFM，宿主為引擎 Season.Platforms.Android.AndroidApp）。
    /// 移植基準：SeasonAndroid.cs（SanguoSeason）+ PlatformWin.cs（ZHSan 桌面端契約）。
    /// 資源讀取走 APK assets（Activity1.Assets.Open），用戶數據走應用私有目錄（FilesDir）；
    /// 圖像處理統一走 NativeImages（Android 無 System.Drawing 可用）。
    /// </summary>
    public class Platform : PlatformBase
    {
        static Platform()
        {
            // 平台標識只保留基類一份存儲（原 new 遮罩字段已移除）。
            PlatformBase.PlatFormType = global::Platforms.PlatFormType.Android;
            PlatformBase.IsMobilePlatForm = true;
            PlatformBase.PreferResolution = "800*480";
        }

        public Platform()
        {
            // 與 SanguoSeason Android 一致的啟動默認值（唯一存儲在基類字段）。
            // QuickTest 保持基類默認 false（僅 Win/Linux 桌面端開啟）。
            Channel = "PlayStore";
            KeyBoardAvailable = false;
            Location = Assembly.GetExecutingAssembly().Location;
            SolutionDir = AppContext.BaseDirectory;
        }

        /// <summary>
        /// 當前 Activity 引用：由 MainActivity.OnCreate 注入（Activity 重建時刷新）。
        /// 平台側讀取屏幕度量、資產流（Assets.Open）與外部 Intent 均經此。
        /// </summary>
        public static Android.App.Activity Activity1;

        public void SetActivity(Android.App.Activity activity)
        {
            Activity1 = activity;
        }

        #region 屏幕度量與窗口控制

        public override void SetGraphicsWidthHeight(int width, int height)
        {
            // 新管線契約（對照 SeasonAndroid）：Android 舊世界的 backbuffer 即物理屏幕，
            // 800×480 邏輯畫面由 Session.ChangeDisplay 以 SpriteScale=(W/800, H/480) 非均勻
            // 拉伸鋪滿全屏。畫布設為物理分辨率後，引擎 CanvasToOutput 化為恒等變換，
            // 既有拉伸路徑逐像素等價。
            // 參數 width/height 為舊實現的邏輯分辨率請求（等於 fullScreenDestination），
            // Android 以物理度量為準（System.Math 全限定：避免與 Java.Lang.Math 歧義）。
            Display display = Activity1.WindowManager.DefaultDisplay;
            DisplayMetrics metrics = new DisplayMetrics();
            display.GetRealMetrics(metrics);
            int screenWidth = System.Math.Max(metrics.WidthPixels, metrics.HeightPixels);
            int screenHeight = System.Math.Min(metrics.WidthPixels, metrics.HeightPixels);
            var game = Game1;
            if (game != null)
            {
                game.DesignResolution = new(screenWidth, screenHeight);
                game.BasicResolution = game.DesignResolution;
            }
        }

        public override void GraphicsApplyChanges()
        {
            // 交換鏈尺寸由引擎 Android 宿主跟隨物理屏幕變化自行處理，此處無需重排。
        }

        public override void SetTimerDisabled(bool timerDisabled)
        {
            // 引擎宿主未提供屏幕常亮控制，保持系統默認。
        }

        public override void PreparePhone()
        {
            // 移動端分支（Session.ChangeDisplay:390-395）以 fullScreenDestination 為邏輯分辨率，
            // 且 :472 RealScale 無條件讀取它，故必須在首個 MainGame 構造時寫入物理度量。
            // 調用點（MainGame ctor:100）在 Platform.MainGame = this（:93）之後，引用安全。
            Display d = Activity1.WindowManager.DefaultDisplay;
            DisplayMetrics dm = new DisplayMetrics();
            d.GetRealMetrics(dm);
            if (dm.WidthPixels >= dm.HeightPixels)
            {
                PreferResolution = dm.WidthPixels + "*" + dm.HeightPixels;
                if (MainGame != null)
                {
                    MainGame.fullScreenDestination = new Microsoft.Xna.Framework.Rectangle(0, 0, dm.WidthPixels, dm.HeightPixels);
                }
            }
            else
            {
                PreferResolution = dm.HeightPixels + "*" + dm.WidthPixels;
                if (MainGame != null)
                {
                    MainGame.fullScreenDestination = new Microsoft.Xna.Framework.Rectangle(0, 0, dm.HeightPixels, dm.WidthPixels);
                }
            }
        }

        public override void SetFullScreen(bool full)
        {
            // 全屏由 Activity 窗口標誌控制（GraphicsDeviceManager 已隨 MonoGame 移除）。
            var window = Activity1?.Window;
            if (window == null)
            {
                return;
            }
            // 渲染線程經 Game1.Update -> MainGame.Init -> Session.ChangeDisplay 調用本方法；
            // Window.AddFlags/ClearFlags 內部走 ViewRootImpl.checkThread 校驗，必須在 UI 線程
            // 執行，否則啟動首幀即拋 CalledFromWrongThreadException。
            Activity1.RunOnUiThread(() =>
            {
                if (full)
                {
                    window.AddFlags(WindowManagerFlags.Fullscreen);
                }
                else
                {
                    window.ClearFlags(WindowManagerFlags.Fullscreen);
                }
            });
        }

        /// <summary>
        /// MainGame.ToggleFullScreen 的切換入口：與 SetFullScreen 同一實現。
        /// </summary>
        public override void SetFullScreen2(bool full)
        {
            SetFullScreen(full);
        }

        public override void SetOrientations()
        {
            // MainActivity 清單已聲明 SensorLandscape（橫屏契約），運行時無需再設置。
        }

        #endregion

        #region 設備信息

        public override string GetDeviceID()
        {
            try
            {
                return Android.Provider.Settings.Secure.GetString(
                    Activity1.ContentResolver, Android.Provider.Settings.Secure.AndroidId) ?? "";
            }
            catch
            {
                return "";
            }
        }

        public override string GetDeviceInfo()
        {
            return Build.Manufacturer + " " + Build.Model + " " + Build.VERSION.SdkInt + " " + Build.VERSION.Release;
        }

        public override string GetSystemInfo()
        {
            return System.Environment.OSVersion.Platform + " " + System.Environment.OSVersion.VersionString;
        }

        #endregion

        #region 用戶文件夾處理

        /// <summary>
        /// 用戶數據目錄（以分隔符結尾）：桌面端的「我的文檔/WorldOfTheThreeKingdoms」在 Android
        /// 不可寫（SpecialFolder 為空時會退化成不可寫的相對路徑），故以應用私有目錄 FilesDir
        /// 為根；遷移檢查構建下沿用隔離目錄（MigrationCheck.UserRoot）。
        /// </summary>
        protected override string UserApplicationDataPath
        {
            get
            {
                string path = MigrationCheck.Enabled
                    ? MigrationCheck.UserRoot + Path.DirectorySeparatorChar
                    : Activity1.FilesDir.AbsolutePath + "/WorldOfTheThreeKingdoms/";
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
            if (System.String.IsNullOrEmpty(Channel))
            {
                // 非商店渠道：退回官網下載頁（ZHSan 無 SanguoSeason 的 WebSiteView 下載入口）。
                OpenLink(WebTools.WebSite);
            }
            else
            {
                var uri = Android.Net.Uri.Parse("market://details?id=" + Activity1.ApplicationContext.PackageName);
                var rateAppIntent = new Intent(Intent.ActionView, uri);
                if (Activity1.PackageManager.QueryIntentActivities(rateAppIntent, 0).Count > 0)
                {
                    Activity1.StartActivity(rateAppIntent);
                }
            }
        }

        public override void OpenReview(string key)
        {
            OpenLink(WebTools.WebSite);
        }

        public override void OpenLink(string link)
        {
            try
            {
                var uri = Android.Net.Uri.Parse(link);
                var intent = new Intent(Intent.ActionView, uri);
                Activity1.StartActivity(intent);
            }
            catch (Exception ex)
            {
                WebTools.SendErrMsg("StartActivity打開Web出錯：", ex);
            }
        }

        public override void Exit()
        {
            try
            {
                Activity1?.Finish();
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

        #region 加載資源文件（APK assets）

        /// <summary>
        /// 歸一化資源路徑為 APK assets 相對路徑（"Content/..."）：遊戲數據沿用 Windows
        /// 習慣書寫（如 Content\Data\tishiText.txt），反斜線在 Android 資產路徑中不是
        /// 分隔符（Assets.Open 按字面字符查找）；絕對路徑中提取 "/Content/" 起的段，
        /// 已帶前綴的原樣返回，裸相對路徑補前綴。
        /// </summary>
        private static string NormalizeAssetPath(string res)
        {
            if (string.IsNullOrEmpty(res)) return res;
            string path = res.Replace('\\', '/');
            int index = path.IndexOf("/Content/", StringComparison.Ordinal);
            if (index >= 0) return path.Substring(index + 1);
            if (path.StartsWith("Content/", StringComparison.Ordinal)) return path;
            return "Content/" + path;
        }

        /// <summary>
        /// 加載資源文本（內容資產打包在 APK assets 內，不是真實文件，必須走 Assets.Open；
        /// 基類的 File.ReadAllText(ResolvePath) 僅適用桌面端）。
        /// </summary>
        public override string LoadText(string res)
        {
            lock (PlatformBase.IoLock)
            {
                using (var stream = Activity1.Assets.Open(NormalizeAssetPath(res)))
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// 加載資源文本行
        /// </summary>
        public override string[] LoadTexts(string res)
        {
            lock (PlatformBase.IoLock)
            {
                using (var stream = Activity1.Assets.Open(NormalizeAssetPath(res)))
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        List<string> texts = new List<string>();
                        while (!streamReader.EndOfStream)
                        {
                            texts.Add(streamReader.ReadLine());
                        }
                        return texts.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 加載資源文本行：真實文件優先（用戶數據、暫存），未命中再按資產候選讀取
        /// （MainMenuScreen 讀取 MODs\{ID}\{ID}.txt 元數據依賴此路徑，不能走強制
        /// Content 前綴的 LoadTexts）。兩處皆無時返回 null：調用方以 NullToEmptyArray
        /// 兜底（基類此處會拋 FileNotFoundException，Android 資產層不存在該真實文件
        /// 路徑，統一走 null 分支）。
        /// </summary>
        public override string[] ReadAllLines(string file)
        {
            if (String.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            string path = ResolvePath(file);
            CheckFileAccess(path);
            if (File.Exists(path))
            {
                return File.ReadAllLines(path);
            }

            foreach (string candidate in AssetCandidates(file))
            {
                if (!AssetFileExists(candidate))
                {
                    continue;
                }

                lock (PlatformBase.IoLock)
                {
                    using (var stream = Activity1.Assets.Open(candidate))
                    {
                        using (var streamReader = new StreamReader(stream))
                        {
                            List<string> texts = new List<string>();
                            while (!streamReader.EndOfStream)
                            {
                                texts.Add(streamReader.ReadLine());
                            }
                            return texts.ToArray();
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 加載資源文件
        /// </summary>
        public override byte[] LoadFile(string res)
        {
            lock (PlatformBase.IoLock)
            {
                using (var dest = new MemoryStream())
                {
                    using (Stream stream = Activity1.Assets.Open(NormalizeAssetPath(res)))
                    {
                        stream.CopyTo(dest);
                        return dest.ToArray();
                    }
                }
            }
        }

        #endregion

        #region 目錄枚舉與存在性判定（APK assets）

        /// <summary>
        /// 資產列表緩存（ordinal）：APK 內容在進程內不可變（重裝即重啟），緩存無
        /// 失效需求。供枚舉與存在性判定共用，避免熱路徑（GetPersonPortraitPath
        /// 逐次嘗試 5 個候選路徑、寶物逐幀 FileExists）反復走 JNI 列目錄。
        /// 列表失敗（路徑非法）緩存為空集，與「空目錄」同樣按「無內容」處理
        /// （APK 不打包空目錄，兩者無需區分）。
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> AssetListCache =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        private static readonly HashSet<string> EmptyAssets = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// AssetManager.List 的容錯包裝（與本類資產訪問同一把 IoLock）：路徑非法
        /// （如含 ".."）時 Java 側拋 IOException，按「無內容」（空集）緩存。
        /// </summary>
        private static HashSet<string> ListAsset(string assetDir)
        {
            lock (PlatformBase.IoLock)
            {
                if (AssetListCache.TryGetValue(assetDir, out HashSet<string> cached))
                {
                    return cached;
                }

                HashSet<string> entries;
                try
                {
                    entries = new HashSet<string>(Activity1.Assets.List(assetDir) ?? Array.Empty<string>(),
                        StringComparer.Ordinal);
                }
                catch (Java.IO.IOException)
                {
                    entries = EmptyAssets;
                }

                AssetListCache[assetDir] = entries;
                return entries;
            }
        }

        /// <summary>
        /// 資源查找的資產候選路徑（按優先序）：
        /// - 已引用 Content 的輸入（含絕對路徑中的 "/Content/"）：僅 "Content/..." 資產路徑；
        /// - 相對路徑（如 "MODs"、"Portraits"，桌面端為 Content 的同級目錄）：先按桌面同級
        ///   佈局取資產根形式（"MODs"，該目錄放進 Resources\Raw 根即打包到 assets 根），
        ///   未命中再退回 "Content/" 前綴形式；
        /// - 其餘絕對路徑（用戶數據目錄等真實文件）：無候選，僅走真實文件系統。
        /// </summary>
        private static string[] AssetCandidates(string dir)
        {
            string raw = dir.Replace('\\', '/').TrimEnd('/');
            if (raw.Length == 0)
            {
                return Array.Empty<string>();
            }
            if (raw.StartsWith("Content/", StringComparison.Ordinal) || raw == "Content")
            {
                return new string[] { raw };
            }
            if (raw.IndexOf("/Content/", StringComparison.Ordinal) >= 0)
            {
                return new string[] { NormalizeAssetPath(raw) };
            }
            if (Path.IsPathRooted(raw))
            {
                return Array.Empty<string>();
            }
            return new string[] { raw, "Content/" + raw };
        }

        /// <summary>
        /// 枚舉文件：內容資產打包在 APK 內，Directory.GetFiles 對其恒為「目錄不存在」
        /// （基類返回 null），Android 上目錄掃描類加載（如 GameTextures 的地形/兵模/
        /// 建築紋理枚舉）將全部落空甚至空引用崩潰；此處在真實文件系統未命中時改為
        /// 枚舉 assets 候選（見 AssetCandidates）。返回的資產路徑為「Content/...」
        /// 相對形式，可直接作為資源名餵給 LoadTexture/LoadFile（經 NativeImages.
        /// ToAssetPath 原樣識別）。
        /// all 參數沿用基類註釋的桌面行為：恒遞歸。
        /// </summary>
        public override string[] GetFiles(string dir, bool all)
        {
            return EnumerateFiles(dir, true);
        }

        /// <summary>
        /// 枚舉文件（基礎位置）：遞歸級別由 all 決定，與基類 GetFilesBasic 語義一致。
        /// </summary>
        public override string[] GetFilesBasic(string dir, bool all = false)
        {
            return EnumerateFiles(dir, all);
        }

        /// <summary>
        /// GetFiles/GetFilesBasic 共用實現：真實路徑（用戶數據、暫存文件）優先，
        /// 未命中再依序枚舉資產候選（首個有內容的候選生效）；兩處均無內容時
        /// 返回 null（與基類「目錄不存在」語義一致）。
        /// </summary>
        private string[] EnumerateFiles(string dir, bool recursive)
        {
            if (String.IsNullOrEmpty(dir))
            {
                return null;
            }

            string path = ResolvePath(dir);
            if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.*",
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            List<string> result = new List<string>();
            foreach (string candidate in AssetCandidates(dir))
            {
                CollectAssetFiles(candidate, recursive, result);
                if (result.Count > 0)
                {
                    break;
                }
            }
            return result.Count == 0 ? null : result.ToArray();
        }

        /// <summary>
        /// 深度枚舉 assets 子樹。AssetManager.List 不區分文件與子目錄（對文件返回空數組，
        /// 空目錄亦然）：以「能否再列出內容」區分——可列出者按目錄處理（僅遞歸時下潛，
        /// 非遞歸時跳過），其餘按文件收錄。
        /// </summary>
        private static void CollectAssetFiles(string assetDir, bool recursive, List<string> result)
        {
            foreach (string entry in ListAsset(assetDir))
            {
                if (String.IsNullOrEmpty(entry))
                {
                    continue;
                }

                string child = assetDir + "/" + entry;
                if (ListAsset(child).Count > 0)
                {
                    if (recursive)
                    {
                        CollectAssetFiles(child, recursive, result);
                    }
                }
                else
                {
                    result.Add(child);
                }
            }
        }

        /// <summary>
        /// 深度枚舉 assets 子樹中的目錄。返回反斜線相對形式（如 "MODs\MyMod"）：
        /// 下游沿用桌面書寫習慣（MainMenuScreen 以 LastIndexOf('\\') 取目錄名、
        /// 以 "\" 拼接路徑），與 GetFiles 的「Content/...」資源名形式各隨其消費方。
        /// </summary>
        private static void CollectAssetDirectories(string assetDir, bool recursive, List<string> result)
        {
            foreach (string entry in ListAsset(assetDir))
            {
                if (String.IsNullOrEmpty(entry))
                {
                    continue;
                }

                string child = assetDir + "/" + entry;
                if (ListAsset(child).Count > 0)
                {
                    result.Add(child.Replace('/', '\\'));
                    if (recursive)
                    {
                        CollectAssetDirectories(child, recursive, result);
                    }
                }
            }
        }

        /// <summary>
        /// GetDirectories/GetDirectoriesBasic 共用實現：真實路徑優先，未命中再依序
        /// 枚舉資產候選的直接子目錄（all 為 true 時遞歸）。目錄存在但無子目錄時返回
        /// 空數組、目錄不存在返回 null，與基類語義一致。
        /// </summary>
        private string[] EnumerateDirectories(string dir, bool recursive)
        {
            if (String.IsNullOrEmpty(dir))
            {
                return null;
            }

            string path = ResolvePath(dir);
            if (Directory.Exists(path))
            {
                return Directory.GetDirectories(path, "*",
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            foreach (string candidate in AssetCandidates(dir))
            {
                if (ListAsset(candidate).Count == 0)
                {
                    continue;
                }

                List<string> result = new List<string>();
                CollectAssetDirectories(candidate, recursive, result);
                return result.ToArray();
            }
            return null;
        }

        /// <summary>
        /// 枚舉子目錄（APK assets）。
        /// </summary>
        public override string[] GetDirectories(string dir, bool all, bool full)
        {
            return EnumerateDirectories(dir, all);
        }

        /// <summary>
        /// 枚舉子目錄（基礎位置）：Android 不分「基礎/擴展位置」，與 GetDirectories 同一
        /// 實現（GetDirectoriesExpan 經基類轉發亦走 GetDirectories 覆寫）。
        /// </summary>
        public override string[] GetDirectoriesBasic(string dir, bool all, bool full)
        {
            return EnumerateDirectories(dir, all);
        }

        /// <summary>
        /// 枚舉直接子目錄（真實路徑優先，未命中再枚舉資產候選）。目錄不存在時拋
        /// DirectoryNotFoundException，與基類語義一致：調用方 MainMenuScreen 以
        /// catch (IOException) 兜底，不可改為早退返回 null，否則 AddRange(null) 將拋
        /// ArgumentNullException。
        /// </summary>
        public override string[] GetDirectoryNames(string dir)
        {
            if (String.IsNullOrEmpty(dir))
            {
                throw new DirectoryNotFoundException("Directory not found: " + dir);
            }

            string path = ResolvePath(dir);
            if (Directory.Exists(path))
            {
                return Directory.GetDirectories(path);
            }

            foreach (string candidate in AssetCandidates(dir))
            {
                if (ListAsset(candidate).Count == 0)
                {
                    continue;
                }

                List<string> result = new List<string>();
                CollectAssetDirectories(candidate, false, result);
                return result.ToArray();
            }

            throw new DirectoryNotFoundException("Directory not found: " + dir);
        }

        /// <summary>
        /// 文件存在性判定：真實文件優先（用戶數據、暫存文件），未命中再查資產候選。
        /// 基類僅查 File.Exists，Android 上遊戲資產不是真實文件，此前 PlayEffect
        /// 音效門控恒 false（音效不播）、CacheManager 頭像/寶物路徑選擇全部落空
        /// （寶物恒走不存在的 9999.png 兜底）；此覆寫後與桌面語義對齊。
        /// </summary>
        public override bool FileExists(string file)
        {
            if (String.IsNullOrEmpty(file))
            {
                return false;
            }

            string path = ResolvePath(file);
            CheckFileAccess(path);
            if (File.Exists(path))
            {
                return true;
            }

            return AssetFileExists(file);
        }

        /// <summary>
        /// assets 文件存在性判定：名字命中所在目錄的列表，且不可再列出內容者為文件
        /// （ListAsset 不區分文件與空目錄；APK 不打包空目錄，兩者無需區分）。
        /// </summary>
        private static bool AssetFileExists(string file)
        {
            foreach (string candidate in AssetCandidates(file))
            {
                int slash = candidate.LastIndexOf('/');
                string parent = slash < 0 ? "" : candidate.Substring(0, slash);
                string name = slash < 0 ? candidate : candidate.Substring(slash + 1);

                if (name.Length == 0 || !ListAsset(parent).Contains(name))
                {
                    continue;
                }

                if (ListAsset(candidate).Count == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 目錄存在性判定：真實路徑優先，未命中再查資產候選（MainMenuScreen 的 MODs
        /// 掃描門控、GetPersonVioce 的語音目錄判定依賴此資產感知）。
        /// </summary>
        public override bool DirectoryExists(string dir)
        {
            if (String.IsNullOrEmpty(dir))
            {
                return false;
            }

            if (Directory.Exists(ResolvePath(dir)))
            {
                return true;
            }

            foreach (string candidate in AssetCandidates(dir))
            {
                if (ListAsset(candidate).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 圖片選擇與圖像處理（委托 NativeImages）

        public override void ChoosePicture(PlatformTask action)
        {
            if (MigrationCheck.Enabled)
                throw new InvalidOperationException("External file pickers are disabled in the dedicated check build.");

            // Android 的 PickFiles 返回 content:// URI（Name 非文件路徑），圖像數據必須從
            // Stream 讀取，擴展名來自 MIME 推導的 Ext（如 ".jpeg"），不能照搬桌面端的
            // File.ReadAllBytes(fileName) 模式。
            string extension = null;
            byte[] bytes = null;

            try
            {
                var files = global::Season.Basic.DeviceServices.File
                    .PickFiles(global::Season.Basic.FileType.Image,
                        new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }, false, true)
                    .GetAwaiter().GetResult();

                if (files != null && files.Count > 0)
                {
                    extension = files[0].Ext;
                    using (var stream = files[0].Stream)
                    {
                        if (stream != null)
                        {
                            using (var dest = new MemoryStream())
                            {
                                stream.CopyTo(dest);
                                bytes = dest.ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("选择图片失败", "ChoosePicture", ex);
            }

            //判斷用戶是否正確的選擇了文件
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg";
            }
            extension = extension.ToLower();
            //聲明允許的後綴名
            string[] str = new string[] { ".jpeg", ".jpg", ".png", ".gif", ".bmp" };
            if (!str.Contains(extension))
            {
                _ = global::Season.Basic.DeviceServices.Dialog.ShowMessage("提示", "", ["好的", "取消"], "仅能上传jpg,png,gif,bmp格式的图片！");
                return;
            }

            //判斷文件大小不能超過5000K
            if (bytes.Length > 5000 * 1024)
            {
                _ = global::Season.Basic.DeviceServices.Dialog.ShowMessage("提示", "", ["好的", "取消"], "上传的图片不能大于5000K");
                return;
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
