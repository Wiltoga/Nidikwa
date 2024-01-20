using System.Windows;
using System.Windows.Media;

namespace Nidikwa.GUI
{
    public class SamplesViewer : FrameworkElement
    {
        public static DependencyProperty MaximumScopeProperty = DependencyProperty.Register(nameof(MaximumScope), typeof(float), typeof(SamplesViewer), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));
        public static DependencyProperty MinimumScopeProperty = DependencyProperty.Register(nameof(MinimumScope), typeof(float), typeof(SamplesViewer), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static DependencyProperty SamplesProperty = DependencyProperty.Register(nameof(Samples), typeof(IEnumerable<float>), typeof(SamplesViewer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public float MaximumScope { get => (float)GetValue(MaximumScopeProperty); set => SetValue(MaximumScopeProperty, value); }
        public float MinimumScope { get => (float)GetValue(MinimumScopeProperty); set => SetValue(MinimumScopeProperty, value); }
        public IEnumerable<float>? Samples { get => (IEnumerable<float>)GetValue(SamplesProperty); set => SetValue(SamplesProperty, value); }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

        }
    }
}
