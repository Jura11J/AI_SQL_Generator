using System.Windows.Input;
using DbDesigner.App.Theme;

namespace DbDesigner.App.ViewModels;

public enum ThemeMode
{
    Light,
    Dark
}

public enum ChatBackendMode
{
    LocalStub,
    ExternalApi
}

public class SettingsViewModel : ViewModelBase
{
    private ThemeMode _theme = ThemeMode.Light;
    private ChatBackendMode _chatBackend = ChatBackendMode.LocalStub;
    private string _apiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private string _apiKey = string.Empty;

    public SettingsViewModel()
    {
        ApplyThemeCommand = new RelayCommand(_ => ThemeManager.ApplyTheme(Theme));
    }

    public ThemeMode Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                OnPropertyChanged(nameof(IsLightTheme));
                OnPropertyChanged(nameof(IsDarkTheme));
            }
        }
    }

    public ChatBackendMode ChatBackend
    {
        get => _chatBackend;
        set
        {
            if (SetProperty(ref _chatBackend, value))
            {
                OnPropertyChanged(nameof(IsLocalChat));
                OnPropertyChanged(nameof(IsApiChat));
            }
        }
    }

    public bool IsLightTheme
    {
        get => Theme == ThemeMode.Light;
        set
        {
            if (value)
            {
                Theme = ThemeMode.Light;
            }
        }
    }

    public bool IsDarkTheme
    {
        get => Theme == ThemeMode.Dark;
        set
        {
            if (value)
            {
                Theme = ThemeMode.Dark;
            }
        }
    }

    public bool IsLocalChat
    {
        get => ChatBackend == ChatBackendMode.LocalStub;
        set
        {
            if (value)
            {
                ChatBackend = ChatBackendMode.LocalStub;
            }
        }
    }

    public bool IsApiChat
    {
        get => ChatBackend == ChatBackendMode.ExternalApi;
        set
        {
            if (value)
            {
                ChatBackend = ChatBackendMode.ExternalApi;
            }
        }
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public ICommand ApplyThemeCommand { get; }
}
