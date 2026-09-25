using Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Platforms;
using GameGlobal;

namespace GameManager
{
    [DataContract]
    public class Setting
    {
        [DataMember]
        public string UserGuid { get; set; }
        [DataMember]
        public string DeviceID { get; set; }
        [DataMember]
        public string Language { get; set; }
        [DataMember]
        public int? MusicVolume { get; set; }
        [DataMember]
        public int? SoundVolume { get; set; }
        [DataMember]
        public string DisplayMode { get; set; }
        [DataMember]
        public string Resolution { get; set; }
        [DataMember]
        public string GamerName { get; set; }
        //[DataMember]
        //public string Difficulty { get; set; }//GlobalVariables已有GameDifficulty
        //[DataMember]
        //public string BattleSpeed { get; set; }//GlobalVariables已有FastBattleSpeed
        [DataMember]
        public int? SpeedUp { get; set; }
        [DataMember]
        public bool Chuchangsuiji { get; set; }

        [DataMember]
        public string MOD { get; set; }

        /// <summary>
        /// 头像包
        /// </summary>
        [DataMember]
        public string PortraitPack { get; set; }

        public string MODRuntime
        {
            get
            {
                if (Session.Current == null || Session.Current.Scenario == null || Session.Current.Scenario.MOD == null)
                {
                    return Setting.Current.MOD;
                }
                else
                {
                    return Session.Current.Scenario.MOD;
                }
            }
        }

        [DataMember]
        public GlobalVariables GlobalVariables { get; set; }

        public static Setting Current = null;

        public Setting()
        {

        }

        public static void Init(bool prepare)
        {
            string file = "Setting.config";
            if (Platform.Current.UserFileExist(file))
            {
                try
                {
                    Current = SimpleSerializer.DeserializeJsonFile<Setting>(file, true, false) ?? new Setting();

                    if (prepare)
                    {
                        Prepare();
                    }

                    Save();
                }
                catch (Exception ex)
                {
                    WebTools.TakeWarnMsg("初始用户设置失败:Setting.config", "Init:", ex);
                }
             }

            if (Current == null)
            {
                Current = new Setting();

                if (prepare)
                {
                    Prepare();
                }

                Save();
            }
            else
            {
                Current = new Setting();
                
                if (prepare)
                {
                    Prepare();
                }

                Save();
            }

            //if (Platform.PlatFormType == PlatFormType.iOS)  //|| Platform.PlatForm == PlatForm.WinRT || Platform.PlatForm == PlatForm.WP)
            //{
            //	Session.RealResolution = Session.Resolution = Platform.PreferResolution;
            //}
        }

        public static void Save()
        {
            string file1 = "Setting.config";
            SimpleSerializer.SerializeJsonFile<Setting>(Setting.Current, file1, false);
        }

        static void Prepare()
        {
            if (Current != null)
            {
                if (String.IsNullOrEmpty(Current.UserGuid))
                {
                    Current.UserGuid = Guid.NewGuid().ToString();
                }

                if (String.IsNullOrEmpty(Current.DeviceID))
                {
                    Current.DeviceID = Platform.Current.GetDeviceID();
                }

                if (String.IsNullOrEmpty(Current.DisplayMode))
                {
                    Current.DisplayMode = Platform.Current.PreferFullMode;
                }
                if (Current.MusicVolume == null)
                {
                    Current.MusicVolume = 70;
                }
                if (Current.SoundVolume == null)
                {
                    Current.SoundVolume = 50;
                }
                if (Current.SpeedUp == null)
                {
                    Current.SpeedUp = 6;
                }
                if (String.IsNullOrEmpty(Current.Chuchangsuiji.ToString()))
                {
                    Current.Chuchangsuiji = false;
                }
    
                //if (Current.NewsBoard == null)
                //{
                //    Current.NewsBoard = new NewsBoard() { Detail = "游戏公告加载中，请稍候……" };
                //}

                if (String.IsNullOrEmpty(Current.Language))
                {
                    string name = "";
                    try
                    {
                        name = Platform.Current.CurrentLanguage; // System.Globalization.CultureInfo.InstalledUICulture.Name;
                    }
                    catch (Exception ex)
                    {
                        //獲取系統語言失敗
                    }
                    if (name.ToLower().Contains("cn"))  // == "zh-cn")
                    {
                        Current.Language = "cn";
                    }
                    else
                    {
                        Current.Language = "tw";
                    }
                }

                if (Current.GlobalVariables == null)
                {
                    Current.GlobalVariables = Session.globalVariablesBasic.Clone();
                }

                if (String.IsNullOrEmpty(Session.Resolution))  // Season.PlatForm == PlatForm.iOS || Season.PlatForm == PlatForm.WinRT || Season.PlatForm == PlatForm.WP)
                {
                    Session.Resolution = Platform.PreferResolution;
                }
                Session.RealResolution = Session.Resolution = Setting.Current.Resolution;

            }
        }

    }
}
