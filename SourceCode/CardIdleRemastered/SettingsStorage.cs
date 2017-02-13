using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CardIdleRemastered
{
    public class SettingsStorage: ISettingsStorage
    {
        public string SessionId { get; set; }
        public string SteamLogin { get; set; }
        public string SteamProfileUrl { get; set; }
        public string SteamParental { get; set; }
        public string SteamRememberLogin { get; set; }
        public string MachineAuth { get; set; }
        
        public bool IgnoreClient { get; set; }

        public string SteamAvatarUrl { get; set; }
        public string SteamBackgroundUrl { get; set; }
        public string SteamUserName { get; set; }
        public string SteamLevel { get; set; }
        public string CustomBackgroundUrl { get; set; }

        public int IdleMode { get; set; }
        public string BadgeFilter { get; set; }
        public byte MaxIdleProcessCount { get; set; }
        public byte SwitchMinutes { get; set; }
        public byte SwitchSeconds { get; set; }

        public StringCollection IdleQueue { get; private set; }

        public StringCollection Blacklist { get; private set; }

        public StringCollection Games { get; private set; }

        public StringCollection AppBrushes { get; private set; }

        public string FileName { get; set; }

        public void Init()
        {
            Blacklist = new StringCollection();
            AppBrushes = new StringCollection();
            IdleQueue = new StringCollection();
            Games = new StringCollection();
            
            ReadXml();
        }

        public void Save()
        {
            WriteXml();
        }

        private void ReadXml()
        {
            if (File.Exists(FileName) == false)
            {
                Logger.Info(FileName + Environment.NewLine + "Settings storage file not found.");
                File.Create(FileName);                
                return;
            }

            try
            {
                var xml = XDocument.Load(new StreamReader(FileName, Encoding.UTF8)).Root;
                if (xml != null)
                {
                    SessionId = (string)xml.Element("SessionId");
                    SteamLogin = (string)xml.Element("SteamLogin");
                    SteamProfileUrl = (string)xml.Element("SteamProfileUrl");
                    SteamParental = (string)xml.Element("SteamParental");
                    SteamRememberLogin = (string)xml.Element("SteamRememberLogin");
                    MachineAuth = (string)xml.Element("MachineAuth");

                    IgnoreClient = ReadBool(xml.Element("IgnoreClient"));

                    SteamAvatarUrl = (string)xml.Element("SteamAvatarUrl");
                    SteamBackgroundUrl = (string)xml.Element("SteamBackgroundUrl");
                    SteamUserName = (string)xml.Element("SteamUserName");
                    SteamLevel = (string)xml.Element("SteamLevel");
                    CustomBackgroundUrl = (string)xml.Element("CustomBackgroundUrl");

                    BadgeFilter = (string)xml.Element("BadgeFilter");
                    IdleMode = ReadInt(xml.Element("IdleMode"));
                    MaxIdleProcessCount = ReadByte(xml.Element("MaxIdleProcessCount"));
                    SwitchMinutes = ReadByte(xml.Element("SwitchMinutes"));
                    SwitchSeconds = ReadByte(xml.Element("SwitchSeconds"));

                    IdleQueue.AddRange(GetStringList(xml.Element("IdleQueue")));
                    Blacklist.AddRange(GetStringList(xml.Element("Blacklist")));
                    Games.AddRange(GetStringList(xml.Element("Games")));
                    AppBrushes.AddRange(GetStringList(xml.Element("AppBrushes")));
                }

                Logger.Info(String.Format("Settings storage initialized" + Environment.NewLine +FileName));
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, FileName);                
            }
        }

        private int ReadInt(XElement xe)
        {
            int? i = (int?)xe;
            return i ?? 0;
        }

        private bool ReadBool(XElement xe)
        {
            bool? b = (bool?)xe;
            return b ?? false;
        }

        private byte ReadByte(XElement xe)
        {
            int? i = (int?)xe;
            if (i.HasValue)
                return (byte)i.Value;
            return 0;
        }

        private string[] GetStringList(XElement xe)
        {
            if (xe == null)
                return new string[0];
            return xe.Value.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
        }

        private void WriteXml()
        {
            var xe = new XElement("Settings",
                new XElement("SessionId", SessionId),
                new XElement("SteamLogin", SteamLogin),
                new XElement("SteamProfileUrl", SteamProfileUrl),
                new XElement("SteamParental", SteamParental),
                new XElement("SteamRememberLogin", SteamRememberLogin),
                new XElement("MachineAuth", MachineAuth),
                new XElement("IgnoreClient", IgnoreClient),
                new XElement("SteamAvatarUrl", SteamAvatarUrl),
                new XElement("SteamBackgroundUrl", SteamBackgroundUrl),
                new XElement("SteamUserName", SteamUserName),
                new XElement("SteamLevel", SteamLevel),
                new XElement("CustomBackgroundUrl", CustomBackgroundUrl),
                new XElement("BadgeFilter", BadgeFilter),
                new XElement("IdleMode", IdleMode),
                new XElement("MaxIdleProcessCount", MaxIdleProcessCount),
                new XElement("SwitchMinutes", SwitchMinutes),
                new XElement("SwitchSeconds", SwitchSeconds),
                new XElement("IdleQueue", String.Join(",", IdleQueue.Cast<string>())),
                new XElement("Blacklist", String.Join(",", Blacklist.Cast<string>())),
                new XElement("Games", String.Join(",", Games.Cast<string>())),
                new XElement("AppBrushes", String.Join(",", AppBrushes.Cast<string>()))
                );

            File.WriteAllText(FileName, xe.ToString(), Encoding.UTF8);
        }
    }
}
