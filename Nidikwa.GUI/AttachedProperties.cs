using Nidikwa.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Nidikwa.GUI;

public static class DestroyableContextAttachedProperty
{
    private const string Name = "DestroyableContext";
    public static DependencyProperty DestroyableContextProperty = DependencyProperty.RegisterAttached(Name, typeof(DestroyableReactiveObject), typeof(Window), new PropertyMetadata(callback));

    private static void callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var window = d as Window;
        if (window is null)
            return;

        window.DataContext = e.NewValue;

        if (e.OldValue is null && e.NewValue is not null)
        {
            window.Closed += Window_Closed;
        }
        if (e.OldValue is not null && e.NewValue is null)
        {
            window.Closed -= Window_Closed;
        }
    }

    private static void Window_Closed(object? sender, EventArgs e)
    {
        var window = sender as Window;
        if (window is null)
            return;

        GetDestroyableContext(window)?.Destroy();
    }

    public static DestroyableReactiveObject? GetDestroyableContext(DependencyObject obj) => obj.GetValue(DestroyableContextProperty) as DestroyableReactiveObject;
    public static void SetDestroyableContext(DependencyObject obj, DestroyableReactiveObject? value) => obj.SetValue(DestroyableContextProperty, value);
}
