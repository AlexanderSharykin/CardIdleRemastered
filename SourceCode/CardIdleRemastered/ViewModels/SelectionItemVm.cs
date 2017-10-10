using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered.ViewModels
{
    public abstract class SelectionItemVm : ObservableModel
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public object Key { get; set; }

        public abstract object Item { get; set; }
    }

    public class SelectionItemVm<T> : SelectionItemVm
    {
        private T _value;

        public T Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public override object Item
        {
            get { return Value; }
            set { Value = (T)value; }
        }
    }
}
