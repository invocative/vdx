using cli;
using Spectre.Console.Cli;
using static version.GlobalVersion;
var watch = Stopwatch.StartNew();


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    OutputEncoding = Encoding.Unicode;
JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore,
    Formatting = Formatting.Indented,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    Culture = CultureInfo.InvariantCulture,
    Converters = new List<JsonConverter>()
    {
        new StringEnumConverter()
    }
};


MarkupLine($"[grey]VDX Package Assembler[/] [red]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");
MarkupLine($"[grey]Copyright (C)[/] [cyan3]2022[/] [bold]Yuuki Wesp[/].\n\n");



var app = new CommandApp();


app.Configure(config =>
{
    config.AddCommand<PackageCommand>("package")
        .WithDescription("Prepare and build content folder.");
});


var result = app.Run(args);

watch.Stop();

MarkupLine($":sparkles: Done in [lime]{watch.Elapsed.TotalSeconds:00.000}s[/].");

return result;