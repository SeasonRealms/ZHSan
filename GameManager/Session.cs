using GameGlobal;
using GameManager;
using GameObjects;
using GameObjects.ArchitectureDetail.EventEffect;
using GameObjects.Conditions;
using GameObjects.Influences;
using Microsoft.Xna.Framework;
// B1+：MonoGame ContentManager 資產層已隨 SeasonXNA 移除，字體載入改走 CacheManager.FontPair 引擎原生路線。
//using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
// B1+：觸摸手勢改由 Season 原生輸入泵（TouchService）承載，舊 XNA TouchPanel 不再存在。
//using Microsoft.Xna.Framework.Input.Touch;
using Platforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tools;
using TroopDetailPlugin;
using WorldOfTheThreeKingdoms;
using WorldOfTheThreeKingdoms.GameScreens;
// B0：MainGame 已遷入 Zhsan 命名空間（宿主 Game1 同層），補 using 供 (MainGame) 轉換解析。
using Zhsan;

namespace GameManager
{

    public enum Difficulty
    {
        beginner,
        easy,
        normal,
        hard,
        veryhard,
        custom
    }

    public class Session
    {
        public static object WorkLock = new object();

        public Session() { }

        public static Session Current = new Session();

        public bool IsWorking = false;

        public static bool LargeContextMenu = false;

        public GameScenario Scenario { get; set; }

        public static Parameters parametersBasic = new Parameters();

        public static Parameters parametersTemp = new Parameters();

        public static Parameters Parameters
        {
            get
            {
                if (Session.Current.Scenario == null || Session.Current.Scenario.Parameters == null)
                {
                    return parametersTemp;
                }
                else
                {
                    return Session.Current.Scenario.Parameters;
                }
            }
        }

        public static GlobalVariables globalVariablesBasic = new GlobalVariables();

        public static GlobalVariables globalVariablesTemp = new GlobalVariables();

        public static GlobalVariables GlobalVariables
        {
            get
            {
                if (Session.Current.Scenario == null || Session.Current.Scenario.GlobalVariables == null)
                {
                    return globalVariablesTemp;
                }
                else
                {
                    return Session.Current.Scenario.GlobalVariables;
                }
            }
        }

        //public Parameters gameParameters = new Parameters();
        //public GlobalVariables globalVariables = new GlobalVariables();

        public static int ResolutionX
        {
            get
            {
                int resolutionX = 0;
                if (!String.IsNullOrEmpty(Resolution) && Resolution.Contains("*"))
                {
                    int.TryParse(Resolution.Split('*')[0].Trim(), out resolutionX);
                }
                return resolutionX;
            }
        }

        public static int ResolutionY
        {
            get
            {
                int resolutionY = 0;
                if (!String.IsNullOrEmpty(Resolution) && Resolution.Contains("*"))
                {
                    int.TryParse(Resolution.Split('*')[1].Trim(), out resolutionY);
                }
                return resolutionY;
            }
        }

        public static string Resolution
        {
            get
            {
                return Setting.Current != null ? Setting.Current.Resolution : "";
            }
            set
            {
                if (!String.IsNullOrEmpty(value) && value.Contains("*"))
                {
                    if (Setting.Current != null)
                    {
                        Setting.Current.Resolution = value;
                    }
                }
            }
        }

        public static MainGame MainGame
        {
            get
            {
                return (MainGame)Platform.MainGame;
            }
        }

        public static string RealResolution = "";

        public static Dictionary<string, TextureRecs> TextureRecs;

        // B1+：原 MonoGame ContentManager 管線字段隨資產層重構移除；資源載入改走引擎原生 API
        //（Platform.Current.LoadTexture/LoadText 等），此處保留註釋僅為對照。
        //public ContentManager Content;
        //public ContentManager FontContent;
        //public ContentManager MusicContent;
        //public ContentManager SoundContent;

        SpriteFont font;


        public SpriteFont Font
        {
            get
            {
                // B0：字體由 TextManager 統一緩存加載（引擎 SpriteFont），
                // 換語言時由 Session.LoadFont 清緩存後按新 FontPair 重建。
                return TextManager.GetFont(CacheManager.FontPair);
            }
            set
            {
                font = value;
            }
        }

        public SpriteBatch SpriteBatch
        {
            get
            {
                return MainGame.SpriteBatch;
            }
        }

        public void Clear()
        {
            if (Scenario != null)
            {
                Scenario.Clear();
                Scenario = null;
            }
        }

        public static void Init()
        {
            #region 手機版采用跟PC同樣設置
            //if (Platform.PlatFormType == PlatFormType.Win)
            //{
            //    //此選項用於生成壓縮格式的劇本，以減小遊戲占用存儲空間
            //    bool BuildScenarioDataZip = false;

            //    if (BuildScenarioDataZip)
            //    {
            //        string comFile = Platform.Current.SolutionDir + @"Content\Data\Common\CommonData.json";
            //        string comFileCon = Platform.Current.ReadAllText(comFile);
            //        var common = Tools.SimpleSerializer.DeserializeJson<CommonData>(comFileCon);

            //        //var str = System.IO.File.ReadAllText(@"C:\Projects\InfluenceKind.xml");
            //        //var doc = new System.Xml.XmlDocument();
            //        //doc.LoadXml(str);

            //        //var childNodes = ((System.Xml.XmlLinkedNode)doc.FirstChild).NextSibling.ChildNodes;

            //        //foreach (System.Xml.XmlElement child in childNodes)
            //        //{
            //        //    var id = child["ID"];
            //        //    var type = child["Type"];
            //        //    var combat = child["Combat"];
            //        //    var pv = child["AIPersonValue"];
            //        //    var pvp = child["AIPersonValuePow"];

            //        //    var influ = common.AllInfluenceKinds.InfluenceKinds.FirstOrDefault(inf => inf.Key == int.Parse(id.InnerText));
            //        //    influ.Value.Type = (InfluenceType)Enum.Parse(typeof(InfluenceType), type.InnerText);
            //        //    influ.Value.Combat = combat.InnerText == "1";
            //        //    influ.Value.AIPersonValue = float.Parse(pv.InnerText);
            //        //    influ.Value.AIPersonValuePow = float.Parse(pvp.InnerText);
            //        //}

            //        string json = SimpleSerializer.SerializeJson<CommonData>(common, true);
            //        string comZipFile = Platform.Current.SolutionDir + @"Content\Data\CommonZip\CommonData.json";
            //        Platform.Current.WriteAllText(comZipFile, json);

            //        string dir = Platform.Current.SolutionDir + @"Content\Data\Scenario\";
            //        var sces = Platform.Current.GetFiles(dir);

            //        foreach (var sce in sces)
            //        {
            //            if (sce.Contains("Scenarios.json"))
            //            {
            //                continue;
            //            }

            //            string fileName = Platform.Current.GetFileNameFromPath(sce);
            //            string sceFileCon = Platform.Current.ReadAllText(dir + fileName);
            //            var scenario = Tools.SimpleSerializer.DeserializeJson<GameScenario>(sceFileCon, false);

            //            json = Tools.SimpleSerializer.SerializeJson<GameScenario>(scenario, true);
            //            string scenarioZipFile = Platform.Current.SolutionDir + @"Content\Data\ScenarioZip\" + fileName;
            //            Platform.Current.WriteAllText(scenarioZipFile, json);
            //        }
            //    }
            //}

            //bool zip = true;

            //if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            //{
            //    zip = false;
            //}
            #endregion

            CommonData.Init();

            //if (String.IsNullOrEmpty(Setting.Current.Difficulty))
            //{
            //    if (String.IsNullOrEmpty(Session.GlobalVariables.GameDifficulty))
            //    {
            //        Setting.Current.Difficulty = Difficulty.beginner.ToString();
            //    }
            //    else
            //    {
            //        Setting.Current.Difficulty = Session.GlobalVariables.GameDifficulty;
            //    }
            //}

            //if (String.IsNullOrEmpty(Setting.Current.BattleSpeed))
            //{
            //    Setting.Current.BattleSpeed = Setting.Current.GlobalVariables.FastBattleSpeed.ToString();
            //}

            Session.LoadFont(Setting.Current.Language);

            // 啟動期字體後台預熱：主菜單文字繪製由 MenuFontWarmup.Ready 門控，
            // 未就緒前僅顯示背景圖（見 MainMenuScreen.Draw）。
            Zhsan.GameManager.MenuFontWarmup.Begin(CacheManager.FontPair);

            // 宿主 Zhsan.Game1（BaseApp）接管圖形設備初始化，Platform.InitGraphicsDeviceManager 已移除。
            //Platform.InitGraphicsDeviceManager();

            // B1+：觸摸手勢開關改由 Season 原生輸入泵承載（TouchPanel 已不存在）。
            //TouchPanel.EnabledGestures = GestureType.Tap | GestureType.DoubleTap | GestureType.FreeDrag | GestureType.Flick | GestureType.Pinch;
        }

        // B1+：原 MonoGame ContentManager 資產入口（宿主 MainGame.LoadContent 調用）已停用；
        // 資產層重構後由引擎原生資源 API 取代，屆時在此重新接入。
        //public static void LoadContent(ContentManager content)
        //{
        //    Current.Content = content;
        //    //Current.Content.RootDirectory = "Content";
        //
        //    Current.FontContent = new ContentManager(Current.Content.ServiceProvider, Current.Content.RootDirectory);
        //    //Current.FontContent.RootDirectory = "Content";
        //
        //    Current.MusicContent = new ContentManager(Current.Content.ServiceProvider, Current.Content.RootDirectory);
        //    //Current.MusicContent.RootDirectory = "Content";
        //
        //    Current.SoundContent = new ContentManager(Current.Content.ServiceProvider, Current.Content.RootDirectory);
        //    //Current.SoundContent.RootDirectory = "Content";
        //
        //    //LoadFont(Setting.Current.Language);
        //}

        public static void LoadFont(string language)
        {
            //Session.Current.FontContent.Unload();

            // B1+：FontStashSharp 整體移除後，換語言改為清空引擎字體緩存（惰性重建）。
            TextManager.ClearFonts();

            //此處兩種字體及大小可隨需要自由更改

            if (language == "cn" || language == "简体")
            {
                CacheManager.FontPair = new FontPair()
                {
                    Name = @"Content\Font\FZLB_GBK.TTF",
                    Size = 30,
                    Style = "",
                    Width = 30,
                    Height = 32
                };
            }
            else
            {
                CacheManager.FontPair = new FontPair()
                {
                    Name = @"Content\Font\JDFGY.TTF",
                    Size = 28,
                    Style = "",
                    Width = 28,
                    Height = 30
                };
            }

            lock (CacheManager.CacheLock)
            {
                if (CacheManager.DicTexts != null)
                {
                    CacheManager.DicTexts.Clear();
                }
            }

        }

        public static void ChangeDisplay(bool setScale)
        {
            Platform.Current.SetFullScreen(Setting.Current.DisplayMode == "Full");

            Platform.Current.SetOrientations();

            float screenscalex1 = 1f;
            float screenscaley1 = 1f;
#pragma warning disable CS0219 // The variable 'screenScale1' is assigned but its value is never used
            float screenScale1 = 1f;
#pragma warning restore CS0219 // The variable 'screenScale1' is assigned but its value is never used

            float screenscalex2 = 1f;
            float screenscaley2 = 1f;
#pragma warning disable CS0219 // The variable 'screenScale2' is assigned but its value is never used
            float screenScale2 = 1f;
#pragma warning restore CS0219 // The variable 'screenScale2' is assigned but its value is never used

            int width = 0;
            int height = 0;

            //Session.Resolution = "925*520";

            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            {
                if (String.IsNullOrEmpty(Session.Resolution))
                {
                    Session.Resolution = Platform.PreferResolution;
                }

                width = int.Parse(Session.Resolution.Split('*')[0]);
                height = int.Parse(Session.Resolution.Split('*')[1]);

                //Session.RealResolution = Session.Resolution = Platform.PreferResolution;

                //InputManager.ScaleDraw = new Vector2(1, 1);

                //Session.MainGame.SpriteScale1 = Matrix.CreateScale(screenscalex1, screenscaley1, 1);

                //InputManager.Scale = new Vector2(screenscalex1, screenscaley1);

            }
            else if (Platform.PlatFormType == PlatFormType.Android || Platform.PlatFormType == PlatFormType.iOS || Platform.PlatFormType == PlatFormType.UWP)
            {
                //Platform.Current.PreparePhone();

                width = Session.MainGame.fullScreenDestination.Width;  // int.Parse(Platform.PreferResolution.Split('*')[0]);
                height = Session.MainGame.fullScreenDestination.Height;  // int.Parse(Platform.PreferResolution.Split('*')[1]);
            }

            float slope = Convert.ToSingle(width) / Convert.ToSingle(height);
            if (Platform.PlatFormType == PlatFormType.Android || Platform.PlatFormType == PlatFormType.iOS || Platform.PlatFormType == PlatFormType.UWP)
            {
                if (slope >= 1.5)
                {
                    Session.Resolution = "1000*620";  //"925*520";
                                                      //LargeContextMenu = true;
                }
                else
                {
                    Session.Resolution = "1024*768";
                }
            }

            //else if (Platform.PlatFormType == PlatFormType.iOS)
            //{
            //    screenscalex1 = Convert.ToSingle(Session.ResolutionX) / 1120f;
            //    screenscaley1 = Convert.ToSingle(Session.ResolutionY) / 630f;
            //    screenScale1 = screenscalex1 >= screenscaley1 ? screenscaley1 : screenscalex1;

            //    Platform.SetGraphicsWidthHeight(Session.ResolutionX, Session.ResolutionY);

            //    InputManager.ScaleDraw = new Vector2(1, 1);

            //    Session.MainGame.disScale = false;
            //}

            Session.RealResolution = Platform.PreferResolution = Session.Resolution;

            screenscalex1 = Convert.ToSingle(width) / 1280f;
            screenscaley1 = Convert.ToSingle(height) / 720f;

            screenscalex2 = Convert.ToSingle(width) / Session.ResolutionX;  // 1120f;
            screenscaley2 = Convert.ToSingle(height) / Session.ResolutionY;  // 630f;

            //screenScale = screenscalex >= screenscaley ? screenscaley : screenscalex;

            InputManager.Scale1 = new Vector2(screenscalex1, screenscaley1);


            //CoreGame.Current.SpriteScale = Matrix.CreateScale(screenScale, screenScale, 1);
            //InputManager.Scale = new Vector2(screenScale, screenScale);

            //if (Platform.PlatFormType == PlatFormType.iOS)
            //{
            //if (slope < 1.5)
            //{
            //        InputManager.Scale1 = new Vector2(Session.ResolutionX / 1280f, Session.ResolutionY / 720f);
            //        InputManager.Scale2 = Vector2.One;
            //}
            //}

            InputManager.ScaleDraw = new Vector2(1, 1);
            if (setScale)
            {
                InputManager.Scale2 = new Vector2(screenscalex2, screenscaley2);
                Session.MainGame.disScale = true;
            }
            else
            {
                Session.MainGame.disScale = false;
            }

            Session.MainGame.SpriteScale1 = Matrix.CreateScale(screenscalex1, screenscaley1, 1);
            Session.MainGame.SpriteScale2 = Matrix.CreateScale(screenscalex2, screenscaley2, 1);

            Platform.Current.SetGraphicsWidthHeight(width, height);

            Platform.Current.ProcessViewChanged();

            Platform.Current.GraphicsApplyChanges();

            InputManager.SWidth = Session.ResolutionX;
            InputManager.SHeight = Session.ResolutionY;
            InputManager.RealScale = new Vector2(Convert.ToSingle(MainGame.fullScreenDestination.Width) / ResolutionX, Convert.ToSingle(MainGame.fullScreenDestination.Height) / ResolutionY);
        }

        public static void ChangeStartDisplay(int width, int height)
        {
            float screenscalex1 = 1f;
            float screenscaley1 = 1f;

            screenscalex1 = Convert.ToSingle(width) / 1280f;
            screenscaley1 = Convert.ToSingle(height) / 720f;

            InputManager.Scale1 = new Vector2(screenscalex1, screenscaley1);

            Session.MainGame.SpriteScale1 = Matrix.CreateScale(screenscalex1, screenscaley1, 1);
        }

        public static void StartScenario(Scenario scenario, bool save)
        {
            var players = scenario.Players.Split(',').RemoveNullOrEmpty().Select(id => int.Parse(id)).NullToEmptyList();

            Session.MainGame.loadingScreen = new LoadingScreen(save  ? "" : "Start", scenario.Name);

            Session.MainGame.loadingScreen.LoadScreenEvent += (sender0, e0) =>
            {
                var mainGameScreen = new MainGameScreen();
                mainGameScreen.InitializationFileName = scenario.Name;

                if (save)
                {
                    mainGameScreen.LoadScenarioInInitialization = false;
                    //CurrentScenario. scenario.ScenarioPath;Session.MainGame.mainGameScreen.InitializationFactionIDs = players;  // scenario.SelectedFactionIDs;
                }
                else
                {
                    mainGameScreen.LoadScenarioInInitialization = true;
                    mainGameScreen.InitializationFactionIDs = players;
                    //scenario.SelectedFactionIDs;@"GameData/Scenario/" + CurrentScenario.Name + ".mdb";  // CurrentScenario. scenario.ScenarioPath;
                }

                mainGameScreen.Initialize();
                Session.MainGame.mainGameScreen = mainGameScreen;

                mainGameScreen.cloudLayer.Start();

                Session.Current.Scenario.AfterInit();
            };
        }

        public static void PlayMusic(string category)
        {
            string[] songs = null;
            songs = Platform.Current.GetMODFiles(@"Content\Music\" + category, true).NullToEmptyArray();

            Platform.Current.PlaySong(songs);
            //if (songs.Length > 0)
            //{
            //    Random rd = new Random();
            //    int index = rd.Next(0, songs.Length);
            //    string song = songs[index];
            //    Platform.Current.PlaySong(song);
            //}

        }

        public static void StopSong()
        {
            Platform.Current.StopSong();
        }

    }
}

//性能优化記錄
//Stopwatch stopwatch = new Stopwatch();
//stopwatch.Start();

//var conditionKinds = new ConditionKindTable();
//foreach (var conditionKind in CommonData.Current.AllConditionKinds.ConditionKinds)
//{
//    var con = new ConditionKind();
//    con.ID = conditionKind.Value.ID;
//    con.Name = conditionKind.Value.Name;
//    conditionKinds.AddConditionKind(con);
//}
//CommonData.Current.AllConditionKinds = conditionKinds;

//foreach (var condition in CommonData.Current.AllConditions.Conditions)
//{
//    var Kind = condition.Value.Kind;
//    var con = new ConditionKind();
//    con.ID = Kind.ID;
//    con.Name = Kind.Name;
//    condition.Value.Kind = con;
//}

//var influenceKinds = new InfluenceKindTable();
//foreach (var influenceKind in CommonData.Current.AllInfluenceKinds.InfluenceKinds)
//{
//    var inf = new InfluenceKind();
//    inf.ID = influenceKind.Value.ID;
//    inf.Name = influenceKind.Value.Name;
//    influenceKinds.AddInfluenceKind(inf);
//}
//CommonData.Current.AllInfluenceKinds = influenceKinds;

//foreach (var influence in CommonData.Current.AllInfluences.Influences)
//{
//    var kind = influence.Value.Kind;
//    var inf = new InfluenceKind();
//    inf.ID = kind.ID;
//    inf.Name = kind.Name;
//    influence.Value.Kind = inf;
//}

//var eventEffectKinds = new EventEffectKindTable();
//foreach (var eventEffectKind in CommonData.Current.AllEventEffectKinds.EventEffectKinds)
//{
//    var eve = new EventEffectKind();
//    eve.ID = eventEffectKind.Value.ID;
//    eve.Name = eventEffectKind.Value.Name;
//    eventEffectKinds.AddEventEffectKind(eve);
//}
//CommonData.Current.AllEventEffectKinds = eventEffectKinds;

//foreach (var eventEffect in CommonData.Current.AllEventEffects.EventEffects)
//{
//    var kind = eventEffect.Value.Kind;
//    var eve = new EventEffectKind();
//    eve.ID = kind.ID;
//    eve.Name = kind.Name;
//    eventEffect.Value.Kind = eve;
//}

//var troopEventEffectKinds = new GameObjects.TroopDetail.EventEffect.EventEffectKindTable();
//foreach (var eventEffectKind in CommonData.Current.AllTroopEventEffectKinds.EventEffectKinds)
//{
//    var eve = new GameObjects.TroopDetail.EventEffect.EventEffectKind();
//    eve.ID = eventEffectKind.Value.ID;
//    eve.Name = eventEffectKind.Value.Name;
//    troopEventEffectKinds.AddEventEffectKind(eve);
//}
//CommonData.Current.AllTroopEventEffectKinds = troopEventEffectKinds;

//foreach (var eventEffect in CommonData.Current.AllTroopEventEffects.EventEffects)
//{
//    var kind = eventEffect.Value.Kind;
//    var eve = new GameObjects.TroopDetail.EventEffect.EventEffectKind();
//    eve.ID = kind.ID;
//    eve.Name = kind.Name;
//    eventEffect.Value.Kind = eve;
//}

//CommonData.Current.ConditionKindList = CommonData.Current.AllConditionKinds.ConditionKinds.Select(co => co.Value).ToList();
//CommonData.Current.AllConditionKinds = null;
//CommonData.Current.ConditionList = CommonData.Current.AllConditions.Conditions.Select(co => co.Value).ToList();
//CommonData.Current.AllConditions = null;

//CommonData.Current.InfluenceKindList = CommonData.Current.AllInfluenceKinds.InfluenceKinds.Select(inf => inf.Value).ToList();
//CommonData.Current.AllInfluenceKinds = null;

//CommonData.Current.InfluenceList = CommonData.Current.AllInfluences.Influences.Select(inf => inf.Value).ToList();
//CommonData.Current.AllInfluences = null;

//string str = Tools.SimpleSerializer.SerializeJson<CommonData>(CommonData.Current);

//stopwatch.Stop();
