using System.Text.Json.Serialization;

namespace HackEnd.Net.Models;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(TransactionResponse))]
internal partial class TransactionResponseContext : JsonSerializerContext
{
}

public class TransactionResponse
{
    public int saldo { get; set; }

    public int limite { get; set; }
}