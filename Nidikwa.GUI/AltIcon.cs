using System.Windows;
using Wpf.Ui.Controls;

namespace Nidikwa.GUI
{
    /// <summary>
    /// Interaction logic for AltImage.xaml
    /// </summary>
    public class AltIcon : SymbolIcon
    {
        private SymbolRegular basicSymbol;
        public SymbolRegular AltSymbol { get => (SymbolRegular)GetValue(AltSymbolProperty); set => SetValue(AltSymbolProperty, value); }
        public bool AltMode { get => (bool)GetValue(AltModeProperty); set => SetValue(AltModeProperty, value); }

        public static DependencyProperty AltSymbolProperty = DependencyProperty.Register(nameof(AltSymbol), typeof(SymbolRegular), typeof(AltIcon), new FrameworkPropertyMetadata(default(SymbolRegular), FrameworkPropertyMetadataOptions.AffectsRender, AltSymbolChanged));

        private static void AltSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (SymbolRegular)e.NewValue;
            var item = (AltIcon)d;
            item.OnAltSymbolChanged(value);
        }

        public static DependencyProperty AltModeProperty = DependencyProperty.Register(nameof(AltMode), typeof(bool), typeof(AltIcon), new PropertyMetadata(false, AltModeChanged));
        private bool ignoreSymbolChanges = false;
        protected virtual void OnAltModeChanged(bool newValue)
        {
            ignoreSymbolChanges = true;
            if (newValue)
            {
                Symbol = AltSymbol;
            }
            else
            {
                Symbol = basicSymbol;
            }
            ignoreSymbolChanges = false;
        }

        protected virtual void OnAltSymbolChanged(SymbolRegular newValue)
        {
            if (AltMode)
            {
                ignoreSymbolChanges = true;
                Symbol = newValue;
                ignoreSymbolChanges = false;
            }
        }

        protected virtual void OnSymbolChanged(SymbolRegular newValue)
        {
            if (!ignoreSymbolChanges)
            {
                basicSymbol = newValue;
                if (AltMode)
                {
                    ignoreSymbolChanges = true;
                    Symbol = AltSymbol;
                    ignoreSymbolChanges = false;
                }
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == SymbolProperty)
                OnSymbolChanged((SymbolRegular)e.NewValue);
        }

        private static void AltModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (bool)e.NewValue;
            var item = (AltIcon)d;
            item.OnAltModeChanged(value);
        }
    }
}
