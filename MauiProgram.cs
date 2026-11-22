using Microsoft.Extensions.Logging;

namespace NextPage.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		Configuration.ApiConfiguration.Configure(DeviceInfo.Current.Platform == DevicePlatform.Android && DeviceInfo.Current.DeviceType == DeviceType.Virtual);
        Diagnostics.AppDiagnostics.Install();
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<Services.AppServices>();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if ANDROID
        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ScrollView, AndroidUi.GentleScrollViewHandler>());
        AndroidUi.AppInteraction.ConfigureMouse();
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
