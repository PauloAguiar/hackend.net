using HackEnd.Net;
using HackEnd.Net.Models;

using Microsoft.AspNetCore.Mvc;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

var database = new DatabaseService("Host=db;Port=5432;Database=rinha;User Id=admin;Password=password;Include Error Detail=true;");

await database.InitializeAsync();

app.MapPost("/clientes/{id}/transacoes", async ([FromRoute] int id, HttpRequest req) =>
{
    TransactionRequest? transactionReq;
    try
    {
        transactionReq = await JsonSerializer.DeserializeAsync(req.Body, TransactionRequestContext.Default.TransactionRequest);
    }
    catch (JsonException jsonEx)
    {
        return Results.BadRequest();
    }

    if (!TransactionIsValid(transactionReq))
    {
        return Results.BadRequest();
    }
    var result = await database.CreateTransaction(id, transactionReq);

    return result.ResultCode switch
    {
        0 => Results.Json(result.Response!, TransactionResponseContext.Default.TransactionResponse),
        1 => Results.BadRequest(),
        2 => Results.UnprocessableEntity(),
        _ => Results.Problem(detail: "Invalid database result", statusCode: StatusCodes.Status500InternalServerError)
    };

    static bool TransactionIsValid([NotNullWhen(true)] TransactionRequest? transactionReq)
    {
        if (transactionReq == null)
        {
            return false;
        }

        if (transactionReq.valor < 1)
        {
            return false;
        }

        if (string.IsNullOrEmpty(transactionReq.descricao) || transactionReq.descricao.Length > 10)
        {
            return false;
        }

        if (transactionReq.tipo != 'd' && transactionReq.tipo != 'c')
        {
            return false;
        }

        return true;
    }
});

app.MapGet("/clientes/{id}/extrato", async ([FromRoute] int id) =>
{
    var result = await database.GetStatement(id);

    return result.ResultCode switch
    {
        0 => Results.Json(result.Response!, StatementResponseContext.Default.StatementResponse),
        1 => Results.BadRequest(),
        _ => Results.Problem(detail: "Invalid database result", statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.Run();