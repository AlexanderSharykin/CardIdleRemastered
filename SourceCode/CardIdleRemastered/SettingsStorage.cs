using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CardIdleRemastered
{
    public class SettingsStorage : FileStorage, ISettingsStorage
    {
        public string SessionId { get; set; }
        public string SteamLoginSecure { get; set; }
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
        public string SteamBadgeUrl { get; set; }
        public string SteamBadgeTitle { get; set; }

        public int IdleMode { get; set; }
        public string BadgeFilter { get; set; }
        public string ShowcaseFilter { get; set; }
        public byte MaxIdleProcessCount { get; set; }
        public byte PeriodicSwitchRepeatCount { get; set; }
        public double TrialPeriod { get; set; }
        public byte SwitchMinutes { get; set; }
        public byte SwitchSeconds { get; set; }

        public bool AllowShowcaseSync { get; set; }

        public bool ShowInTaskbar { get; set; }
        public bool ShowBackground { get; set; }

        public string Dimensions { get; set; }

        public int PricesCatalogDate { get; set; }

        public StringCollection IdleQueue { get; private set; }

        public StringCollection Blacklist { get; private set; }

        public StringCollection ShowcaseBookmarks { get; private set; }

        public StringCollection Games { get; private set; }

        public StringCollection AppBrushes { get; private set; }

        public void Init()
        {
            Blacklist = new StringCollection();
            ShowcaseBookmarks = new StringCollection();
            AppBrushes = new StringCollection();
            IdleQueue = new StringCollection();
            Games = new StringCollection();

            ShowInTaskbar = true;
            ShowBackground = true;

            ReadXml();
        }

        public void Save()
        {
            WriteXml();
        }

        private void ReadXml()
        {
            var content = ReadContent();
            if (String.IsNullOrWhiteSpace(content))
                return;

            try
            {
                var xml = XDocument.Parse(content).Root;
                if (xml != null)
                {
                    SessionId = (string)xml.Element("SessionId");
                    SteamLoginSecure = (string)xml.Element("SteamLoginSecure");
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
                    SteamBadgeTitle = (string)xml.Element("SteamBadgeTitle");
                    SteamBadgeUrl = (string)xml.Element("SteamBadgeUrl");

                    BadgeFilter = (string)xml.Element("BadgeFilter");
                    ShowcaseFilter = (string)xml.Element("ShowcaseFilter");
                    IdleMode = ReadInt(xml.Element("IdleMode"));
                    MaxIdleProcessCount = ReadByte(xml.Element("MaxIdleProcessCount"));
                    PeriodicSwitchRepeatCount = ReadByte(xml.Element("PeriodicSwitchRepeatCount"));
                    TrialPeriod = ReadDouble(xml.Element("TrialPeriod"));
                    SwitchMinutes = ReadByte(xml.Element("SwitchMinutes"));
                    SwitchSeconds = ReadByte(xml.Element("SwitchSeconds"));

                    AllowShowcaseSync = ReadBool(xml.Element("AllowShowcaseSync"));
                    ShowInTaskbar = ReadBool(xml.Element("ShowInTaskbar"), true);
                    ShowBackground = ReadBool(xml.Element("ShowBackground"), true);
                    Dimensions = (string)xml.Element("Dimensions");
                    PricesCatalogDate = ReadInt(xml.Element("PricesCatalogDate"));

                    IdleQueue.AddRange(GetStringList(xml.Element("IdleQueue")));
                    Blacklist.AddRange(GetStringList(xml.Element("Blacklist")));
                    ShowcaseBookmarks.AddRange(GetStringList(xml.Element("ShowcaseBookmarks")));
                    Games.AddRange(GetStringList(xml.Element("Games")));
                    AppBrushes.AddRange(GetStringList(xml.Element("AppBrushes")));
                }

                Logger.Info("Settings storage initialized");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Settings storage");
            }
        }

        private double ReadDouble(XElement xe)
        {
            double? i = (double?)xe;
            return i ?? 0;
        }

        private int ReadInt(XElement xe)
        {
            int? i = (int?)xe;
            return i ?? 0;
        }

        private bool ReadBool(XElement xe, bool missingValue = false)
        {
            bool? b = (bool?)xe;
            return b ?? missingValue;
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
            return xe.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void WriteXml()
        {
            var xe = new XElement("Settings",
                new XElement("SessionId", SessionId),
                new XElement("SteamLoginSecure", SteamLoginSecure),
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
                new XElement("SteamBadgeUrl", SteamBadgeUrl),
                new XElement("SteamBadgeTitle", SteamBadgeTitle),
                new XElement("BadgeFilter", BadgeFilter),
                new XElement("ShowcaseFilter", ShowcaseFilter),
                new XElement("IdleMode", IdleMode),
                new XElement("MaxIdleProcessCount", MaxIdleProcessCount),
                new XElement("PeriodicSwitchRepeatCount", PeriodicSwitchRepeatCount),
                new XElement("TrialPeriod", TrialPeriod),
                new XElement("SwitchMinutes", SwitchMinutes),
                new XElement("SwitchSeconds", SwitchSeconds),
                new XElement("AllowShowcaseSync", AllowShowcaseSync),
                new XElement("ShowInTaskbar", ShowInTaskbar),
                new XElement("ShowBackground", ShowBackground),
                new XElement("Dimensions", Dimensions),
                new XElement("PricesCatalogDate", PricesCatalogDate),
                new XElement("IdleQueue", String.Join(",", IdleQueue.Cast<string>())),
                new XElement("Blacklist", String.Join(",", Blacklist.Cast<string>())),
                new XElement("ShowcaseBookmarks", String.Join(",", ShowcaseBookmarks.Cast<string>())),
                new XElement("Games", String.Join(",", Games.Cast<string>())),
                new XElement("AppBrushes", String.Join(",", AppBrushes.Cast<string>()))
                );

            WriteContent(xe.ToString());
        }
    }
}
