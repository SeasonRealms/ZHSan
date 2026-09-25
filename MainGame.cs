using GameGlobal;
using GameManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using PluginServices;
using SeasonXNA.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tools;
using WorldOfTheThreeKingdoms.GameScreens;

namespace Zhsan
{
    /// <summary>
    /// 邏輯遊戲類（原 MonoGame Game 子類拆分後的邏輯層）：持有主菜單/加載/遊戲畫面鏈，
    /// 由宿主 <see cref="Game1"/> 驅動（Init/LoadContent/Update/Draw）。
    /// </summary>
    public class MainGame
    {
        //public static  ContentManager Content;   //原程序，没有new
        //public static new ContentManager Content;

        //public System.Windows.Forms.Form GameForm;

        //private GraphicsDeviceManager graphics;
        //private KeyboardState keyState;  //原程序，由于警告去掉

        public MainMenuScreen mainMenuScreen;

        public LoadingScreen loadingScreen;

        public MainGameScreen mainGameScreen;

        public float time = 0f;

        public bool beginApply = false;

#pragma warning disable CS0414 // The field 'MainGame.previousWindowHeight' is assigned but its value is never used
        private int previousWindowHeight = 720;
#pragma warning restore CS0414 // The field 'MainGame.previousWindowHeight' is assigned but its value is never used
#pragma warning disable CS0414 // The field 'MainGame.previousWindowWidth' is assigned but its value is never used
        private int previousWindowWidth = 0x438;
#pragma warning restore CS0414 // The field 'MainGame.previousWindowWidth' is assigned but its value is never used

        //public jiazaitishichuangkou jiazaitishi = new jiazaitishichuangkou();

        //public WindowsMediaPlayerClass Player = new WindowsMediaPlayerClass();

        //标识是否为全屏
#pragma warning disable CS0414 // The field 'MainGame.IsFullScreen' is assigned but its value is never used
        private bool IsFullScreen = false;
#pragma warning restore CS0414 // The field 'MainGame.IsFullScreen' is assigned but its value is never used

        public Matrix SpriteScale1, SpriteScale2;

        public bool disScale = false;

        public Rectangle fullScreenDestination;

        public SpriteBatch SpriteBatch;

        public bool? takePicture = null;

        // 截圖走引擎錄製/圖像服務（異步），不再使用 RenderTarget 讀回。
        private System.Threading.Tasks.Task _captureTask = System.Threading.Tasks.Task.CompletedTask;

        public string picture = "";

        public Vector2 errPos = Vector2.Zero;
        public string err = "";
        public string warn = "";
        public DateTime? lastWarnTime = null;
        public string view = "";
        
        public Texture2D renderLast = null;

        public bool isDebug = false;
        public bool loaded2 = false;

        /// <summary>當前邏輯幀時間（供屏幕 Update/Draw 使用）。</summary>
        private readonly GameTime frameTime = new GameTime();

        /// <summary>是否處於繪製區間（宿主維護，供資源線程親和斷言使用）。</summary>
        public bool IsDrawing { get; internal set; }

        public MainGame()
        {
            //第一步
          
            Platform.MainGame = this;

            //if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            //{
            //    this.Window.IsBorderless = true;
            //}

            Platform.Current.PreparePhone();

            //獲取設置數據
            Setting.Init(false);

            Session.globalVariablesBasic = new GlobalVariables();
            Session.globalVariablesBasic.InitialGlobalVariables();

            Session.parametersBasic = new Parameters();
            Session.parametersBasic.InitializeGameParameters();

            //獲取設置數據
            Setting.Init(true);

            Session.Init();

            //加速：原 TargetElapsedTime/IsFixedTimeStep（60Hz 邏輯幀）由宿主定時器（FixedStepClock）承接；
            //SpeedUp 的加速語義在 B1+ 隨宿主時鐘擴展接入。

            //原本的分辨率默认值为720*1080，在其他大小的屏幕上会变形
            //将当前屏幕的分辨率作为默认值……
            //this.previousWindowWidth = WinHelper.GetSystemMetrics(WinHelper.SM_CXSCREEN);
            //this.previousWindowHeight = WinHelper.GetSystemMetrics(WinHelper.SM_CYSCREEN);
            //Platform.SetGraphicsWidthHeight(this.previousWindowWidth, this.previousWindowHeight);
            //this.graphics.PreferredBackBufferWidth = this.previousWindowWidth;
            //this.graphics.PreferredBackBufferHeight = this.previousWindowHeight;                      

            if (Platform.PlatFormType == PlatFormType.Win)  //Platform.PlatFormType == PlatFormType.UWP
            {
                DateTime buildDate = new FileInfo(Platform.Current.Location).LastWriteTime;
                var host = Platform.Game1;
                if (host != null)
                {
                    host.Title = "中华三国志(v1.25.1) - build-" + buildDate.Year + "-" + buildDate.Month + "-" + buildDate.Day;
                }
            }

            Platform.Current.SetMouseVisible(false);
            Platform.Current.SetBarStyle();
            Platform.Current.SetTimerDisabled(true);
            Platform.Current.ApplicationViewChanged();

            //System.Windows.Forms.Control control = System.Windows.Forms.Control.FromHandle(base.Window.Handle);
            //this.GameForm = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(this.Window.Handle);
            //this.GameForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;

            //this.GameForm = control as System.Windows.Forms.Form;
            //this.GameForm.KeyDown += new KeyEventHandler(this.GameForm_KeyDown);

            //int uFlags = 0x400;
            //IntPtr systemMenu = GetSystemMenu(base.Window.Handle, false);
            //int menuItemCount = GetMenuItemCount(systemMenu);
            //RemoveMenu(systemMenu, menuItemCount - 1, uFlags);
            //RemoveMenu(systemMenu, menuItemCount - 2, uFlags);

            //Plugin.Plugins.FindPlugins(AppDomain.CurrentDomain.BaseDirectory + "GameComponents");
            //Plugin.Plugins.FindPlugins(AppDomain.CurrentDomain.BaseDirectory + "GamePlugins");
            //this.mainGameScreen = new MainGameScreen(this);
            //base.Components.Add(this.mainGameScreen);
        }

        //private static bool AltComboPressed(KeyboardState state, Microsoft.Xna.Framework.Input.Keys key)
        //{
        //    return (state.IsKeyDown(key) && (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) || state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt)));
        //}

        //private void GameForm_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Alt && (e.KeyCode == System.Windows.Forms.Keys.F4))
        //    {
        //        e.Handled = true;
        //    }
        //}

        public void Init()
        {
            //第二步
            //if (Platform.PlatFormType != PlatFormType.UWP)
            //{
                Session.ChangeDisplay(true);
            //}

            //基本材質初始化
            Session.TextureRecs = TextureRecsManager.AllTextureRectangles();

            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            {
                Platform.Current.SetWindowAllowUserResizing(true);
                
            }

            //try
            //{
                this.mainMenuScreen = new MainMenuScreen();
            //}
            //catch (Exception ex)
            //{
            //    GameTools.SendErrMsg("MainMenuScreen", ex);
            //}

            //this.jiazaitishi.Close();
            ////全屏的判断放到初始化代码中
            //if (Session.GlobalVariables.FullScreen)
            //{
            //    this.ToggleFullScreen();
            //}

            // 原 Window.ClientSizeChanged 事件註冊已隨 MonoGame Game 基類移除；
            // 窗口尺寸變化時由宿主/平台調用 Window_ClientSizeChanged。
        }

        /// <summary>
        /// 窗口尺寸變化處理（由宿主/平台在尺寸變化時調用）。
        /// </summary>
        public void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (mainGameScreen == null)
            {
                var host = Platform.Game1;
                int width = host != null ? (int)host.DesignResolution.X : 0;
                int height = host != null ? (int)host.DesignResolution.Y : 0;
                Session.ChangeStartDisplay(width, height);                
            }
            else
            {
                mainGameScreen.Window_ClientSizeChanged(sender, e);
            }
        }

        /// <summary>
        /// 加載內容（第三步）：由宿主在啟動時調用。
        /// </summary>
        public void LoadContent(DrawContext context)
        {
            //第三步

            SpriteBatch = new SpriteBatch(context);

            // B0：預熱共享純白貼圖（滾動條/純色塊的 tint 底圖）。
            // 紋理建立要求在非繪製期的幀線程上執行，此處為加載期，符合約束。
            _ = Zhsan.GameManager.SolidTextures.White;

            // B1+：Session.LoadContent（原 MonoGame ContentManager 資產層）將隨資源層重構接入。
            //Session.LoadContent(base.Content);

            Session.PlayMusic("Start");
            
        }

        public void ToggleFullScreen()
        {
            //bool full = Setting.Current.DisplayMode == "Full";
            //if (full)
            //{
            //    Setting.Current.DisplayMode = "Window";
            //    Platform.Current.SetFullScreen(false);
            //}
            //else
            //{
            //    Setting.Current.DisplayMode = "Full";
            //    Platform.Current.SetFullScreen(true);
            //}

            //Setting.Save();

            //Platform.GraphicsApplyChanges();

            //原来的代码会和单挑程序冲突
            /*
            if (this.graphics.GraphicsDevice.PresentationParameters.IsFullScreen)
            {
                this.graphics.PreferredBackBufferWidth = this.previousWindowWidth;
                this.graphics.PreferredBackBufferHeight = this.previousWindowHeight;
            }
            else
            {
                this.previousWindowWidth = this.graphics.GraphicsDevice.Viewport.Width;
                this.previousWindowHeight = this.graphics.GraphicsDevice.Viewport.Height;
                GraphicsAdapter adapter = this.graphics.GraphicsDevice.CreationParameters.Adapter;
                FullScreenHelper.FullScreen();
                this.graphics.PreferredBackBufferWidth = adapter.CurrentDisplayMode.Width;
                this.graphics.PreferredBackBufferHeight = adapter.CurrentDisplayMode.Height;
            }
            this.graphics.ToggleFullScreen();
            Session.GlobalVariables.FullScreen = this.graphics.GraphicsDevice.PresentationParameters.IsFullScreen;
             */

            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            {
                Platform.Current.SetFullScreen2(this.IsFullScreen);                

                this.IsFullScreen = !this.IsFullScreen;
            }

            ////修改后的全屏代码
            //if (this.IsFullScreen)
            //{
            //    WinHelper.RestoreFullScreen(this.GameForm.Handle);//传入窗体句柄
            //    this.IsFullScreen = false;

            //}
            //else
            //{
            //    WinHelper.FullScreen(this.GameForm.Handle);
            //    this.IsFullScreen = true;
            //}
        }

        private void TryToExit()
        {
            this.mainGameScreen.TryToExit();
        }

        /// <summary>
        /// 邏輯更新（第四步）：由宿主按固定 60Hz 步長驅動（ti 為固定步長秒數）。
        /// </summary>
        public void Update(float ti)
        {
            //第四步

            time += ti;

            //if (time >= 0.5f && !beginApply)
            //{
            //    beginApply = true;
            //    if (Platform.PlatFormType == PlatFormType.UWP)
            //    {
            //        Session.ChangeDisplay(true);
            //        Platform.GraphicsApplyChanges();
            //    }
            //}

            frameTime.TotalGameTime = TimeSpan.FromSeconds(time);
            frameTime.ElapsedGameTime = TimeSpan.FromSeconds(ti);

            if (Platform.IsActive || Setting.Current.GlobalVariables.RunWhileNotFocused)
            {
                if (Platform.Current.InputTextNow())
                {
                    return;
                }

                InputManager.Update(ti);

                if (loadingScreen == null)
                {
                    if (mainGameScreen == null)
                    {
                        if (mainMenuScreen == null)
                        {

                        }
                        else
                        {
                            mainMenuScreen.Update(frameTime);
                        }
                    }
                    else
                    {
                        if (isDebug)
                        {
                            mainGameScreen.Update(frameTime);
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(err))
                            {
#if DEBUG
                                mainGameScreen.Update(frameTime);
#else
                                try
                                {
                                    mainGameScreen.Update(frameTime);
                                }
                                catch (Exception ex)
                                {
                                    err = "不好意思，游戏运行出错，点击将返回主菜单，请考虑读取自动存档。\r\n" + ex.Message;
                                    WebTools.TakeWarnMsg("mainGameScreen.Update", "", ex);
                                }
#endif
                            }
                            else
                            {
                                if (InputManager.IsPressed)
                                {
                                    err = "";

                                    //保存當前進度
                                    mainGameScreen.SaveGameAutoPosition();

                                    loadingScreen = new LoadingScreen("End", "");
                                    loadingScreen.LoadScreenEvent += (sender0, e0) =>
                                    {
                                        Platform.Sleep(1000);
                                    };
                                }
                            }
                        }
                    }
                }
                else
                {
                    loadingScreen.Update(frameTime);
                }

                //if (AltComboPressed(this.mainGameScreen.KeyState, Microsoft.Xna.Framework.Input.Keys.F4))
                //{
                //    this.TryToExit();
                //}
                //if (AltComboPressed(this.mainGameScreen.KeyState, Microsoft.Xna.Framework.Input.Keys.Enter) && (this.mainGameScreen.PeekUndoneWork().Kind == UndoneWorkKind.None))
                //{
                //    this.mainGameScreen.ToggleFullScreen();
                //}
                //游戏设置中的全屏选项勾选后将多次调用这个方法，界面会闪来闪去
                //只在初始化的时候全屏一次就好啦……
                /*if ((Session.GlobalVariables.FullScreen && !this.mainGameScreen.IsFullScreen) || (!Session.GlobalVariables.FullScreen && this.mainGameScreen.IsFullScreen))
                {
                    this.mainGameScreen.ToggleFullScreen();
                }*/
            }
        }

        /// <summary>
        /// 邏輯繪製（第五步）：由宿主在 Draw2D 繪製區間內調用。
        /// </summary>
        public void Draw()
        {
            //第五步

            // 截圖由引擎錄製服務承接（見 Draw 尾部 CapturePictureAsync），原 RenderTarget 讀回邏輯已移除。

            //var spriteMode = mainGameScreen == null ? SpriteSortMode.Deferred : SpriteSortMode.BackToFront;

            // SeasonXNA：引擎僅支持 Deferred 提交序（BackToFront/Immediate 等一律被拒，見 DrawContracts.ValidateSortMode），
            // 遊戲內繪製原依賴 layerDepth 排序，移植後改由「按提交序繪製」承接（與 SanguoSeason 移植做法一致）。
            var spriteMode = SpriteSortMode.Deferred;


            if (disScale) //Platform.PlatFormType == PlatForm.iOS && isRetina)
            {
                if (mainGameScreen == null || loadingScreen != null)
                {
                    SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend, null, SpriteScale1);
                }
                else
                {
                    SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend, null, SpriteScale2);
                }
            }
            else
            {
                if (mainGameScreen == null || loadingScreen != null)
                {
                    SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend, null, SpriteScale1);
                }
                else
                {
                    //SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend, null, SpriteScale2);
                    SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend);
                }


            }

            // 清屏由引擎宿主承接（Game1.BackgroundColor）。
            //Platform.GraphicsDevice.Clear(Color.Transparent);

            if (loadingScreen == null)
            {
                if (mainGameScreen == null)
                {
                    if (mainMenuScreen == null)
                    {

                    }
                    else
                    {
                        mainMenuScreen.Draw(frameTime);
                    }
                }
                else
                {
                    if (isDebug)
                    {
                        mainGameScreen.Draw(frameTime);
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(err))
                        {
#if DEBUG
                            mainGameScreen.Draw(frameTime);
#else
                                try
                                {
                                    mainGameScreen.Draw(frameTime);
                                }
                                catch (Exception ex)
                                {
                                    err = "不好意思，游戏运行出错，点击将返回主菜单，请考虑读取自动存档。\r\n" + ex.Message;
                                    WebTools.TakeWarnMsg("mainGameScreen.Draw", "", ex);
                                }
#endif

                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(err))
                            {
                                CacheManager.DrawString(Session.Current.Font, err.SplitLineString(100), errPos, Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                            }

                            if (InputManager.IsPressed)
                            {
                                err = "";

                                //保存當前進度
                                mainGameScreen.SaveGameAutoPosition();

                                loadingScreen = new LoadingScreen("End", "");
                                loadingScreen.LoadScreenEvent += (sender0, e0) =>
                                {
                                    Platform.Sleep(1000);
                                };
                            }
                        }
                    }
                }
            }
            else
            {
                loadingScreen.Draw(frameTime);
            }

            //view = Platform.Current.MemoryUsage;
            //if (!String.IsNullOrEmpty(view))
            //{
            //    CacheManager.DrawString(Session.Current.Font, "view:" + view.SplitLineString(100), errPos, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            //}

            //if (!String.IsNullOrEmpty(warn) && lastWarnTime != null && (DateTime.Now - (DateTime)lastWarnTime).TotalSeconds < 20)
            //{
            //    CacheManager.DrawString(Session.Current.Font, "Warn:" + warn, errPos, Color.Red, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 1f);
            //}

            ////if (!String.IsNullOrEmpty(err))
            ////{
            ////CacheManager.DrawString(LightAncient, "Err:" + err.SplitLineString(100).ProcessStar(), errPos, Color.Red, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 1f);
            ////}

            if (renderLast != null)
            {
                SpriteBatch.Draw(renderLast, Vector2.Zero, Color.White);
            }

            SpriteBatch.End();

            if (takePicture == true && String.IsNullOrEmpty(err) && _captureTask.IsCompleted)
            {
                takePicture = false;
                _captureTask = CapturePictureAsync(picture);
            }
        }

        /// <summary>
        /// 截取應用畫面（引擎錄製服務）並縮放為 800*480 保存為用戶文件；異步執行避免阻塞幀。
        /// </summary>
        private async System.Threading.Tasks.Task CapturePictureAsync(string destination)
        {
            try
            {
                byte[] shot = await Platform.Current.ScreenShotAsync();
                if (shot != null && shot.Length > 0)
                {
                    var task = new PlatformTask(() => { });
                    task.OnStartFinish += (result) =>
                    {
                        if (task.ParamArrayResultBytes != null && task.ParamArrayResultBytes.Length > 0)
                        {
                            Platform.Current.SaveUserFile(destination, task.ParamArrayResultBytes, null);
                        }
                    };
                    Platform.Current.ResizeImageFile(shot, 800, 480, false, task);
                }
            }
            catch (Exception ex)
            {
                WebTools.TakeWarnMsg("游戏界面截屏失败:", "CaptureApp", ex);
            }
        }

        public void SaveGameWhenCrash(String _savePath)
        {
            this.mainGameScreen.SaveGameWhenCrash(_savePath);
        }

        public List<int> InitializationFactionIDs
        {
            set
            {
                this.mainGameScreen.InitializationFactionIDs = value;
            }
        }

        public string InitializationFileName
        {
            set
            {
                this.mainGameScreen.InitializationFileName = value;
            }
        }

        public bool LoadScenarioInInitialization
        {
            set
            {
                this.mainGameScreen.LoadScenarioInInitialization = value;
            }
        }
        
        //public void PlayMusic()
        //{
            //Player.currentPlaylist.clear();
            //WMPLib.IWMPMedia media;

            //string[] filePaths = Directory.GetFiles("GameMusic/Start/", "*.mp3");
            //Random rd = new Random();
            //int index = rd.Next(0, filePaths.Length);
            //string path = filePaths[index];

            //foreach (String s in filePaths)
            //{
            //    media = Player.newMedia(s);
            //    Player.currentPlaylist.appendItem(media);
            //}
            //media = Player.newMedia(path);
            //Player.currentPlaylist.appendItem(media);
            //Player.currentItem = media;
            //Player.play();
            //Player.settings.setMode("loop", true);
        //}

        /// <summary>
        /// 幀線程資源預熱（宿主每幀 Update 階段調用）。ZHSan 的材質緩存按需加載，
        /// 暫無需預熱；保留掛載點供 B1+ 資源層重構使用。
        /// </summary>
        public void PrepareCommonResources()
        {
        }

        /// <summary>
        /// 邏輯層釋放（第六步）：由宿主在關閉時調用。
        /// </summary>
        public void Dispose()
        {
            Plugin.Plugins.ClosePlugins();
        }

        //[DllImport("user32.dll")]
        //public static extern int GetMenuItemCount(IntPtr hMenu);
        //[DllImport("user32.dll")]
        //public static extern IntPtr GetSystemMenu(IntPtr hwnd, bool bRevert);
        //[DllImport("user32.dll")]
        //public static extern int RemoveMenu(IntPtr hMenu, int uPosition, int uFlags);

        //public void Processing()
        //{
        //    //formMainMenu menu = new formMainMenu
        //    //{
        //    //    mainGame = this
        //    //};

        //    if (menu.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //    {
        //        mainGame.jiazaitishi.Show();
        //        mainGame.jiazaitishi.Refresh();
        //        mainGame.Run();
        //    }
        //}

    }
}
