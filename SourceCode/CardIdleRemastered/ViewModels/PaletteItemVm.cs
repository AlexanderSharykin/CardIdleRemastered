using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CardIdleRemastered
{
    public class PaletteItemVm : ObservableModel
    {
        public PaletteItemVm(string name, Color? color)
        {
            Name = name;
            _brushColor = color;
        }

        public string Name { get; set; }

        private Color? _brushColor;
        public Color? BrushColor
        {
            get { return _brushColor; }
            set
            {
                _brushColor = value;
                if (_brushColor.HasValue)
                    Application.Current.Resources[Name] = new SolidColorBrush(_brushColor.Value);
                OnPropertyChanged();
            }
        }
    }
}
