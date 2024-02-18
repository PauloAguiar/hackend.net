using System.Text.Json.Serialization;

namespace HackEnd.Net.Models;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(StatementTransaction))]
internal partial class StatementTransactionContext : JsonSerializerContext
{
}

public class StatementTransaction
{
    public int valor { get; set; }
    public char tipo { get; set; }
    public string descricao { get; set; }
    public DateTime realizada_em { get; set; }
}