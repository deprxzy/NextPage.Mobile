using NextPage.Mobile.Services;

namespace NextPage.Mobile;

public partial class App : Application
{
    private readonly AppServices services;
    public App(AppServices services)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        this.services = services;
    }
    protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell(services));
}
