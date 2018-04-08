using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace CardIdleRemastered
{
    public class PaletteItemsCollection : IEnumerable<PaletteItemVm>
    {
        private ObservableCollection<PaletteItemVm> _items;
        private PropertyChangedEventHandler _notifier;

        private PaletteItemsCollection()
        {
        }

        public static PaletteItemsCollection Create()
        {
            var resources = App.CardIdle.Resources;
            var brushes = resources.Keys
                .OfType<string>()
                .Where(key => key.StartsWith("Dyn"))
                .Select(resKey => new PaletteItemVm(resKey, ((SolidColorBrush)resources[resKey]).Color))
                .ToList();

            var palette = new PaletteItemsCollection();
            palette._items = new ObservableCollection<PaletteItemVm>(brushes);
            return palette;
        }

        public void Deserialize(IEnumerable<string> items)
        {
            foreach (var text in items)
            {
                var parts = text.Split(';');
                var key = parts[0];

                var brush = _items.FirstOrDefault(x => x.Name == key);
                if (brush == null)
                    continue;

                brush.BrushColor = (Color)ColorConverter.ConvertFromString(parts[1]);
            }
        }

        public void SetNotifier(Action handler)
        {
            if (_notifier != null)
                foreach (var item in _items)
                    item.PropertyChanged -= _notifier;

            if (handler != null)
            {
                // create and attach new event listener
                _notifier = (sender, e) =>
                {
                    if (e.PropertyName == "BrushColor")
                        handler();
                };

                foreach (var item in _items)
                    item.PropertyChanged += _notifier;
            }
            else
                _notifier = null;
        }

        public IEnumerable<string> Serialize()
        {
            return _items.Select(x => String.Format("{0};{1}", x.Name, x.BrushColor));
        }

        public IEnumerator<PaletteItemVm> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
