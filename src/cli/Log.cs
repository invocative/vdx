using System.Diagnostics.CodeAnalysis;

namespace cli;

[ExcludeFromCodeCoverage]
public static class Log
{
    public static void Info(string s) => MarkupLine($"[aqua]INFO[/]: {s}");
    public static void Warn(string s) => MarkupLine($"[orange3]WARN[/]: {s}");
    public static void Error(string s) => MarkupLine($"[red]ERROR[/]: {s}");
    public static void Error(Exception s) => WriteException(s);
}