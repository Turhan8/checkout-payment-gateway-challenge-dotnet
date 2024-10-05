using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcquiringBank.Service.Api.Tests;

public class AcquiringBankWrapperIntegrationTests
{
    private readonly IAcquiringBankWrapper _acquiringBank;

    private (IHttpClientFactory, IConfiguration) CreateDependencies()
    {
        // Create a new service collection
        var services = new ServiceCollection();

        // Register the necessary services for IHttpClientFactory
        services.AddHttpClient();

        // Create and configure IConfiguration
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        // Register IConfiguration
        services.AddSingleton<IConfiguration>(configuration);

        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();

        // Resolve IHttpClientFactory and IConfiguration from the service provider
        return (
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            serviceProvider.GetRequiredService<IConfiguration>()
        );
    }

    public AcquiringBankWrapperIntegrationTests()
    {
        Environment.SetEnvironmentVariable("AcquiringBank__ApiUrl", "http://localhost:8080/payments");
        var (httpClientFactory, configuration) = CreateDependencies();
        _acquiringBank = new AcquiringBankWrapper(httpClientFactory, configuration);
    }

    [Fact]
    public async Task AcquiringBankSuccessResult()
    {
        // Arrange
        var payment = new AcquiringBankPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryDate = "04/2025",
            Currency = "GBP",
            Amount = 100,
            Cvv = 123,
        };

        // Act
        AcquiringBankPaymentResponse result = await _acquiringBank.MakePaymentAsync(payment);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Authorized);
        Assert.Equal("", result.ErrorMessage);
    }

    [Fact]
    public async Task AcquiringBankDeclineResult()
    {
        // Arrange
        var payment = new AcquiringBankPaymentRequest
        {
            CardNumber = "2222405343248112",
            ExpiryDate = "01/2026",
            Currency = "USD",
            Amount = 60000,
            Cvv = 456,
        };

        // Act
        AcquiringBankPaymentResponse result = await _acquiringBank.MakePaymentAsync(payment);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Authorized);
        Assert.Equal("", result.ErrorMessage);
    }

    [Fact]
    public async Task AcquiringBankServerErrorResult()
    {
        // Arrange
        var payment = new AcquiringBankPaymentRequest
        {
            CardNumber = "2000000000000002",
            ExpiryDate = "01/2026",
            Currency = "GBP",
            Amount = 60000,
            Cvv = 456,
        };

        // Act
        AcquiringBankPaymentResponse result = await _acquiringBank.MakePaymentAsync(payment);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Authorized);
        Assert.Equal("AcquiringBank server error", result.ErrorMessage);        
    }

    [Fact]
    public async Task AcquiringBankPayentValidationErrorResult()
    {
        // Arrange
        var payment = new AcquiringBankPaymentRequest
        {
            CardNumber = "2222405343248112",
            ExpiryDate = "01/2026",
            Currency = "AUD",
            Amount = 60000,
            Cvv = 456,
        };

        // Act
        AcquiringBankPaymentResponse result = await _acquiringBank.MakePaymentAsync(payment);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Authorized);
        Assert.Equal("Invalid payment request", result.ErrorMessage);
    }
}