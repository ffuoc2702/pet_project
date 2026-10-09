using System.Globalization;
using System.Text.RegularExpressions;

namespace MatchingSports.DemoSimulator;

public sealed record DemoOptions(int Users = 50, int Teams = 6, int Listings = 15,
    string RunId = "cv-demo-01", bool TestMode = false, string OutputDirectory = ".artifacts/demo-simulator")
{
    public void Validate()
    {
        if (Users is < 16 or > 200 || Teams is < 2 or > 20 || Users < Teams * 5 + 6)
            throw new DemoSimulationException("Users phải từ 16–200; Teams từ 2–20; cần 5 người/team và ít nhất 6 người vãng lai.");
        if (Listings is < 8 or > 50) throw new DemoSimulationException("Listings phải từ 8–50.");
        if (!Regex.IsMatch(RunId, "^[a-z0-9][a-z0-9-]{0,31}$", RegexOptions.CultureInvariant))
            throw new DemoSimulationException("RunId gồm 1–32 ký tự thường, số và dấu gạch ngang.");
        if (string.IsNullOrWhiteSpace(OutputDirectory)) throw new DemoSimulationException("Output directory không được trống.");
    }

    public static DemoOptions Parse(string[] args)
    {
        var options = new DemoOptions();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !seen.Add(args[index])) throw new DemoSimulationException("Tham số thiếu giá trị hoặc bị lặp.");
            var value = args[index + 1];
            options = args[index] switch
            {
                "--users" => options with { Users = Number(value) },
                "--teams" => options with { Teams = Number(value) },
                "--listings" => options with { Listings = Number(value) },
                "--run-id" => options with { RunId = value },
                "--mode" when value is "demo" or "test" => options with { TestMode = value == "test" },
                "--output" => options with { OutputDirectory = value },
                _ => throw new DemoSimulationException("Tham số không hợp lệ; dùng --help để xem cú pháp.")
            };
        }
        options.Validate();
        return options;
    }

    private static int Number(string value) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
        ? number : throw new DemoSimulationException("Tham số số lượng phải là số nguyên dương.");
}

public sealed class DemoSimulationException(string message) : Exception(message);
