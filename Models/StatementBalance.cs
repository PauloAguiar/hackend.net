using System.Text.Json.Serialization;

namespace HackEnd.Net.Models;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(StatementBalance))]
internal partial class StatementBalanceContext : JsonSerializerContext
{
}

public class StatementBalance
{
    public int total { get; set; }

    public int limite { get; set; }

    public string data_extrato { get; set; }
}