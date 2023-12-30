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

namespace Nidikwa.GUI
{
    /// <summary>
    /// Interaction logic for AltImage.xaml
    /// </summary>
    public partial class AltImage : UserControl
    {
        public ImageSource? Source { get => GetValue(SourceProperty) as ImageSource; set => SetValue(SourceProperty, value); }
        public ImageSource? AltSource { get => GetValue(AltSourceProperty) as ImageSource; set => SetValue(AltSourceProperty, value); }
        public bool AltMode { get => (bool)GetValue(AltModeProperty); set => SetValue(AltModeProperty, value); }

        public static DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(AltImage), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static DependencyProperty AltSourceProperty = DependencyProperty.Register(nameof(AltSource), typeof(ImageSource), typeof(AltImage), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static DependencyProperty AltModeProperty = DependencyProperty.Register(nameof(AltMode), typeof(bool), typeof(AltImage), new PropertyMetadata(false));
        public AltImage()
        {
            InitializeComponent();
        }
    }
}
