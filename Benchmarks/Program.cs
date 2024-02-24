using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Net;
using System.Text;

BenchmarkRunner.Run<ApiCallBenchmark>();

[ExecutionValidator]
public class ApiCallBenchmark
{
    private readonly HttpClient client;
    private readonly HttpContent content;
    private readonly Uri requestUri;
    private readonly Uri requestUri2;
    private readonly Uri requestUri3;

    public ApiCallBenchmark()
    {
        client = new HttpClient();

        this.requestUri = new Uri("http://nginx:10002/clientes/4/transacoes");
        this.requestUri2 = new Uri("http://ph-nginx:10000/clientes/4/transacoes");
        this.requestUri3 = new Uri("http://tomer-nginx:9999/clientes/4/transacoes");
        string jsonContent = "{\"valor\":1,\"tipo\":\"d\",\"descricao\":\"devolve\"\r\n}";
        content = new StringContent(jsonContent, Encoding.UTF8,  "application/json");
    }

    public async Task<bool> ValidateApiEndpointsAsync()
    {
        // Perform a simple validation request or any setup necessary
        // This is just a conceptual illustration; adapt as needed for actual validation
        var response = await client.PostAsync(requestUri, content);
        var response2 = await client.PostAsync(requestUri2, content);
        var response3 = await client.PostAsync(requestUri3, content);
        return response.StatusCode == HttpStatusCode.OK && response2.StatusCode == HttpStatusCode.OK && response3.StatusCode == HttpStatusCode.OK;
    }

    [GlobalSetup]
    public async Task SetupAsync()
    {
        //await client.GetAsync("http://ph-nginx:10000/wipe");
    }

    //[Benchmark]
    public async Task Goku()
    {
        // var response = await client.PostAsync(requestUri, content);
        var response = await client.GetAsync(requestUri);
    }

    [Benchmark]
    public async Task Doente()
    {
        var response = await client.PostAsync(requestUri2, content);
        // var response = await client.GetAsync(requestUri2);
    }

    //[Benchmark]
    public async Task Tomer()
    {
        // var response = await client.PostAsync(requestUri3, content);
        var response = await client.GetAsync(requestUri3);
    }
}
