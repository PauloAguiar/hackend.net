using HackEnd.Net.Models;

using Npgsql;

using System.Collections.Concurrent;

namespace HackEnd.Net
{
    public class DatabaseService
    {
        private readonly string connectionString;
        private readonly NpgsqlDataSource dataSource;
        private readonly ConcurrentDictionary<int, int> transactionCounters = new();

        public DatabaseService(string connectionString)
        {
            this.connectionString = connectionString;
            this.dataSource = new NpgsqlSlimDataSourceBuilder(connectionString).Build();
        }

        public record struct CreateTransactionResult(int ResultCode, TransactionResponse? Response);

        private async Task Prune(int clientId)
        {
            using var connection = await this.dataSource.OpenConnectionAsync();

            using var cmd = new NpgsqlCommand("CALL prune_transactions(@client_id);", connection);
            cmd.Parameters.AddWithValue("client_id", clientId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<CreateTransactionResult> CreateTransaction(int id, TransactionRequest transaction)
        {
            using var connection = await this.dataSource.OpenConnectionAsync();

            using var cmd = new NpgsqlCommand("SELECT * FROM create_transaction_for_client(@client_id, @transaction_value, @transaction_type, @transaction_description);", connection);

            var storedValue = transaction.tipo == 'c' ? transaction.valor : -1 * transaction.valor;
            cmd.Parameters.AddWithValue("client_id", id);
            cmd.Parameters.AddWithValue("transaction_value", storedValue);
            cmd.Parameters.AddWithValue("transaction_type", transaction.tipo);
            cmd.Parameters.AddWithValue("transaction_description", transaction.descricao);

            cmd.Parameters.AddWithValue("result_code", 0);
            cmd.Parameters.AddWithValue("out_client_limit", 0);
            cmd.Parameters.AddWithValue("out_client_current", 0);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                int transactionCount = transactionCounters.AddOrUpdate(id, 1, (key, oldValue) => oldValue + 1);
                if (transactionCount % 5 == 0)
                {
                    _ = Task.Run(async () => await Prune(id));
                }

                if (await reader.ReadAsync())
                {
                    int resultCode = reader.GetInt32(reader.GetOrdinal("result_code"));
                    if (resultCode > 0)
                    {
                        return new CreateTransactionResult(resultCode, null);
                    }

                    int clientLimit = reader.GetInt32(reader.GetOrdinal("out_client_limit"));
                    int clientCurrent = reader.GetInt32(reader.GetOrdinal("out_client_current"));
                    return new CreateTransactionResult(resultCode, new TransactionResponse() { limite = clientLimit, saldo = clientCurrent });
                }
            }

            throw new InvalidOperationException("Should never happen");
        }

        public record struct GetStatementResult(int ResultCode, StatementResponse? Response);

        public async Task<GetStatementResult> GetStatement(int id)
        {
            using var connection = await this.dataSource.OpenConnectionAsync();

            using var cmd = new NpgsqlCommand("SELECT * FROM get_client_transactions(@p_client_id)", connection);
            cmd.Parameters.AddWithValue("@p_client_id", id);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    return new GetStatementResult(1, null);
                }

                var result = new StatementResponse
                {
                    saldo = new StatementBalance
                    {
                        data_extrato = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"),
                        limite = reader.GetInt32(0),
                        total = reader.GetInt32(1)
                    },
                    ultimas_transacoes = new List<StatementTransaction>()
                };

                var transactions = new List<StatementTransaction>();

                int count = 10;
                do
                {

                    if (!reader.IsDBNull(2))
                    {
                        var tipo = reader.GetString(3)[0];
                        if (tipo == 'c' || tipo == 'd')
                        {
                            transactions.Add(new StatementTransaction
                            {
                                valor = Math.Abs(reader.GetInt32(2)),
                                tipo = tipo,
                                descricao = reader.GetString(4),
                                realizada_em = reader.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss.ffffff")
                            });
                            count--;
                        }
                    }
                }
                while (await reader.ReadAsync() && count > 0);

                result.ultimas_transacoes = transactions;

                return new GetStatementResult(0, result);
            }
        }

        public async Task Wipe()
        {
            using var connection = await this.dataSource.OpenConnectionAsync();

            using var cmd = new NpgsqlCommand("SELECT wipe_all_transactions();", connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
