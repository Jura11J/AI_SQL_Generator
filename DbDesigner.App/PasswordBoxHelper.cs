using System.Windows;
using System.Windows.Controls;

namespace DbDesigner.App;

public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false, OnBindPasswordChanged));

    public static readonly DependencyProperty BindablePasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindablePassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(string.Empty, OnBindablePasswordChanged));

    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached(
            "UpdatingPassword",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false));

    public static void SetBindPassword(DependencyObject obj, bool value) =>
        obj.SetValue(BindPasswordProperty, value);

    public static bool GetBindPassword(DependencyObject obj) =>
        (bool)obj.GetValue(BindPasswordProperty);

    public static void SetBindablePassword(DependencyObject obj, string value) =>
        obj.SetValue(BindablePasswordProperty, value);

    public static string GetBindablePassword(DependencyObject obj) =>
        (string)obj.GetValue(BindablePasswordProperty);

    private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            passwordBox.PasswordChanged += PasswordBoxOnPasswordChanged;
        }
        else
        {
            passwordBox.PasswordChanged -= PasswordBoxOnPasswordChanged;
        }
    }

    private static void OnBindablePasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= PasswordBoxOnPasswordChanged;
        if (!GetUpdatingPassword(passwordBox))
        {
            passwordBox.Password = e.NewValue as string ?? string.Empty;
        }

        passwordBox.PasswordChanged += PasswordBoxOnPasswordChanged;
    }

    private static void PasswordBoxOnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        SetUpdatingPassword(passwordBox, true);
        SetBindablePassword(passwordBox, passwordBox.Password);
        SetUpdatingPassword(passwordBox, false);
    }

    private static void SetUpdatingPassword(DependencyObject obj, bool value) =>
        obj.SetValue(UpdatingPasswordProperty, value);

    private static bool GetUpdatingPassword(DependencyObject obj) =>
        (bool)obj.GetValue(UpdatingPasswordProperty);
}
