using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CardIdleRemastered.ViewModels
{
    public class FilterStatesCollection : ObservableModel, IEnumerable<SelectionItemVm<FilterState>>
    {
        private IList<SelectionItemVm<FilterState>> _items;
        private PropertyChangedEventHandler _notifier;

        private FilterStatesCollection()
        {
        }

        public bool HasActiveFilters
        {
            get { return _items.Any(x => x.Value != FilterState.Any); }
        }

        public static FilterStatesCollection Create<T>()
        {
            return new FilterStatesCollection
                   {
                       _items = Enums.GetValues<T>()
                           .Select(p => GetFilterItem((Enum)(object)p))
                           .ToList()
                   };
        }

        private static SelectionItemVm<FilterState> GetFilterItem(Enum e)
        {
            return new SelectionItemVm<FilterState>
            {
                Key = e,
                Value = FilterState.Any
            };
        }

        public FilterStatesCollection SetNotifier(Action handler)
        {
            // detach previous event listener
            if (_notifier != null)
                foreach (var item in _items)
                    item.PropertyChanged -= _notifier;

            if (handler != null)
            {
                // create and attach new event listener
                _notifier = (sender, e) =>
                {
                    if (e.PropertyName == "Value")
                    {
                        handler();
                        OnPropertyChanged("HasActiveFilters");
                    }
                };

                foreach (var item in _items)
                    item.PropertyChanged += _notifier;
            }
            else
                _notifier = null;

            return this;
        }

        public string Serialize()
        {
            return String.Join(";", _items.Select(f => String.Format("{0}:{1}", f.Key, (int)f.Value)));
        }

        public void Deserialize<T>(string filtersList)
        {
            if (string.IsNullOrWhiteSpace(filtersList))
                return;

            var enumType = typeof(T);

            // parsing filter list
            string[] parts = filtersList.Split(';');
            foreach (string property in parts)
            {
                string[] keyValue = property.Split(':');
                if (keyValue.Length < 2)
                    continue;
                FilterState value = (FilterState)int.Parse(keyValue[1]);

                if (value != FilterState.Any)
                {
                    var key = Enum.Parse(enumType, keyValue[0]);
                    var filter = _items.First(f => key.Equals(f.Key));
                    filter.Value = value;
                }
            }
        }

        public IEnumerator<SelectionItemVm<FilterState>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
