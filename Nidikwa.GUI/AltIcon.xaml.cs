using System;
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
using Wpf.Ui.Controls;

namespace Nidikwa.GUI
{
    /// <summary>
    /// Interaction logic for AltImage.xaml
    /// </summary>
    public partial class AltIcon : UserControl
    {
        public SymbolRegular? Symbol { get => GetValue(SymbolProperty) as SymbolRegular?; set => SetValue(SymbolProperty, value); }
        public SymbolRegular? AltSymbol { get => GetValue(AltSymbolProperty) as SymbolRegular?; set => SetValue(AltSymbolProperty, value); }
        public bool AltMode { get => (bool)GetValue(AltModeProperty); set => SetValue(AltModeProperty, value); }

        public static DependencyProperty SymbolProperty = DependencyProperty.Register(nameof(Symbol), typeof(SymbolRegular?), typeof(AltIcon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static DependencyProperty AltSymbolProperty = DependencyProperty.Register(nameof(AltSymbol), typeof(SymbolRegular?), typeof(AltIcon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static DependencyProperty AltModeProperty = DependencyProperty.Register(nameof(AltMode), typeof(bool), typeof(AltIcon), new PropertyMetadata(false));
        public AltIcon()
        {
            InitializeComponent();
        }
    }
}
