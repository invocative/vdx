using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace cli;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Invocative.VDX;
using Spectre.Console;
using ValidationResult = Spectre.Console.ValidationResult;

[ExcludeFromCodeCoverage]
public class PackageCommand : Command<PackageSettings>
{
    public override int Execute(CommandContext context, PackageSettings settings)
    {
        var outputFolder = new DirectoryInfo(settings.OutputFolder ?? "./content");
        var inputFolder = new DirectoryInfo(settings.InputFolder);

        
        if (!inputFolder.Exists)
        {
            Log.Error($"Folder [orange3]'{inputFolder.FullName}'[/] is not exist.");
            return -1;
        }


        var r = new VdxAssembler(inputFolder, outputFolder, settings.GameName ?? "generic-game");


        r.SetLogActions((x) => MarkupLine(x), (x) => MarkupLine(x), (x) => MarkupLine(x));

        var files = inputFolder.GetFiles("*.*", SearchOption.AllDirectories);

        foreach (var i in files.Where(x => VDXConstants.TEXTURE_EXT.Contains(x.Extension)))
            r.AddTexture(i);
        foreach (var i in files.Where(x => VDXConstants.MODEL_EXT.Contains(x.Extension)))
            r.AddModel(i);
        foreach (var i in files.Where(x => VDXConstants.AUDIO_EXT.Contains(x.Extension)))
            r.AddAudio(i);

        AnsiConsole.Status()
            .Start("Packing...", ctx => 
            {
                ctx.SpinnerStyle(Style.Parse("orange3"));
                ctx.Spinner(Spinner.Known.Dots8Bit);
                ctx.Status("Sealing...");
                Thread.Sleep(500);
                r.Seal();
        
                ctx.Status("Flushing...");
                Thread.Sleep(500);
                r.FlushToDisk();
            });

        return 0;
    }
}


[ExcludeFromCodeCoverage]
public class PackageSettings : CommandSettings
{
    [Description("Path to input folder")]
    [Required]
    [CommandArgument(0, "[INPUT FOLDER]")]
    public string InputFolder { get; set; }

    [Description("Display exported types table")]
    [CommandOption("--output|-o")]
    public string OutputFolder { get; set; }

    [Description("Game Name")]
    [CommandOption("--game|-g")]
    public string GameName { get; set; }
}
