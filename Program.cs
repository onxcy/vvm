using System.Diagnostics;
using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

if (!OperatingSystem.IsLinux())
{
    throw new NotImplementedException();
}

var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var destination = Path.Join(userProfile, ".vvm");

if (args.Length == 1 && args[0] == "install")
{
    Directory.CreateDirectory(destination);
    Console.WriteLine("Installing to " + destination);
    await DownloadAndPrepareVsc("latest", destination);
}
else if (args.Length == 1 && args[0] == "start")
{
    var exePath = GetVscExePath(destination);
    Process.Start(exePath);
}
else if (args.Length == 1 && args[0] == "uninstall")
{
    Directory.Delete(destination, true);
}
else
{
    Console.WriteLine("usage: vvm COMMAND");
    Console.WriteLine("Subcommands:");
    Console.WriteLine("install      Install Visual Studio Code");
    Console.WriteLine("start        Start Visual Studio Code");
    Console.WriteLine("uninstall    Uninstall Visual Studio Code");
}
return;

static string GetVscDownloadUri(string version)
{
    return $"https://update.code.visualstudio.com/{version}/linux-x64/stable";
}

static async Task DownloadVsc(string version, string destination)
{
    var httpClient = new HttpClient();

    var uri = GetVscDownloadUri(version);
    var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

    response.EnsureSuccessStatusCode();

    var tarGzStream = await response.Content.ReadAsStreamAsync();
    var tarStream = new GZipStream(tarGzStream, CompressionMode.Decompress);
    await TarFile.ExtractToDirectoryAsync(tarStream, destination, false);
}

static string GetVscDataPath(string destination)
{
    return Path.Join(destination, "VSCode-linux-x64", "data");
}

static string GetVscExePath(string destination)
{
    return Path.Join(destination, "VSCode-linux-x64", "bin", "code");
}

static (string, string) GetVscLocale()
{
    return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "fr" => ("fr", "MS-CEINTL.vscode-language-pack-fr"),
        "it" => ("it", "MS-CEINTL.vscode-language-pack-it"),
        "de" => ("de", "MS-CEINTL.vscode-language-pack-de"),
        "es" => ("es", "MS-CEINTL.vscode-language-pack-es"),
        "ru" => ("ru", "MS-CEINTL.vscode-language-pack-ru"),
        "zh" => ("zh-cn", "MS-CEINTL.vscode-language-pack-zh-hans"),
        "ja" => ("ja", "MS-CEINTL.vscode-language-pack-ja"),
        "ko" => ("ko", "MS-CEINTL.vscode-language-pack-ko"),
        "cs" => ("cs", "MS-CEINTL.vscode-language-pack-cs"),
        "pt" => ("pt-br", "MS-CEINTL.vscode-language-pack-pt-BR"),
        "tr" => ("tr", "MS-CEINTL.vscode-language-pack-tr"),
        "pl" => ("pl", "MS-CEINTL.vscode-language-pack-pl"),
        _ => throw new NotImplementedException()
    };
}

static async Task DownloadAndPrepareVsc(string version, string destination)
{
    await DownloadVsc(version, destination);

    var dataPath = GetVscDataPath(destination);
    Directory.CreateDirectory(dataPath);

    var (locale, extensionName) = GetVscLocale();

    var exePath = GetVscExePath(destination);
    await Process.Start(exePath, "--install-extension " + extensionName).WaitForExitAsync();

    var jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    var argv = JsonSerializer.Serialize(new VscArgv(locale), jsonSerializerOptions);
    await File.WriteAllTextAsync(Path.Join(dataPath, "argv.json"), argv);

    var settings = JsonSerializer.Serialize(new VscSettings(), jsonSerializerOptions);
    await File.WriteAllTextAsync(Path.Join(dataPath, "user-data", "User", "settings.json"), settings);
}

class VscArgv(string locale)
{
    [JsonPropertyName("locale")]
    public string P1 { get; } = locale;
    [JsonPropertyName("enable-crash-reporter")]
    public bool P2 { get; } = false;
}

class VscSettings()
{
    [JsonPropertyName("security.workspace.trust.enabled")]
    public bool P1 { get; } = false;
    [JsonPropertyName("telemetry.telemetryLevel")]
    public string P2 { get; } = "off";
    [JsonPropertyName("update.mode")]
    public string P3 { get; } = "none";
    [JsonPropertyName("window.titleBarStyle")]
    public string P4 { get; } = "custom";
    [JsonPropertyName("workbench.enableExperiments")]
    public bool P5 { get; } = false;
}
