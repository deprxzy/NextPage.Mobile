using System.Net;
using Microsoft.Maui.Controls.Shapes;
using NextPage.Mobile.Core;
using NextPage.Mobile.Services;

namespace NextPage.Mobile.Views;

public static class Ui
{
    public static readonly Color Ink = Color.FromArgb("#172554");
    public static readonly Color Blue = Color.FromArgb("#1D4ED8");
    public static readonly Color Muted = Color.FromArgb("#334155");
    public static readonly Color Background = Color.FromArgb("#F1F5F9");
    public static Label Text(string text = "", double size = 15, bool bold = false) => new()
    {
        Text = text, FontSize = size, TextColor = Ink,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None
    };
    public static Label Small(string text) => new() { Text = text, FontSize = 14, TextColor = Muted };
    public static Entry Field(string placeholder, Keyboard? keyboard = null, bool password = false) => new()
    {
        Placeholder = placeholder, Keyboard = keyboard ?? Keyboard.Default, IsPassword = password,
        TextColor = Ink, PlaceholderColor = Muted, BackgroundColor = Color.FromArgb("#F8FAFC"),
        MinimumHeightRequest = 52, FontSize = 16,
        Margin = new Thickness(0, 2), ClearButtonVisibility = password ? ClearButtonVisibility.Never : ClearButtonVisibility.WhileEditing
    };
    public static Editor Editor(string placeholder, int max = 5000) => new()
    {
        Placeholder = placeholder, MaxLength = max, AutoSize = EditorAutoSizeOption.TextChanges,
        MinimumHeightRequest = 100, FontSize = 16, TextColor = Ink, PlaceholderColor = Muted, BackgroundColor = Colors.White
    };
    public static Border Card(params View[] views)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        foreach (var view in views) stack.Children.Add(view);
        return new Border
        {
            BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E2E8F0"), Padding = 14,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = stack
        };
    }
    public static string Required(Entry field, string name)
    {
        var text = field.Text?.Trim() ?? "";
        if (text.Length == 0) throw new InvalidOperationException($"Preencha {name}.");
        return text;
    }
    public static string Email(Entry field)
    {
        var email = Required(field, "o e-mail");
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            throw new InvalidOperationException("Informe um e-mail válido.");
        return email;
    }
    public static string Password(Entry field, Entry confirm)
    {
        var password = field.Text ?? "";
        if (password.Length < 8) throw new InvalidOperationException("A senha deve ter pelo menos 8 caracteres.");
        if (password != confirm.Text) throw new InvalidOperationException("As senhas não são iguais.");
        return password;
    }
}
public abstract class AppPage : ContentPage
{
    protected readonly AppServices Services;
    protected readonly VerticalStackLayout Body = new() { Spacing = 12, Padding = 14 };
    private readonly Label message = Ui.Text();
    private readonly ActivityIndicator spinner = new() { Color = Ui.Blue, IsVisible = false };
    private readonly VerticalStackLayout status = new() { Spacing = 6, Padding = new Thickness(16, 4) };
    private View? bodyView;
    private View? footer;
    private bool working;
    protected AppPage(AppServices services, string title)
    {
        Services = services;
        Title = title;
        BackgroundColor = Ui.Background;
        message.IsVisible = false;
        status.Children.Add(spinner);
        status.Children.Add(message);
        SetBody(new ScrollView { Content = Body });
    }
    protected void SetBody(View view)
    {
        bodyView = view;
        var grid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) } };
        grid.Children.Add(status);
        Grid.SetRow(view, 1);
        grid.Children.Add(view);
        if (footer is not null) { Grid.SetRow(footer, 2); grid.Children.Add(footer); }
        Content = grid;
    }
    protected void SetFooter(View view)
    {
        if (Content is not Grid grid) return;
        if (footer is not null) grid.Children.Remove(footer);
        footer = view;
        Grid.SetRow(view, 2);
        grid.Children.Add(view);
    }
    protected Button Action(string text, Func<Task> action, bool secondary = false)
    {
        var button = new Button
        {
            Text = text, BackgroundColor = secondary ? Colors.White : Ui.Blue,
            TextColor = secondary ? Ui.Blue : Colors.White, CornerRadius = 10,
            BorderColor = secondary ? Ui.Blue : Colors.Transparent, BorderWidth = secondary ? 1 : 0,
            MinimumHeightRequest = 46
        };
        button.Clicked += async (_, _) =>
        {
            if (working) return;
            button.IsEnabled = false;
            try { await ExecuteAsync(action); }
            catch (Exception ex) { Diagnostics.AppDiagnostics.Log(ex); }
            finally
            {
                try { if (button.Handler is not null) button.IsEnabled = true; }
                catch (Exception ex) { Diagnostics.AppDiagnostics.Log(ex); }
            }
        };
        return button;
    }
    protected void Notice(string text, bool error = false)
    {
        message.Text = text;
        message.TextColor = error ? Color.FromArgb("#B91C1C") : Color.FromArgb("#166534");
        message.IsVisible = !string.IsNullOrWhiteSpace(text);
    }
    protected async Task ExecuteAsync(Func<Task> action)
    {
        if (working) return;
        working = true;
        spinner.IsVisible = spinner.IsRunning = true;

        Notice("");
        try { await action(); }
        catch (ApiException ex)
        {
            Notice(ex.Message, true);
            if (ex.Status == HttpStatusCode.Unauthorized && this is not LoginPage && this is not CadastroPage)
                await DisplayAlertAsync("Entre novamente", ex.Message, "OK");
        }
        catch (InvalidOperationException ex) { Notice(ex.Message, true); }
        catch (Exception ex)
        {
            Diagnostics.AppDiagnostics.Log(ex);
            Notice("Não foi possível concluir. Tente novamente.", true);
        }
        finally
        {
            working = false;
            spinner.IsVisible = spinner.IsRunning = false;

        }
    }
    protected void ScrollToTop()
    {
        if (bodyView is ScrollView scroll) _ = scroll.ScrollToAsync(0, 0, false);
    }
    protected virtual Task LoadAsync() => Task.CompletedTask;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ExecuteAsync(async () => { await Services.Session.InitializeAsync(); await LoadAsync(); });
    }
    protected async Task<bool> RequireLoginAsync()
    {
        await Services.Session.InitializeAsync();
        if (Services.Session.IsAuthenticated) return true;
        await Navigation.PushAsync(new LoginPage(Services));
        return false;
    }
    protected void AddRefresh()
    {
        ToolbarItems.Add(new ToolbarItem("Atualizar", null, async () => await ExecuteAsync(LoadAsync)));
    }
}
