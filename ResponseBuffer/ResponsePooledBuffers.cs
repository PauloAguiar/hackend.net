using HackEnd.Net.Models;

using Microsoft.IO;

using System.Text;

namespace HackEnd.Net.ResponseBuffer
{
    public class TransactionBuffer
    {
        private static readonly string JsonTemplate = @"{""saldo"": 00000000000, ""limite"": 00000000001}";

        public static int SaldoPosition = 10;
        public static int LimitePosition = 33;

        public static readonly byte[] BaseBuffer;

        static TransactionBuffer()
        {
            BaseBuffer = Encoding.UTF8.GetBytes(JsonTemplate);
        }
    }

    public class StatementBuffer
    {
        public struct BodyPart
        {
            public int ValorPosition;
            public int TipoPosition;
            public int DescPosition;
            public int RealizadaPosition;
        }

        private static readonly string JsonTemplateHeader = @"{""saldo"": {""total"": 00000000000,""limite"": 00000000000,""data_extrato"": ""00000000000000000000000000Z""},""ultimas_transacoes"": [";
        private static readonly string JsonTemplateBody = @"{""valor"": 00000000000,""tipo"": ""0"",""descricao"": ""00000000000000000000000000000000000000000,""realizada_em"": ""00000000000000000000000000Z""}";
        private static readonly string JsonTemplateFooter = @"]}";

        private static int HeaderLength = JsonTemplateHeader.Length;
        public static int TotalPosition = 20;
        public static int LimitePosition = 42;
        public static int DataPosition = 71;

        private static int BodyLength = JsonTemplateBody.Length + 1;
        private static int ValorOffset = 10;
        private static int TipoOffset = 31;
        private static int DescOffset = 48;
        private static int RealizadaOffset = 107;

        public static readonly BodyPart[] Parts = new BodyPart[10];

        public static readonly byte[] BaseBuffer;

        static StatementBuffer()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(JsonTemplateHeader);

            for(int i = 0; i < Parts.Length; i++)
            {
                sb.Append(JsonTemplateBody);
                if (i < Parts.Length - 1)
                {
                    sb.Append(',');
                }
                Parts[i] = new BodyPart()
                {
                    ValorPosition = HeaderLength + i * BodyLength + ValorOffset,
                    TipoPosition = HeaderLength + i * BodyLength + TipoOffset,
                    DescPosition = HeaderLength + i * BodyLength + DescOffset,
                    RealizadaPosition = HeaderLength + i * BodyLength + RealizadaOffset,
                };
            }
            sb.Append(JsonTemplateFooter);
            BaseBuffer = Encoding.UTF8.GetBytes(sb.ToString());
        }
    }

    public static class ResponsePooledBuffers
    {
        private static readonly RecyclableMemoryStreamManager managerTransactionBuffers = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options()
        {
            BlockSize = 45,
            AggressiveBufferReturn = true
        });

        private static readonly RecyclableMemoryStreamManager managerStatementBuffers = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options()
        {
            BlockSize = 1497,
            AggressiveBufferReturn = true,
        });

        public static Stream GetBalanceResponseStream(StatementResponse response)
        {
            var stream = managerStatementBuffers.GetStream(StatementBuffer.BaseBuffer);

            Span<byte> buffer = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            Span<byte> bufferDate = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            Span<byte> bufferDesc = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

            response.saldo.total.TryFormat(buffer, out var written);
            stream.Position = StatementBuffer.TotalPosition;
            stream.Write(buffer);

            stream.Position = StatementBuffer.TotalPosition + written;
            while (written < 11) {
                written++;
                stream.WriteByte(32);
            }

            response.saldo.limite.TryFormat(buffer, out written);
            stream.Position = StatementBuffer.LimitePosition;
            stream.Write(buffer);
            stream.Position = StatementBuffer.LimitePosition + written;
            while (written < 11)
            {
                written++;
                stream.WriteByte(32);
            }

            Encoding.UTF8.TryGetBytes(response.saldo.data_extrato, bufferDate, out written);

            stream.Position = StatementBuffer.DataPosition;
            stream.Write(bufferDate);

            int i = 0;
            foreach(var transaction in response.ultimas_transacoes)
            {
                transaction.valor.TryFormat(buffer, out written);

                stream.Position = StatementBuffer.Parts[i].ValorPosition;
                stream.Write(buffer);
                stream.Position = StatementBuffer.Parts[i].ValorPosition + written;
                while (written < 11)
                {
                    written++;
                    stream.WriteByte(32);
                }

                stream.Position = StatementBuffer.Parts[i].TipoPosition;
                stream.WriteByte((byte)transaction.tipo);

                Encoding.UTF8.TryGetBytes(transaction.descricao, bufferDesc, out written);
                stream.Position = StatementBuffer.Parts[i].DescPosition;
                stream.Write(bufferDesc);
                stream.Position = StatementBuffer.Parts[i].DescPosition + written;
                stream.WriteByte((byte)'"');

                while (written < 40)
                {
                    written++;
                    stream.WriteByte(32);
                }

                Encoding.UTF8.TryGetBytes(transaction.realizada_em, bufferDate, out written);
                stream.Position = StatementBuffer.Parts[i].RealizadaPosition;
                stream.Write(bufferDate);
                i++;
            }

            stream.Position = 0;
            return stream;
        }

        public static Stream GetTransactionResponseStream(TransactionResponse response)
        {
            var stream = managerTransactionBuffers.GetStream(TransactionBuffer.BaseBuffer);

            Span<byte> buffer1 = [32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32];
            response.saldo.TryFormat(buffer1, out var _);

            stream.Position = TransactionBuffer.SaldoPosition;
            stream.Write(buffer1);

            Span<byte> buffer2 = [32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32];
            response.limite.TryFormat(buffer2, out var _);

            stream.Position = TransactionBuffer.LimitePosition;
            stream.Write(buffer2);

            stream.Position = 0;
            return stream;
        }
    }
}

