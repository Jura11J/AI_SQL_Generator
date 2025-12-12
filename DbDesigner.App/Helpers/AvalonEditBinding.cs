using System.Windows;
using ICSharpCode.AvalonEdit;

namespace DbDesigner.App.Helpers;

public static class AvalonEditBinding
{
    public static readonly DependencyProperty BoundTextProperty =
        DependencyProperty.RegisterAttached(
            "BoundText",
            typeof(string),
            typeof(AvalonEditBinding),
            new FrameworkPropertyMetadata(string.Empty, OnBoundTextChanged));

    public static void SetBoundText(DependencyObject d, string value) =>
        d.SetValue(BoundTextProperty, value);

    public static string GetBoundText(DependencyObject d) =>
        (string)d.GetValue(BoundTextProperty);

    private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor editor)
        {
            editor.Text = e.NewValue as string ?? string.Empty;
        }
    }
}
