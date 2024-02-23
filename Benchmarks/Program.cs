// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using System.Text;

BenchmarkRunner.Run<ApiCallBenchmark>();

public class ApiCallBenchmark
{
    private readonly HttpClient client;
    private readonly HttpContent content;
    private readonly Uri requestUri;
    private readonly Uri requestUri2;

    public ApiCallBenchmark()
    {
        client = new HttpClient();
        this.requestUri = new Uri("http://nginx:9999/clientes/2/transacoes");
        this.requestUri2 = new Uri("http://nginx:9999/clientes/2/transacoesbuffer");
        string jsonContent = "{\"valor\":1,\"tipo\":\"d\",\"descricao\":\"devolve\"\r\n}";
        content = new StringContent(jsonContent, Encoding.UTF8,  "application/json");
    }

    [Benchmark]
    public async Task PostTransacoesSystemJsonAsync()
    {
        var response = await client.PostAsync(requestUri, content);
    }

    [Benchmark]
    public async Task PostTransacoesBufferAsync()
    {
        var response = await client.PostAsync(requestUri2, content);
    }
}
