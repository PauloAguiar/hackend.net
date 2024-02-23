using System.Text.Json.Serialization;

namespace HackEnd.Net.Models;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(StatementResponse))]
internal partial class StatementResponseContext : JsonSerializerContext
{
}

public class StatementResponse
{
    public StatementBalance saldo { get; set; }

    public IEnumerable<StatementTransaction> ultimas_transacoes { get; set; }
}