using System.Diagnostics;
using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;

throw new NotImplementedException();

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
    return destination + "/VSCode-linux-x64" + "/data";
}

static string GetVscExePath(string destination)
{
    return destination + "/VSCode-linux-x64" + "/bin" + "/code";
}

static (string, string) GetVscLocale()
{
    return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "fr" => ("fr", "ms-ceintl.vscode-language-pack-fr"),
        "it" => ("it", "ms-ceintl.vscode-language-pack-it"),
        "de" => ("de", "ms-ceintl.vscode-language-pack-de"),
        "es" => ("es", "ms-ceintl.vscode-language-pack-es"),
        "ru" => ("ru", "ms-ceintl.vscode-language-pack-ru"),
        "zh" => ("zh-cn", "ms-ceintl.vscode-language-pack-zh-hans"),
        "ja" => ("ja", "ms-ceintl.vscode-language-pack-ja"),
        "ko" => ("ko", "ms-ceintl.vscode-language-pack-ko"),
        "cs" => ("cs", "ms-ceintl.vscode-language-pack-cs"),
        "pt" => ("pt-br", "ms-ceintl.vscode-language-pack-pt-br"),
        "tr" => ("tr", "ms-ceintl.vscode-language-pack-tr"),
        "pl" => ("pl", "ms-ceintl.vscode-language-pack-pl"),
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

    var argv = JsonSerializer.Serialize(new
    {
        Locale = locale,
        EnableCrashReporter = false
    }, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
        WriteIndented = true
    });
    await File.WriteAllTextAsync(dataPath + "/argv.json", argv);
}
