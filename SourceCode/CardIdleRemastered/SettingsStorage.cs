using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace CardIdleRemastered
{
    public class SettingsStorage : FileStorage, ISettingsStorage
    {
        #region Fields

        private readonly StringSettingsValue _sessionId = new StringSettingsValue { Key = "SessionId" };
        private readonly StringSettingsValue _steamLoginSecure = new StringSettingsValue { Key = "SteamLoginSecure" };
        private readonly StringSettingsValue _steamProfileUrl = new StringSettingsValue { Key = "SteamProfileUrl" };
        private readonly StringSettingsValue _steamParental = new StringSettingsValue { Key = "SteamParental" };
        private readonly StringSettingsValue _steamRememberLogin = new StringSettingsValue { Key = "SteamRememberLogin" };
        private readonly StringSettingsValue _machineAuth = new StringSettingsValue { Key = "MachineAuth" };

        private readonly StringSettingsValue _steamAvatarUrl = new StringSettingsValue { Key = "SteamAvatarUrl" };
        private readonly StringSettingsValue _steamBackgroundUrl = new StringSettingsValue { Key = "SteamBackgroundUrl" };
        private readonly StringSettingsValue _steamUserName = new StringSettingsValue { Key = "SteamUserName" };
        private readonly StringSettingsValue _steamLevel = new StringSettingsValue { Key = "SteamLevel" };
        private readonly StringSettingsValue _customBackgroundUrl = new StringSettingsValue { Key = "CustomBackgroundUrl" };
        private readonly StringSettingsValue _steamBadgeUrl = new StringSettingsValue { Key = "SteamBadgeUrl" };
        private readonly StringSettingsValue _steamBadgeTitle = new StringSettingsValue { Key = "SteamBadgeTitle" };

        private readonly StringSettingsValue _dimensions = new StringSettingsValue { Key = "Dimensions" };
        private readonly StringSettingsValue _showcaseFilter = new StringSettingsValue { Key = "ShowcaseFilter" };
        private readonly StringSettingsValue _badgeFilter = new StringSettingsValue { Key = "BadgeFilter" };

        private readonly BoolSettingsValue _showBackground = new BoolSettingsValue { Key = "ShowBackground", DefaultValue = true };
        private readonly BoolSettingsValue _showInTaskbar = new BoolSettingsValue { Key = "ShowInTaskbar", DefaultValue = true };
        private readonly BoolSettingsValue _allowShowcaseSync = new BoolSettingsValue { Key = "AllowShowcaseSync", DefaultValue = true };
        private readonly BoolSettingsValue _ignoreClient = new BoolSettingsValue { Key = "IgnoreClient" };

        private readonly ByteSettingsValue _maxIdleProcessCount = new ByteSettingsValue { Key = "MaxIdleProcessCount", Minimum = 1, DefaultValue = AppConstants.DefaultIdleInstanceCount };
        private readonly ByteSettingsValue _periodicSwitchRepeatCount = new ByteSettingsValue { Key = "PeriodicSwitchRepeatCount", DefaultValue = 1, Minimum = 1 };
        private readonly ByteSettingsValue _switchMinutes = new ByteSettingsValue { Key = "SwitchMinutes" };
        private readonly ByteSettingsValue _switchSeconds = new ByteSettingsValue { Key = "SwitchSeconds", DefaultValue = AppConstants.DefaultSwitchSeconds };

        private readonly IntSettingsValue _pricesCatalogDate = new IntSettingsValue { Key = "PricesCatalogDate" };
        private readonly IntSettingsValue _idleMode = new IntSettingsValue { Key = "IdleMode" };

        private readonly DoibleSettingsValue _trialPeriod = new DoibleSettingsValue { Key = "TrialPeriod", Minimum = 0.1, DefaultValue = AppConstants.DefaultTrialPeriod };
        private readonly StringCollectionSettingsValue _appBrushes = new StringCollectionSettingsValue { Key = "AppBrushes" };
        private readonly StringCollectionSettingsValue _games = new StringCollectionSettingsValue { Key = "Games" };
        private readonly StringCollectionSettingsValue _showcaseBookmarks = new StringCollectionSettingsValue { Key = "ShowcaseBookmarks" };
        private readonly StringCollectionSettingsValue _blacklist = new StringCollectionSettingsValue { Key = "Blacklist" };
        private readonly StringCollectionSettingsValue _idleQueue = new StringCollectionSettingsValue { Key = "IdleQueue" };

        #endregion

        private List<IXmlSettings> _settings;

        public void Init()
        {
            Type ts = typeof(IXmlSettings);
            _settings = typeof(SettingsStorage).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(fi => ts.IsAssignableFrom(fi.FieldType))
                .Select(fi => fi.GetValue(this))
                .OfType<IXmlSettings>()
                .ToList();

            ReadXml();

            foreach (IXmlSettings xs in _settings)
            {
                xs.ValueChanged += (sender, args) => this.Save();
            }
        }

        private void ReadXml()
        {
            var content = ReadContent();

            XElement xml = null;
            try
            {
                if (false == String.IsNullOrWhiteSpace(content))
                    xml = XDocument.Parse(content).Root;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Settings storage");
            }

            foreach (var setting in _settings)
            {
                var xe = xml != null ? xml.Element(setting.Key) : null;
                setting.FromXml(xe);
            }

            Logger.Info("Settings storage initialized");
        }

        public void Save()
        {
            WriteXml();
        }

        private void WriteXml()
        {
            var xe = new XElement("Settings");
            foreach (var setting in _settings)
            {
                xe.Add(setting.ToXml());
            }
            WriteContent(xe.ToString());
        }

        #region Properties

        public string SessionId
        {
            get { return _sessionId.Value; }
            set { _sessionId.Value = value; }
        }

        public string SteamLoginSecure
        {
            get { return _steamLoginSecure.Value; }
            set { _steamLoginSecure.Value = value; }
        }

        public string SteamProfileUrl
        {
            get { return _steamProfileUrl.Value; }
            set { _steamProfileUrl.Value = value; }
        }

        public string SteamParental
        {
            get { return _steamParental.Value; }
            set { _steamParental.Value = value; }
        }

        public string SteamRememberLogin
        {
            get { return _steamRememberLogin.Value; }
            set { _steamRememberLogin.Value = value; }
        }

        public string MachineAuth
        {
            get { return _machineAuth.Value; }
            set { _machineAuth.Value = value; }
        }

        public bool IgnoreClient
        {
            get { return _ignoreClient.Value; }
            set { _ignoreClient.Value = value; }
        }

        public string SteamAvatarUrl
        {
            get { return _steamAvatarUrl.Value; }
            set { _steamAvatarUrl.Value = value; }
        }

        public string SteamBackgroundUrl
        {
            get { return _steamBackgroundUrl.Value; }
            set { _steamBackgroundUrl.Value = value; }
        }

        public string SteamUserName
        {
            get { return _steamUserName.Value; }
            set { _steamUserName.Value = value; }
        }

        public string SteamLevel
        {
            get { return _steamLevel.Value; }
            set { _steamLevel.Value = value; }
        }

        public string CustomBackgroundUrl
        {
            get { return _customBackgroundUrl.Value; }
            set { _customBackgroundUrl.Value = value; }
        }

        public string SteamBadgeUrl
        {
            get { return _steamBadgeUrl.Value; }
            set { _steamBadgeUrl.Value = value; }
        }

        public string SteamBadgeTitle
        {
            get { return _steamBadgeTitle.Value; }
            set { _steamBadgeTitle.Value = value; }
        }

        public int IdleMode
        {
            get { return _idleMode.Value; }
            set { _idleMode.Value = value; }
        }

        public string BadgeFilter
        {
            get { return _badgeFilter.Value; }
            set { _badgeFilter.Value = value; }
        }

        public string ShowcaseFilter
        {
            get { return _showcaseFilter.Value; }
            set { _showcaseFilter.Value = value; }
        }

        public byte MaxIdleProcessCount
        {
            get { return _maxIdleProcessCount.Value; }
            set { _maxIdleProcessCount.Value = value; }
        }

        public byte PeriodicSwitchRepeatCount
        {
            get { return _periodicSwitchRepeatCount.Value; }
            set { _periodicSwitchRepeatCount.Value = value; }
        }

        public double TrialPeriod
        {
            get { return _trialPeriod.Value; }
            set { _trialPeriod.Value = value; }
        }

        public byte SwitchMinutes
        {
            get { return _switchMinutes.Value; }
            set { _switchMinutes.Value = value; }
        }

        public byte SwitchSeconds
        {
            get { return _switchSeconds.Value; }
            set { _switchSeconds.Value = value; }
        }

        public bool AllowShowcaseSync
        {
            get { return _allowShowcaseSync.Value; }
            set { _allowShowcaseSync.Value = value; }
        }

        public bool ShowInTaskbar
        {
            get { return _showInTaskbar.Value; }
            set { _showInTaskbar.Value = value; }
        }

        public bool ShowBackground
        {
            get { return _showBackground.Value; }
            set { _showBackground.Value = value; }
        }

        public string Dimensions
        {
            get { return _dimensions.Value; }
            set { _dimensions.Value = value; }
        }

        public int PricesCatalogDate
        {
            get { return _pricesCatalogDate.Value; }
            set { _pricesCatalogDate.Value = value; }
        }

        public StringCollection IdleQueue
        {
            get { return _idleQueue.Value; }
            private set { _idleQueue.Value = value; }
        }

        public StringCollection Blacklist
        {
            get { return _blacklist.Value; }
            private set { _blacklist.Value = value; }
        }

        public StringCollection ShowcaseBookmarks
        {
            get { return _showcaseBookmarks.Value; }
            private set { _showcaseBookmarks.Value = value; }
        }

        public StringCollection Games
        {
            get { return _games.Value; }
            private set { _games.Value = value; }
        }

        public StringCollection AppBrushes
        {
            get { return _appBrushes.Value; }
            private set { _appBrushes.Value = value; }
        }

        #endregion

    }
}
