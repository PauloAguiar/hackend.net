using HackEnd.Net.Models;

using Npgsql;

namespace HackEnd.Net
{
    public class DatabaseService
    {
        private readonly string connectionString;

        public DatabaseService(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task InitializeAsync()
        {
            
        }

        public record struct CreateTransactionResult(int ResultCode, TransactionResponse? Response);

        public async Task<CreateTransactionResult> CreateTransaction(int id, TransactionRequest transaction)
        {
            using var connection = new NpgsqlConnection(this.connectionString);
            await connection.OpenAsync();

            string commandText = "SELECT * FROM create_transaction_for_client(@client_id, @transaction_value, @transaction_type, @transaction_description, @transaction_date);";

            using var cmd = new NpgsqlCommand(commandText, connection);

            var storedValue = transaction.tipo == 'c' ? transaction.valor : -1 * transaction.valor;
            cmd.Parameters.AddWithValue("client_id", id);
            cmd.Parameters.AddWithValue("transaction_value", storedValue);
            cmd.Parameters.AddWithValue("transaction_type", transaction.tipo);
            cmd.Parameters.AddWithValue("transaction_description", transaction.descricao);
            cmd.Parameters.AddWithValue("transaction_date", DateTime.Now);

            cmd.Parameters.AddWithValue("result_code", 0); // Initial value, will be overwritten by function output
            cmd.Parameters.AddWithValue("out_client_limit", 0); // Placeholder, will be set by function
            cmd.Parameters.AddWithValue("out_client_current", 0); // Placeholder, will be set by function

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    int resultCode = reader.GetInt32(reader.GetOrdinal("result_code"));
                    int clientLimit = reader.GetInt32(reader.GetOrdinal("out_client_limit"));
                    int clientCurrent = reader.GetInt32(reader.GetOrdinal("out_client_current"));

                    return new CreateTransactionResult(resultCode, resultCode == 0 ? new TransactionResponse() { limite = clientLimit, saldo = clientCurrent } : null);
                }
            }

            throw new InvalidOperationException("Should never happen");
        }

        public record struct GetStatementResult(int ResultCode, StatementResponse? Response);

        public async Task<GetStatementResult> GetStatement(int id)
        {
            using var connection = new NpgsqlConnection(this.connectionString);
            await connection.OpenAsync();

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
                        data_extrato = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                        limite = reader.GetInt32(0),
                        total = reader.GetInt32(1)
                    },
                    ultimas_transacoes = new List<StatementTransaction>()
                };

                var transactions = new List<StatementTransaction>();

                do
                {
                    if (!reader.IsDBNull(2))
                    {
                        transactions.Add(new StatementTransaction
                        {
                            valor = reader.GetInt32(2),
                            tipo = reader.GetString(3)[0],
                            descricao = reader.GetString(4),
                            realizada_em = reader.GetDateTime(5)
                        });
                    }
                }
                while (await reader.ReadAsync());

                result.ultimas_transacoes = transactions;

                return new  GetStatementResult(0, result);
            }
        }
    }
}
