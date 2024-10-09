﻿using alfabank;
using alfabank.Actions;
using alfabank.Exceptions;
using alfabank.Models;
using alfabank.Models.Response;
using alfabank.RestClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

IConfiguration appConfig = builder.Build();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();  // Добавляем логирование в консоль
    builder.SetMinimumLevel(LogLevel.Trace);
});
var logger = loggerFactory.CreateLogger<AlfabankRestClient<AlfaBankConfiguration>>();

string env = appConfig["ENVIRONMENT"]!;
AlfaBankConfiguration cfg = env switch
{
    "TEST" => new AlfaBankConfiguration(appConfig["AB_SERVER"]!)
    {
        Login = appConfig["AB_LOGIN"] ?? appConfig[$"{env}_AB_LOGIN"]!,
        Password = appConfig["AB_PASS"] ?? appConfig[$"{env}_AB_PASS"]!,
        Token = appConfig["AB_TOKEN"] ?? appConfig[$"{env}_AB_TOKEN"]!,
        Merchant = appConfig["AB_MERCHANT"] ?? appConfig[$"{env}_AB_MERCHANT"]!
    },
    "PROD" => new AlfaBankConfiguration()
    {
        Login = appConfig["AB_LOGIN"] ?? appConfig[$"{env}_AB_LOGIN"]!,
        Password = appConfig["AB_PASS"] ?? appConfig[$"{env}_AB_PASS"]!,
        Token = appConfig["AB_TOKEN"] ?? appConfig[$"{env}_AB_TOKEN"]!,
        Merchant = appConfig["AB_MERCHANT"] ?? appConfig[$"{env}_AB_MERCHANT"]!
    },
    _ => throw new NotImplementedException()
};

var client = new AlfabankRestClient(logger, cfg);

var orders = await GetOrders(client);

//var newOrder = await CreateOrder(client);

//var order = await GetOrderExtended(client, null, "");

//await RefundOrder(client, "");

Console.WriteLine("Exit");

static async Task<RegisterOrderResponse> CreateOrder(AlfabankRestClient client)
{
    //int orderId = 1;
    var req = new RegisterOrderAction()
    {
        OrderNumber = $"0-0001",
        Amount = 100,
        ReturnUrl = "",
        Email = "",
        Description = "Тестовый заказ созданный через API",
        DynamicCallbackUrl = ""
    };

    if (req.ValidateActionParams(out var errors))
    {
        try
        {
            var res = await client.CallActionAsync(req);
            Console.WriteLine($"{res.OrderId} - {res.FormUrl}");
            return res;
        }
        catch (AlfabankException abEx)
        {
            Console.WriteLine($"Error: {abEx.Message}");
            throw;
        }
    }

    throw new NotImplementedException();
}

static async Task<LastOrdersForMerchants> GetOrders(AlfabankRestClient client)
{
    var req = new GetLastOrdersForMerchantsAction()
    {
        Size = 100,
        FromDate = DateTime.Today.AddDays(-30),
        ToDate = DateTime.Today.AddDays(1),
        TransactionStates = TransactionState.ALL
    };

    if (req.ValidateActionParams(out var errors))
    {
        try
        {
            var res = await client.CallActionAsync(req);
            return res;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    throw new NotImplementedException();
};

static async Task<Order> GetOrderExtended(AlfabankRestClient client, string? orderId, string? orderNumber)
{
    var req = new GetOrderStatusExtendedAction()
    {
        OrderId = orderId,
        OrderNumber = orderNumber
    };

    try
    {
        var res = await client.CallActionAsync(req);
        return res;
    }
    catch (Exception ex)
    {
        throw;
    }
}

static async Task RefundOrder(AlfabankRestClient client, string orderId)
{
    var order = await GetOrderExtended(client, orderId, null);

    var req = new RefundAction()
    {
        OrderId = orderId,
        Amount = 5000
    };

    try
    {
        await client.CallActionAsync(req);
    }
    catch (Exception ex)
    {
        throw;
    }
}