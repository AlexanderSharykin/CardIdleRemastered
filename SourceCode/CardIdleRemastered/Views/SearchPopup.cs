using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CardIdleRemastered.Views
{
    public class SearchPopup : UserControl
    {
        static SearchPopup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchPopup), new FrameworkPropertyMetadata(typeof(SearchPopup)));
        }

        public static readonly DependencyProperty SearchOptionsProperty =
            DependencyProperty.Register("SearchOptions", typeof(IEnumerable), typeof(SearchPopup), new PropertyMetadata(default(IEnumerable)));

        public IEnumerable SearchOptions
        {
            get { return (IEnumerable)GetValue(SearchOptionsProperty); }
            set { SetValue(SearchOptionsProperty, value); }
        }

        public static readonly DependencyProperty SearchOptionTemplateProperty =
            DependencyProperty.Register("SearchOptionTemplate", typeof(DataTemplate), typeof(SearchPopup), new PropertyMetadata(default(DataTemplate)));

        public DataTemplate SearchOptionTemplate
        {
            get { return (DataTemplate)GetValue(SearchOptionTemplateProperty); }
            set { SetValue(SearchOptionTemplateProperty, value); }
        }
    }
}
