namespace NextPage.Mobile.Configuration;

public static class ApiConfiguration
{
    public static Uri BaseAddress { get; private set; } = new("http://127.0.0.1:5177/");
    public static void Configure(bool androidEmulator) =>
        BaseAddress = new(androidEmulator ? "http://10.0.2.2:5177/" : "http://127.0.0.1:5177/");
    public static string ImageUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps)) return absolute.ToString();
        return new Uri(BaseAddress, path.TrimStart('/')).ToString();
    }
}
