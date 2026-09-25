using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using GameManager;
using Tools;

namespace Platforms
{

    public enum PlatFormType
    {
        Win,
        Android,
        iOS,
        UWP,
        Desktop  //Mac, Linux
    }

    /// <summary>
    /// 後台任務（線程版）：完成後回調 OnStartFinish，結果經 ParamArray/ParamArrayResult/ParamArrayResultBytes 傳遞。
    /// </summary>
    public class PlatformTask
    {
        Thread thread;
        public bool IsStop = false;
        public string[] ParamArray = null;
        public string[] ParamArrayResult = null;
        public byte[] ParamArrayResultBytes = null;
        public AsyncCallback OnStartFinish = null;
        public PlatformTask(Action action)
        {
            thread = new Thread(() =>
            {
                if (action != null)
                {
                    action.Invoke();
                }
                if (OnStartFinish != null)
                {
                    OnStartFinish.Invoke(null);
                }
            });
        }
        public bool IsAlive
        {
            get
            {
                return thread != null && thread.ThreadState == System.Threading.ThreadState.Running;
            }
        }

        public void Abort()
        {
            IsStop = true;
        }

        public void Start()
        {
            thread.Start();
        }
    }

    public class PlatformTask2
    {
        Thread thread;
        public PlatformTask2(Action action)
        {
            thread = new Thread(() => { action.Invoke(); });
        }
        public void Start()
        {
            thread.Start();
        }
    }

    public abstract class PlatformBase
    {
        public static string Product = "WorldOfTheThreeKingdoms";

        public static PlatFormType PlatFormType;

        /// <summary>
        /// 當前平台的 PlatformBase 實現。由平台入口在啟動時通過 <see cref="Initialize"/> 注入，
        /// 共享代碼只依賴本基類，不再反向引用平台類型（參考引擎 DeviceServices 的注入方式）。
        /// </summary>
        public static PlatformBase Current { get; private set; }

        /// <summary>
        /// 平台入口注入當前平台實現，須早於任何 <see cref="Current"/> 的訪問。
        /// </summary>
        public static void Initialize(PlatformBase current)
        {
            Current = current;
        }

        /// <summary>
        /// 宿主 Game1（引擎宿主層）。幀線程歸屬與繪製狀態由其維護。
        /// </summary>
        public static Zhsan.Game1 Game1 = null;
        //System.IO.File.Exists(GameApplicationUrl))
        //System.Reflection.AssemblyName.GetAssemblyName(GameApplicationUrl).Version.ToString();
        public static string GameVersion = "1.2.8.8";

        public static int PackVersion = 1288;

        public static string GameVersionType = "dev";

        public static string PreferResolution = "925*520";

        public virtual bool? IsTrialCheck()
        {
            return true;
        }

        public static bool IsMobile = true;

        public bool DebugMode = true;
        public bool ProcessGameData = false;

        public bool AssetsPng = false;

        public bool QuickTest = false;

        public string Channel = "";

        public bool DisplayMetroStart = false;
        public bool OpenWeb = true;

        public bool IsOnline = true;

        public string GetSlash = "\\";  // "//";

        public string PlatformPre = @"";

        public bool AdAvaliable = false;

        public string PreferFullMode = "Full";

        public string Location = "";

        //IGraphicsDeviceService service = base.Game.Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
        //this.batch = new SpriteBatch(service.GraphicsDevice);

        /// <summary>
        /// 解決方案文件夾
        /// </summary>
        public string SolutionDir = "";

        /// <summary>
        /// 內存使用占用
        /// </summary>
        public string MemoryUsage
        {
            get
            {
                return (System.GC.GetTotalMemory(false) / 1024).ToString();
                //Android.Activity1.os.Debug.getNativeHeapAllocatedSize()
                //return "";
            }
        }

        /// <summary>
        /// 程序路徑
        /// </summary>
        public string ApplicationUrl = "";
        /// <summary>
        /// 遊戲路徑
        /// </summary>
        public string GameApplicationUrl = "";

        public bool WindowInputCapturerEnable = false;

        public bool IsGuideVisible = false;

        public bool KeyBoardAvailable = true;

        /// <summary>
        /// 是否為移動平台（靜態字段，各平台可覆蓋默認值；唯一存儲）。
        /// </summary>
        public static bool IsMobilePlatForm = false;

        public string CurrentLanguage
        {
            get
            {
                return CultureInfo.CurrentCulture.ToString();
            }
        }

        public static object SerializerLock = new object();

        public static object IoLock = new object();

        public static bool SessionActive { get; set; }

        /// <summary>
        /// 邏輯遊戲實例（原 MonoGame Game 子類已拆分為宿主 Game1 + 邏輯 MainGame 兩層）。
        /// </summary>
        public static Zhsan.MainGame MainGame = null;

        /// <summary>
        /// 是否處於激活（前台）狀態。
        /// </summary>
        public static bool IsActive
        {
            get
            {
                return Game1 != null && Game1.IsActive;
            }
        }

        /// <summary>
        /// 線程休眠（阻塞等待，供加載流程使用）。
        /// </summary>
        public static void Sleep(int time)
        {
            Thread.Sleep(time);
        }

        public PlatformBase()
        {

        }

        public virtual void SetMouseVisible(bool visible)
        {

        }

        public virtual void SetWindowAllowUserResizing(bool allow)
        {

        }

        public virtual void SetTimerDisabled(bool timerDisabled)
        {

        }

        public virtual void PreparePhone()
        {

        }

        //public virtual void SetWindowBorder(bool visible)
        //{

        //}

        public virtual Vector2 GetWorkingArea()
        {
            return Vector2.Zero;
        }

        public virtual void SetFullScreen(bool full)
        {
            //SystemTray.IsVisible = !full;
            //Session.Current.graphics.IsFullScreen = true;
        }

        public virtual void SetFullScreen2(bool full)
        {

        }

        /// <summary>
        /// 設置渲染寬高（默認寫入宿主 Game1 的設計分辨率，平台可覆蓋）。
        /// </summary>
        public virtual void SetGraphicsWidthHeight(int width, int height)
        {
            var game = Game1;
            if (game != null)
            {
                game.DesignResolution = new(width, height);
                game.BasicResolution = game.DesignResolution;
            }
        }

        /// <summary>
        /// 應用渲染尺寸變更（默認請求宿主重排，平台可覆蓋）。
        /// </summary>
        public virtual void GraphicsApplyChanges()
        {
            Game1?.Resize();
        }

        public virtual void SetOrientations()
        {
            //Session.Current.graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        }

        public virtual void EnableFrameCount()
        {

        }

        public virtual string GetDeviceID()
        {
            return "";
        }

        public virtual string GetDeviceInfo()
        {
            return "";
        }

        public virtual string GetSystemInfo()
        {
            return "";
        }

        //public virtual string GetPhoneModel()
        //{
        //    return "";
        //}

        //public virtual string GetPhoneID()
        //{
        //    return "";
        //}

        public virtual float GetUIScale()
        {
            return 1f;
        }

        public virtual void OpenFactory()
        {

        }

        public virtual void PrepareAd()
        {

        }

        public virtual void AdOffersStart()
        {

        }

        public virtual void ShowAd()
        {

        }

        public virtual void ShowOffersWall()
        {

        }

        public virtual void ShowOffersWallDialog()
        {

        }

        public virtual void ShowShareWallDialog()
        {

        }

        public virtual void EndShowAd()
        {

        }

        public virtual void DisplayAdView()
        {

        }

        public virtual void HideAdView()
        {

        }

        public virtual bool InputTextNow()
        {
            return false;
        }

        public virtual void ShowText(string text, AsyncCallback callBack)
        {

        }

        public virtual void HideText(string result)
        {

        }

        public virtual void InitInputCapturer()
        {

        }

        public virtual List<Character> GetChars()
        {
            return null;
        }

        public virtual void ClearChars()
        {

        }

        public virtual void ApplicationViewChanged()
        {

        }

        public virtual void ProcessViewChanged()
        {

        }

        public virtual void SetBarStyle()
        {

        }

        public virtual void ShowKeyBoard()
        {
            //Guide.BeginShowKeyboardInput(PlayerIndex.One, Title, Desc, this.Text, CallbackFunction, null);
        }

        public virtual void ShowKeyBoard(PlayerIndex index, string name, string title, string desc, AsyncCallback callBack)
        {
            //Guide.BeginShowKeyboardInput (PlayerIndex.One, "????????", "????????????????", Session.NickName, CallbackFunction, null);
        }

        public virtual string EndShowKeyBoard(IAsyncResult ar)
        {
            //Guide.EndShowKeyboardInput(ar);
            return "";
        }

        //public byte[] ProcessPictureBytes = null;
        //public SeasonTask ProcessPictureTask = null;

        public virtual void TakePhoto(PlatformTask action)
        {

        }

        public virtual void ChoosePicture(PlatformTask action)
        {

        }

        public virtual void MirrorPicture(byte[] image, PlatformTask action)
        {

        }

        public virtual void RotatePicture(byte[] image, int rotate, PlatformTask action)
        {

        }

        public virtual void CropPicture(byte[] image, int x, int y, int width, int height, PlatformTask action)
        {

        }

        public virtual void ResizeImageFile(byte[] image, int targetSizeWidth, int targetSizeHeight, bool sameRatio, PlatformTask action)
        {

        }

        public virtual void CropResizePicture(byte[] image, int x, int y, int width, int height, int targetSizeWidth, int targetSizeHeight, bool sameRatio, PlatformTask action)
        {

        }

        public virtual void OpenMarket(string key)
        {

        }

        public virtual void OpenReview(string key)
        {

        }

        #region 網絡服務與外部連結（默認實現走系統瀏覽器/WebClient，平台可覆蓋）

        /// <summary>
        /// 調用WebService (post方式)
        /// </summary>
        /// <param name="strURL"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual string GetWebServicePost(string strURL, string data, PlatformTask onUpload)
        {
            Zhsan.GameManager.MigrationCheck.DemandBusinessAccess();
            var client = new WebClient();
            string result = null;
            byte[] sendData = Encoding.GetEncoding("UTF-8").GetBytes(data);

            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            client.Headers.Add("ContentLength", sendData.Length.ToString());

            if (onUpload != null)
            {
                client.UploadProgressChanged += (sender, e) =>
                {
                    onUpload.ParamArray = new string[] { e.ProgressPercentage.ToString() };
                };
            }

            byte[] recData = client.UploadData(strURL, "POST", sendData);

            using (MemoryStream stream = new MemoryStream(recData))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    reader.MoveToContent();
                    result = reader.ReadInnerXml();
                }
            }
            result = result.Replace("&lt;", "<").Replace("&gt;", ">");
            return result;
        }

        /// <summary>
        /// 調用WebService
        /// </summary>
        /// <param name="strURL"></param>
        /// <returns></returns>
        public virtual string GetWebService(string strURL)
        {
            Zhsan.GameManager.MigrationCheck.DemandBusinessAccess();
            if (!OpenWeb)
            {
                return "";
            }
            var client = new WebClient();
            string result = null;

            byte[] response = client.DownloadData(new Uri(strURL));
            using (MemoryStream stream = new MemoryStream(response))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    reader.MoveToContent();
                    result = reader.ReadInnerXml();
                }
            }
            result = result.Replace("&lt;", "<").Replace("&gt;", ">");
            return result;
        }

        /// <summary>
        /// 下載網絡數據（結果經 PlatformTask.ParamArrayResultBytes 回傳）。
        /// </summary>
        public virtual void DownloadWebData(string file, PlatformTask action)
        {
            Zhsan.GameManager.MigrationCheck.DemandBusinessAccess();
            var client = new WebClient();
            byte[] result = client.DownloadData(new Uri(file));
            if (action != null)
            {
                action.ParamArrayResultBytes = result;
                action.Start();
            }
        }

        /// <summary>
        /// 打開外部連結（默認實現走系統默認瀏覽器；遷移檢查模式下被攔截）。
        /// </summary>
        public virtual void OpenLink(string link)
        {
            Zhsan.GameManager.MigrationCheck.DemandBusinessAccess();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(link);
                startInfo.UseShellExecute = true;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("ProcessStartInfo打開連結出錯：", link, ex);
            }
        }

        #endregion

        /// <summary>
        /// 截取應用畫面（JPEG 字節）。由引擎的錄製/圖像服務實現，平台可覆蓋。
        /// </summary>
        public virtual async Task<byte[]> ScreenShotAsync()
        {
            using var image = await global::Season.Basic.DeviceServices.Record.CaptureApp()
                ?? throw new InvalidOperationException("Application capture returned no image.");
            return global::Season.Basic.DeviceServices.Image.SaveImage(image, global::Season.Basic.ImageFormat.Jpeg);
        }

        #region 平台圖像處理與原生文本編輯（由平台實現覆蓋）

        /// <summary>
        /// 將字節解碼為遊戲材質。
        /// </summary>
        public virtual Texture2D LoadImageBytes(byte[] bytes)
        {
            return null;
        }

        /// <summary>
        /// 從文件加載遊戲材質（可帶遮罩形狀）。
        /// </summary>
        public virtual Texture2D LoadImageFile(string path, TextureShape shape)
        {
            return null;
        }

        /// <summary>
        /// 裁剪圖像並返回編碼後的字節。
        /// </summary>
        public virtual byte[] CropImage(byte[] bytes, int x, int y, int width, int height)
        {
            return null;
        }

        /// <summary>
        /// 將引擎圖像編碼為 PNG 字節。
        /// </summary>
        public virtual byte[] EncodePng(global::Season.Basic.INativeImageDecoder image)
        {
            return null;
        }

        /// <summary>
        /// 生成圓形材質。
        /// </summary>
        public virtual Texture2D CreateCircle(int radius, Microsoft.Xna.Framework.Color color)
        {
            return null;
        }

        /// <summary>
        /// 清理平台圖像緩存。
        /// </summary>
        public virtual void CleanupImageCache()
        {
        }

        /// <summary>
        /// 平台圖像緩存目錄（無緩存時為空）。
        /// </summary>
        public virtual string TemporaryImageDirectory => "";

        /// <summary>
        /// 彈出原生文本編輯器，返回編輯結果（取消時為 null）。
        /// </summary>
        public virtual Task<string?> EditText(string title, string description, string text, bool password)
        {
            return global::Season.Basic.DeviceServices.Dialog.ShowKeyboard(title, description, ["OK", "Cancel"], text);
        }

        #endregion

        /// <summary>
        /// 設置音量（走引擎媒體服務，SoundVolume 由引擎統一管理）。
        /// </summary>
        /// <param name="volume"></param>
        public virtual void SetMusicVolume(int volume)
        {
            try
            {
                global::Season.Basic.DeviceServices.Media.SetVolume(volume, (int)Setting.Current.SoundVolume);
            }
            catch (Exception ex)
            {
                //Why?
            }
        }

        /// <summary>
        /// 解析音頻資源路徑：兼容舊調用方的 "Content\\..." 前綴（已有則不重複添加），
        /// 相對路徑基於應用程式目錄解析。
        /// </summary>
        protected static string ResolveAudioPath(string res)
        {
            string rel = res.Replace('\\', '/');
            //if (!rel.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
            //{
            //    rel = "Content/" + rel;
            //}
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rel));
        }

        /// <summary>
        /// 播放歌曲
        /// </summary>
        /// <param name="url"></param>
        public virtual void PlaySong(string res)
        {
            try
            {
                if (String.IsNullOrEmpty(Path.GetExtension(res)))
                {
                    res = res + ".mp3";
                }

                SetMusicVolume((int)Setting.Current.MusicVolume);
                global::Season.Basic.DeviceServices.Media.PlayMedia("Music",
                    ResolveAudioPath(res),
                    ((int)Setting.Current.MusicVolume).ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                //监控此
            }
        }
        List<string> songs2 = new List<string>();
        /// <summary>
        /// 播放歌曲列表：保留原「過濾擴展名 + 隨機選曲」語義；原 MediaStateChanged 自動續播
        /// 由引擎媒體服務的隊列播放承擔。
        /// </summary>
        public virtual void PlaySong(string[] songs)
        {
            try
            {
                songs2 = new List<string>();
                foreach (var item in songs)
                {
                    if ((!item.EndsWith(".mp3") && !item.EndsWith(".wav")))
                    {
                        continue;
                    }
                    songs2.Add(item);
                }
                if (songs2.Count >= 1)
                {
                    SetMusicVolume((int)Setting.Current.MusicVolume);
                    string pick = songs2[new Random().Next(0, songs2.Count)];
                    global::Season.Basic.DeviceServices.Media.PlayMedia("Music",
                        pick, //ResolveAudioPath(pick),
                        ((int)Setting.Current.MusicVolume).ToString(CultureInfo.InvariantCulture));
                }
            }
            catch (Exception ex)
            {
                //监控此
            }
        }
        public virtual void StopSong()
        {
            try
            {
                // 引擎媒體服務沒有 Stop；Pause 近似「停止」語義（後續 PlayMedia 會替換曲目）。
                global::Season.Basic.DeviceServices.Media.Pause();
            }
            catch (Exception ex)
            {

            }
        }
        public virtual void PauseSong()
        {
            try
            {
                global::Season.Basic.DeviceServices.Media.Pause();
            }
            catch (Exception ex)
            {

            }
        }
        public virtual void ResumeSong()
        {
            try
            {
                global::Season.Basic.DeviceServices.Media.Resume();
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="url"></param>
        public virtual bool PlayEffect(string res)
        {
            if (String.IsNullOrEmpty(res))
            {
                return true;
            }
            if (String.IsNullOrEmpty(Path.GetExtension(res)))
            {
                res = res + ".wav";
            }
            try
            {
                string path = ResolveAudioPath(res);
                System.Console.WriteLine("[b4dbg] PlayEffect res=" + res + " path=" + path + " exists=" + FileExists(path));
                // 保留原 LoadFile 失敗（文件不存在）→ false 的語義，供 PlayEffects 繼續嘗試下一個。
                if (!FileExists(path))
                {
                    return false;
                }
                global::Season.Basic.DeviceServices.Media.PlayMedia("Sound", path,
                    ((int)Setting.Current.SoundVolume).ToString(CultureInfo.InvariantCulture));
                System.Console.WriteLine("[b4dbg] PlayMedia sent vol=" + Setting.Current.SoundVolume);
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[b4dbg] PlayEffect EX " + ex.GetType().Name + ": " + ex.Message);
                //监控此
                return false;
            }
        }

        public virtual void PlayEffects(string[] list, string ext)
        {
            if (list == null || list.Length == 0) return;

            foreach (var li in list)
            {
                //if (FileContentExists(li, ext))
                //{
                if (PlayEffect(li))
                {
                    return;
                }
                //}
            }
        }

        public bool FileContentExists(string res, string ext)
        {
            return FileExists("Content/" + res + ext.NullToString(".xnb"));
        }

        public virtual void PauseGame()
        {
            //Session.PauseGame();
        }

        public virtual void ResumeGame()
        {
            //Session.ResumeGame();
        }

        public virtual void Exit()
        {

        }

        /// <summary>
        /// 全局未處理異常兜底（Linux/Apple 入口的 AppDomain.UnhandledException 掛接）：
        /// 記錄到 MainGame.err（調試輸出）並經 WebTools 上報。
        /// </summary>
        public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = args.ExceptionObject as Exception;
            if (e == null) return;
            if (MainGame != null) MainGame.err = e.ToString();
            WebTools.SendErrMsg("RuntimeTerminating: " + args.IsTerminating, e);
        }

        //public virtual void ReStart()
        //{

        //}

        #region 加載資源文件

        /// <summary>
        /// 解析資源路徑：絕對路徑原樣返回；相對路徑（如 "Content/xxx"）基於應用程式目錄解析。
        /// </summary>
        protected static string ResolvePath(string res)
        {
            if (String.IsNullOrEmpty(res)) return res;
            string rel = res.Replace('\\', '/');
            return Path.IsPathRooted(rel) ? rel : Path.Combine(AppContext.BaseDirectory, rel);
        }

        /// <summary>
        /// 資源存取檢查（遷移檢查模式下用於離線目錄切換與訪問審計）。
        /// </summary>
        protected static void CheckFileAccess(string file, bool write = false)
        {
            Zhsan.GameManager.MigrationCheck.CheckFile(file, write);
        }

        /// <summary>
        /// 加載資源文本
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual string LoadText(string res)
        {
            lock (IoLock)
            {
                string path = ResolvePath(res);
                CheckFileAccess(path);
                return File.ReadAllText(path);
            }
        }
        /// <summary>
        /// 加載資源文本
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual string[] LoadTexts(string res)
        {
            lock (IoLock)
            {
                string path = ResolvePath(res);
                CheckFileAccess(path);
                return File.ReadAllLines(path);
            }
        }
        /// <summary>
        /// 加載資源文件
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual byte[] LoadFile(string res)
        {
            string path = ResolvePath(res);
            CheckFileAccess(path);
            lock (IoLock)
            {
                using (var dest = new MemoryStream())
                {
                    using (Stream stream = File.OpenRead(path))
                    {
                        stream.CopyTo(dest);
                        return dest.ToArray();
                    }
                }
            }
        }
        /// <summary>
        /// 加載資源材質（isUser 時從用戶數據目錄讀取）。
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual Texture2D LoadTexture(string res, bool isUser)
        {
            try
            {
                // 無效資源名（空白或僅目錄符號，如 "" / "." / ".."）：直接按失敗返回。
                // 否則 ResolvePath 會拼出 ".../files/." 這類路徑，在 Android 資產層以
                // java.io.FileNotFoundException 形式穿透出來（桌面端則是無意義的 IO 失敗）。
                if (String.IsNullOrWhiteSpace(res) || res.Trim('\\', '/', ' ', '.').Length == 0)
                {
                    return null;
                }

                if (isUser)
                {
                    if (!UserFileExist(new string[] { res })[0])
                    {
                        //暫時沒有文件
                        return null;
                    }

                    byte[] bytes = GetUserFile(res);
                    return bytes == null ? null : LoadImageBytes(bytes);
                }
                return LoadImageFile(ResolvePath(res), TextureShape.None);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载游戏材质失败:" + res, "LoadTexture:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }
        #endregion

        #region 用戶文件夾處理

        public string GetMODFile(string res)
        {
            if (Setting.Current == null) return res;
            
            var mod = Setting.Current.MODRuntime;
            if (Setting.Current != null && !string.IsNullOrEmpty(mod))
            {
                var modRes = res.Replace("Content", $"MODs/{mod}");
                if (Platform.Current.FileExists(modRes))
                    return modRes;
            }

            return res;
        }

        public string[] GetMODFiles(string dir, bool full)
        {
            string[] files = null;

            //根據MOD來選擇素材
            if (String.IsNullOrEmpty(Setting.Current.MODRuntime))
            {
                files = GetFilesBasic(dir, full).NullToEmptyArray();
            }
            else
            {
                var mod = dir.Replace("Content", "MODs\\" + Setting.Current.MODRuntime);

                files = GetFiles(mod, full).NullToEmptyArray();

                if (files.Length == 0)
                {
                    files = GetFilesBasic(dir, full).NullToEmptyArray();
                }
                else
                {
                    
                }
            }

            return files;
        }

        public string[] GetMODDirectories(string dir, bool full)
        {
            string[] dirs = null;

            //根據MOD來選擇文件夾
            if (String.IsNullOrEmpty(Setting.Current.MODRuntime))
            {
                dirs = GetDirectories(dir, false, full).NullToEmptyArray();
            }
            else
            {
                var mod = dir.Replace("Content", "MODs\\" + Setting.Current.MODRuntime);

                dirs = GetDirectories(mod, false, full).NullToEmptyArray();

                if (dirs.Length == 0)
                {
                    dirs = GetDirectoriesBasic(dir, false, full).NullToEmptyArray();
                }
                else
                {

                }
            }

            return dirs;
        }

        public string GetPersonVioce(GameObjects.Person p, string a)
        {
            string ThePersonSound = "";
            if (!Setting.Current.GlobalVariables.TroopVoice && a.Length > 0)
            {
                return ThePersonSound;
            }
            if (DirectoryExists(@"Content/Sound/Animation/Person/" + p.ID.ToString()))
            {
                //string[] files = Directory.GetFiles("Content/Sound/Animation/Person/" + p.ID.ToString(), "CriticalStrike" + "*.wav");
                string[] files = GetMODFiles("Content/Sound/Animation/Person/" + p.ID.ToString() + "/", false);
                if (a.Length > 0)
                {
                    files = files.NullToEmptyArray().Select(en => en.Contains(a) ? en : "").NullToEmptyArray();
                }
                if (files.Count() > 0)
                {
                    //ThePersonSound = "Content/Sound/Animation/Person/" + p.ID.ToString() + "/" + "CriticalStrike" + GameObjects.GameObject.Random(1, files.Count()) + ".wav";
                    ThePersonSound = files[GameObjects.GameObject.Random(0, files.Count() - 1)];
                }
            }
            else if (DirectoryExists(@"Content/Sound/Animation/Person/" + ((int)p.PictureIndex).ToString()))
            {
                string[] files = GetMODFiles("Content/Sound/Animation/Person/" + ((int)p.PictureIndex).ToString() + "/", false);
                if (a.Length > 0)
                {
                    files = files.NullToEmptyArray().Select(en => en.Contains(a) ? en : "").NullToEmptyArray();
                }
                if (files.Count() > 0)
                {
                    ThePersonSound = files[GameObjects.GameObject.Random(0, files.Count() - 1)];
                }
            }
            else if (p.Sex == true && FileExists(@"Content/Sound/Female.wav") && a.Length < 1)
            {
                ThePersonSound = "Content/Sound/Female.wav";
            }
            else
            {
                return p.Sex ? "Content/Sound/Animation/Female/" + a : "Content/Sound/Animation/Male/" + a;
            }
            return ThePersonSound;
        }
        /// <summary>
        /// 用戶數據目錄（以分隔符結尾）。默認為空，桌面平台覆蓋為用戶根目錄。
        /// </summary>
        protected virtual string UserApplicationDataPath => String.Empty;

        /// <summary>
        /// 歸一化用戶文件路徑：反斜線統一為正斜線。
        /// Windows 下兩種分隔符等價；Android/Linux/Apple 上反斜線是字面文件名字符，
        /// 寫入側與讀取側必須歸一化到同一形態，否則存檔會寫入「名字含反斜線」的文件、
        /// 讀取與存在性判定卻按目錄結構查找而讀不到。本家族全部 API（寫/讀/存在性/刪除/流）統一在此歸一化。
        /// </summary>
        private static string NormalizeUserPath(string res)
        {
            if (string.IsNullOrEmpty(res)) return res;
            return res.Replace('\\', '/');
        }

        #region 用戶文件處理（桌面默認實現，移動平台可覆蓋）

        /// <summary>
        /// 判斷用戶文件夾是否存在
        /// </summary>
        public virtual bool UserDirectoryExist(string path)
        {
            try
            {
                path = NormalizeUserPath(path);
                return DirectoryExists(UserApplicationDataPath + path);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("判斷用戶文件夾失敗:" + path, "UserDirectoryExist:" + UserApplicationDataPath + path, ex);
                return false;
            }
        }

        /// <summary>
        /// 創建用戶文件夾
        /// </summary>
        public virtual void UserDirectoryCreate(string path)
        {
            try
            {
                path = NormalizeUserPath(path);
                DirectoryCreateDirectory(UserApplicationDataPath + path);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("創建用戶文件夾失敗:" + path, "UserCreateDirectory:" + UserApplicationDataPath + path, ex);
            }
        }

        /// <summary>
        /// 獲得用戶文件
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public virtual string[] GetUserFileNames(string searchPattern)
        {
            try
            {
                lock (IoLock)
                {
                    var files = Directory.GetFiles(UserApplicationDataPath, searchPattern);
                    if (files != null)
                    {
                        files = files.Select(fi => Path.GetFileName(fi)).ToArray();
                    }
                    return files;
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("获取文件列表失败:" + searchPattern, "GetUserFileNames:" + UserApplicationDataPath, ex);
                return null;
            }
        }

        /// <summary>
        /// 加載用戶文本
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual string GetUserText(string res)
        {
            try
            {
                res = NormalizeUserPath(res);
                lock (IoLock)
                {
                    if (File.Exists(UserApplicationDataPath + res))
                    {
                        return File.ReadAllText(UserApplicationDataPath + res);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载用户文本失败:" + res, "GetUserText:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }

        /// <summary>
        /// 加載用戶文本段
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual string[] GetUserFileString(string res)
        {
            try
            {
                res = NormalizeUserPath(res);
                lock (IoLock)
                {
                    if (File.Exists(UserApplicationDataPath + res))
                    {
                        return File.ReadAllLines(UserApplicationDataPath + res);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载用户文本失败:" + res, "GetUserFileString:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }

        /// <summary>
        /// 加載用戶文件
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual byte[] GetUserFile(string res)
        {
            try
            {
                res = NormalizeUserPath(res);
                lock (IoLock)
                {
                    using (var dest = new MemoryStream())
                    {
                        using (Stream stream = File.Open(UserApplicationDataPath + res, FileMode.Open))
                        {
                            stream.CopyTo(dest);
                            return dest.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载用户文件失败:" + res, "GetUserFile:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }

        /// <summary>
        /// 獲取用戶鍵值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual object GetUserValueByKey(string key)
        {
            try
            {
                string[] strings = GetUserFileString("settings.config");
                if (strings != null)
                {
                    string value = strings.FirstOrDefault(st => st.Contains("=") && st.Split('=')[0] == key);
                    if (!String.IsNullOrEmpty(value))
                    {
                        return value.Split('=')[1];
                    }
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("获取用户键值失败:" + key, "GetUserValueByKey:", ex);
            }
            return null;
        }

        public virtual Stream LoadUserFileStream(string res)
        {
            return LoadUserFileStream(res, false);
        }

        /// <summary>
        /// 加載用戶文件流
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual Stream LoadUserFileStream(string res, bool write)
        {
            res = NormalizeUserPath(res);
            lock (IoLock)
            {
                if (write || File.Exists(UserApplicationDataPath + res))
                {
                    return File.Open(UserApplicationDataPath + res, write ? FileMode.OpenOrCreate : FileMode.Open);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 加載用戶材質
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual Texture2D LoadUserTexture(string res)
        {
            try
            {
                byte[] bytes = GetUserFile(res);
                return bytes == null ? null : LoadImageBytes(bytes);
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("加载用户材质失败:" + res, "LoadUserTexture:" + UserApplicationDataPath + res, ex);
                return null;
            }
        }

        public virtual bool UserFileExist(string res)
        {
            res = NormalizeUserPath(res);
            bool exis = false;
            if (!String.IsNullOrEmpty(res.Trim()))
            {
                try
                {
                    lock (IoLock)
                    {
                        exis = File.Exists(UserApplicationDataPath + res.Trim());
                    }
                }
                catch
                {

                }
            }

            return exis;
        }

        /// <summary>
        /// 判斷用戶文件是否存在
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public virtual bool[] UserFileExist(string[] res)
        {
            if (res == null || res.Length == 0)
            {
                return null;
            }
            try
            {
                List<bool> results = new List<bool>();
                foreach (string re in res)
                {
                    string path = NormalizeUserPath(re);
                    bool exis = false;
                    if (!String.IsNullOrEmpty(path.Trim()))
                    {
                        try
                        {
                            lock (IoLock)
                            {
                                exis = File.Exists(UserApplicationDataPath + path.Trim());
                            }
                        }
                        catch
                        {

                        }
                    }
                    results.Add(exis);
                }
                return results.ToArray();
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("判断文件存在失败:" + String.Join(",", res), "UserFileExist:" + UserApplicationDataPath + String.Join(",", res), ex);
                return null;
            }
        }

        /// <summary>
        /// 保存用戶文本
        /// </summary>
        /// <param name="res"></param>
        /// <param name="content"></param>
        public virtual void SaveUserFile(string res, string content, bool fullPathProvided = false)
        {
            try
            {
                res = NormalizeUserPath(res);
                lock (IoLock)
                {
                    File.WriteAllText(UserApplicationDataPath + res, content);
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("保存用户文本失败:" + res, "SaveUserFile:" + UserApplicationDataPath + res, ex);
            }
        }

        /// <summary>
        /// 保存用戶文件
        /// </summary>
        /// <param name="res"></param>
        /// <param name="bytes"></param>
        public virtual void SaveUserFile(string res, byte[] bytes, PlatformTask action)
        {
            try
            {
                res = NormalizeUserPath(res);
                lock (IoLock)
                {
                    // WriteAllBytes 自動創建/截斷文件（原 OpenWrite 不截斷，短寫會殘留舊字節）。
                    File.WriteAllBytes(UserApplicationDataPath + res, bytes);
                }
                if (action != null)
                {
                    action.Start();
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("保存用户文件失败:" + res, "SaveUserFile:bytes" + bytes.Length + UserApplicationDataPath + res, ex);
            }
        }

        /// <summary>
        /// 刪除用戶文件
        /// </summary>
        /// <param name="files"></param>
        public virtual void DelUserFiles(string[] files, PlatformTask action)
        {
            foreach (string file in files)
            {
                string path = NormalizeUserPath(file);
                var exist = UserFileExist(new string[] { path.NullToString().Trim() });
                if (exist != null && exist[0])
                {
                    try
                    {
                        lock (IoLock)
                        {
                            File.Delete(UserApplicationDataPath + path.Trim());
                        }
                    }
                    catch (Exception ex)
                    {
                        WebTools.TakeWarnMsg("删除用户文件失败:" + file, "File.Delete:" + UserApplicationDataPath + file, ex);
                    }
                }
            }
            if (action != null)
            {
                action.Start();
            }
        }

        #endregion

        /// <summary>
        /// 獲取子目錄（all 為 true 時遞歸；返回絕對路徑）。
        /// </summary>
        public virtual string[] GetDirectories(string dir, bool all, bool full)
        {
            string path = ResolvePath(dir);
            if (!Directory.Exists(path)) return null;
            return Directory.GetDirectories(path, "*", all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// 獲取子目錄（基礎位置，桌面與 GetDirectories 等價）。
        /// </summary>
        public virtual string[] GetDirectoriesBasic(string dir, bool all, bool full)
        {
            return GetDirectories(dir, all, full);
        }

        /// <summary>
        /// 獲取子目錄（擴展位置，桌面與 GetDirectories 等價）。
        /// </summary>
        public virtual string[] GetDirectoriesExpan(string dir, bool all, bool full)
        {
            return GetDirectories(dir, all, full);
        }

        public virtual string[] GetFiles(string dir)
        {
            return GetFiles(dir, true);
        }

        /// <summary>
        /// 獲取文件（all 參數沿用原桌面行為：恒遞歸）。
        /// </summary>
        public virtual string[] GetFiles(string dir, bool all)
        {
            string path = ResolvePath(dir);
            if (!Directory.Exists(path)) return null;
            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        }

        /// <summary>
        /// 獲取文件（基礎位置，桌面與 GetFiles 等價）。
        /// </summary>
        public virtual string[] GetFilesBasic(string dir, bool all = false)
        {
            string path = ResolvePath(dir);
            if (!Directory.Exists(path)) return null;
            return Directory.GetFiles(path, "*.*", all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// 獲取子目錄（返回絕對路徑）。目錄不存在時拋 DirectoryNotFoundException（與原版
        /// PlatformDesktop 語義一致）：調用方 MainMenuScreen 以 catch (IOException) 兜底，
        /// 不可改為早退返回 null，否則 AddRange(null) 將拋 ArgumentNullException。
        /// </summary>
        public virtual string[] GetDirectoryNames(string dir)
        {
            string path = ResolvePath(dir);
            return Directory.GetDirectories(path);
        }

        public virtual string ReadAllText(string file)
        {
            string path = ResolvePath(file);
            CheckFileAccess(path);
            return File.ReadAllText(path);
        }

        public virtual string[] ReadAllLines(string file)
        {
            string path = ResolvePath(file);
            CheckFileAccess(path);
            return File.ReadAllLines(path);
        }

        public virtual byte[] ReadAllBytes(string file)
        {
            string path = ResolvePath(file);
            CheckFileAccess(path);
            return File.ReadAllBytes(path);
        }

        public virtual void WriteAllText(string file, string xml1)
        {
            string path = ResolvePath(file);
            CheckFileAccess(path, write: true);
            File.WriteAllText(path, xml1, Encoding.UTF8);
        }

        public virtual void WriteAllBytes(string file, byte[] bytes1)
        {
            string path = ResolvePath(file);
            CheckFileAccess(path, write: true);
            File.WriteAllBytes(path, bytes1);
        }

        public virtual Stream FileOpenWrite(string file, bool write)
        {
            string path = ResolvePath(file);
            CheckFileAccess(path, write);
            if (write)
            {
                return File.OpenWrite(path) as Stream;
            }
            else
            {
                return File.OpenRead(path) as Stream;
            }
        }

        public virtual bool FileExists(string file)
        {
            string path = ResolvePath(file);
            CheckFileAccess(path);
            return File.Exists(path);
        }

        public virtual void FileDelete(string file)
        {
            string path = ResolvePath(file);
            CheckFileAccess(path, write: true);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public virtual bool DirectoryExists(string dir)
        {
            return Directory.Exists(ResolvePath(dir));
        }

        public virtual void DirectoryCreateDirectory(string dir)
        {
            Directory.CreateDirectory(ResolvePath(dir));
        }

        public virtual string DirectoryName(string dir)
        {
            if (String.IsNullOrEmpty(dir))
            {
                return "";
            }
            else
            {
                return Path.GetDirectoryName(dir);
            }
        }

        public virtual string GetFileNameFromPath(string file)
        {
            return Path.GetFileName(file);
        }

        #endregion

    }

    /// <summary>
    /// character message
    /// </summary>
    public class Character
    {
        /// <summary>
        /// 是否已经使用过
        /// </summary>
        public bool IsUsed { get; set; }
        /// <summary>
        /// char类型
        /// </summary>
        public characterType CharaterType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public char Chars { get; set; }
    }
    public enum characterType
    {
        Char = 0,
        BackSpace = 8,
        Tab = 9,
        Enter = 13,
        Esc = 27
    }


}
