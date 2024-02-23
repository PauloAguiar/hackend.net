using System.Text.Json.Serialization;

namespace HackEnd.Net.Models;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(TransactionRequest))]
internal partial class TransactionRequestContext : JsonSerializerContext
{
}

public class TransactionRequest
{
    public int valor { get; set; }

    public char tipo { get; set; }

    public string descricao { get; set; }
}
