using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AcquiringBank.Service;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using Microsoft.AspNetCore.Hosting;
using PaymentGateway.Api.Models.Requests;
using System.Text.Json;
using System.Text;
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<PaymentsController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                    {"AcquiringBank:ApiUrl", "http://localhost:8080/payments"}
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing registrations
            var descriptors = services.Where(
                d => d.ServiceType == typeof(IPaymentsRepository) ||
                     d.ServiceType == typeof(IAcquiringBankWrapper)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add mock IPaymentsRepository
            services.AddSingleton<IPaymentsRepository>(new PaymentsRepository());

            // Add mock IAcquiringBankWrapper
            services.AddSingleton<IAcquiringBankWrapper>(new MockAcquiringBankWrapper());
        });
    }
}

public class MockAcquiringBankWrapper : IAcquiringBankWrapper
{
    public Task<AcquiringBankPaymentResponse> MakePaymentAsync(AcquiringBankPaymentRequest request)
    {
        switch (request.CardNumber)
        {
            case "1000000000000001":
                return Task.FromResult(new AcquiringBankPaymentResponse
                {
                    Authorized = true,
                    AuthorizationCode = "Test-Authorisation-Code"
                });
            case "1000000000000002":
                return Task.FromResult(new AcquiringBankPaymentResponse
                {
                    Authorized = false
                });
            case "1000000000000003": // Test card number for forcing an exception
                {
                    throw new Exception("Simulated server error");
                }
            default:
                return Task.FromResult(new AcquiringBankPaymentResponse
                {
                    Authorized = false,
                    ErrorMessage = "Acquiring Bank Error"
                });
        }

    }
}

public class PaymentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly Random _random = new();
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP"
        };

        var paymentsRepository = _factory.Services.GetRequiredService<IPaymentsRepository>();
        paymentsRepository.Add(payment);

        // Act
        var response = await _client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        if (paymentResponse != null)
        {
            Assert.Equal(payment.Id, paymentResponse.Id);
            Assert.Equal(payment.ExpiryYear, paymentResponse.ExpiryYear);
            Assert.Equal(payment.ExpiryMonth, paymentResponse.ExpiryMonth);
            Assert.Equal(payment.Amount, paymentResponse.Amount);
            Assert.Equal(payment.CardNumberLastFour, paymentResponse.CardNumberLastFour);
            Assert.Equal(payment.Currency, paymentResponse.Currency);
        }
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SuccesfulPaymentExecution()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = 2030,
            ExpiryMonth = 11,
            Amount = 100,
            CardNumber = "1000000000000001", //success mock
            Currency = "GBP",
            Cvv = 100
        };

        var content = new StringContent(JsonSerializer.Serialize(payment), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/Payments", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PostPaymentResponse>(responseContent);

        Assert.NotNull(result);
        if (result != null)
        {
            Assert.Equal(PaymentStatus.Authorized, result.Status);
            Assert.Equal(payment.ExpiryYear, result.ExpiryYear);
            Assert.Equal(payment.ExpiryMonth, result.ExpiryMonth);
            Assert.Equal(payment.Amount, result.Amount);
            var expectedCardNumber = new string(payment.CardNumber.TakeLast(4).ToArray());
            Assert.Equal(expectedCardNumber, result.CardNumberLastFour);
            Assert.Equal(payment.Currency, result.Currency);
        }
    }

    [Fact]
    public async Task DeclinedPaymentExecution()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = 2030,
            ExpiryMonth = 11,
            Amount = 100,
            CardNumber = "1000000000000002", //declined mock
            Currency = "GBP",
            Cvv = 100
        };

        var content = new StringContent(JsonSerializer.Serialize(payment), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/Payments", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PostPaymentResponse>(responseContent);

        Assert.NotNull(result);
        if (result != null)
        {
            Assert.Equal(PaymentStatus.Declined, result.Status);
            Assert.Equal(payment.ExpiryYear, result.ExpiryYear);
            Assert.Equal(payment.ExpiryMonth, result.ExpiryMonth);
            Assert.Equal(payment.Amount, result.Amount);
            var expectedCardNumber = new string(payment.CardNumber.TakeLast(4).ToArray());
            Assert.Equal(expectedCardNumber, result.CardNumberLastFour);
            Assert.Equal(payment.Currency, result.Currency);
        }
    }

    [Fact]
    public async Task BankErrorPaymentExecution()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = 2030,
            ExpiryMonth = 11,
            Amount = 100,
            CardNumber = "1000000000000004", //bank error mock
            Currency = "GBP",
            Cvv = 100
        };

        var content = new StringContent(JsonSerializer.Serialize(payment), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/Payments", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PostPaymentResponse>(responseContent);

        Assert.NotNull(result);
        if (result != null)
        {
            Assert.Equal(PaymentStatus.BankError, result.Status);
            Assert.Equal(payment.ExpiryYear, result.ExpiryYear);
            Assert.Equal(payment.ExpiryMonth, result.ExpiryMonth);
            Assert.Equal(payment.Amount, result.Amount);
            var expectedCardNumber = new string(payment.CardNumber.TakeLast(4).ToArray());
            Assert.Equal(expectedCardNumber, result.CardNumberLastFour);
            Assert.Equal(payment.Currency, result.Currency);
        }
    }

    [Fact]
    public async Task InvalidRequestPayment()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = 2030,
            ExpiryMonth = 11,
            Amount = 100,
            CardNumber = "1", //invalid request
            Currency = "GBP",
            Cvv = 100
        };

        var content = new StringContent(JsonSerializer.Serialize(payment), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/Payments", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // var responseContent = await response.Content.ReadAsStringAsync();
        // var result = JsonSerializer.Deserialize<PostPaymentResponse>(responseContent);

        // Assert.NotNull(result);
        // if (result != null)
        // {
        //     Assert.Equal(PaymentStatus.Rejected, result.Status);
        //     Assert.Equal(payment.ExpiryYear, result.ExpiryYear);
        //     Assert.Equal(payment.ExpiryMonth, result.ExpiryMonth);
        //     Assert.Equal(payment.Amount, result.Amount);
        //     var expectedCardNumber = new string(payment.CardNumber.TakeLast(4).ToArray());
        //     Assert.Equal(expectedCardNumber, result.CardNumberLastFour);
        //     Assert.Equal(payment.Currency, result.Currency);
        // }
    }

    [Fact]
    public async Task GatewayErrorPaymentExecution()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = 2030,
            ExpiryMonth = 11,
            Amount = 100,
            CardNumber = "1000000000000003", //simulate server error
            Currency = "GBP",
            Cvv = 100
        };

        var content = new StringContent(JsonSerializer.Serialize(payment), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/Payments", content);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PostPaymentResponse>(responseContent);

        Assert.NotNull(result);
        if (result != null)
        {
            Assert.Equal(PaymentStatus.GatewayError, result.Status);
            Assert.Equal(payment.ExpiryYear, result.ExpiryYear);
            Assert.Equal(payment.ExpiryMonth, result.ExpiryMonth);
            Assert.Equal(payment.Amount, result.Amount);
            var expectedCardNumber = new string(payment.CardNumber.TakeLast(4).ToArray());
            Assert.Equal(expectedCardNumber, result.CardNumberLastFour);
            Assert.Equal(payment.Currency, result.Currency);
        }
    }
}