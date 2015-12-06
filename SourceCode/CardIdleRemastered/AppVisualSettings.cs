using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered
{
    public class AppVisualSettings: ObservableModel
    {
        private string _backgroundUrl;
        private byte _idleProcessCount;

        public AppVisualSettings()
        {
            AppBrushes = new ObservableCollection<AppBrush>();
        }

        public string BackgroundUrl
        {
            get { return _backgroundUrl; }
            set
            {
                _backgroundUrl = value;
                OnPropertyChanged();
            }
        }

        public byte IdleProcessCount
        {
            get { return _idleProcessCount; }
            set
            {
                _idleProcessCount = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<AppBrush> AppBrushes { get; private set; }

        public AppVisualSettings Copy()
        {
            var vis = new AppVisualSettings();
            vis.BackgroundUrl = BackgroundUrl;
            vis.IdleProcessCount = IdleProcessCount;

            foreach (var b in AppBrushes)
            {
                vis.AppBrushes.Add(new AppBrush(b.Name, b.BrushColor));
            }
            return vis;
        }
    }
}
