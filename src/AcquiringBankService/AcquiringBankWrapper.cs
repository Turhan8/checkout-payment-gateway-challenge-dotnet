using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AcquiringBank.Service;

public interface IAcquiringBankWrapper
{
    Task<AcquiringBankPaymentResponse> MakePaymentAsync(AcquiringBankPaymentRequest request);
}

public class AcquiringBankWrapper : IAcquiringBankWrapper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiUrl;

    public AcquiringBankWrapper(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiUrl = configuration["AcquiringBank:ApiUrl"] ?? throw new InvalidOperationException("AcquiringBank:ApiUrl is not configured");
    }

    public async Task<AcquiringBankPaymentResponse> MakePaymentAsync(AcquiringBankPaymentRequest request)
    {
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            Console.WriteLine($"AcquirinBank server error: {string.Join("\n", validationResults)}");
            return new AcquiringBankPaymentResponse { Authorized = false, ErrorMessage = "Invalid payment request" };
        }

        try
        {
            var client = _httpClientFactory.CreateClient();

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await client.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode(); // Throws if the status code is not successful

            string responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AcquiringBankPaymentResponse>(responseBody);

            return result ?? new AcquiringBankPaymentResponse { Authorized = false };
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"AcquiringBank server error: {e.Message}");
            return new AcquiringBankPaymentResponse { Authorized = false, ErrorMessage = "AcquiringBank server error" };
        }
        catch (JsonException e)
        {
            // Handle JSON deserialization errors
            Console.WriteLine($"JSON error: {e.Message}");
            return new AcquiringBankPaymentResponse { Authorized = false, ErrorMessage = "JSON error" };
        }
        catch (Exception e)
        {
            // Handle any other exceptions that might occur
            Console.WriteLine($"An unexpected error occurred: {e.Message}");
            return new AcquiringBankPaymentResponse { Authorized = false, ErrorMessage = "An unexpected error occurred" };
        }
    }
}