using System.Text.Json.Serialization;

namespace AcquiringBank.Service;

public class AcquiringBankPaymentResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; set; }

    [JsonPropertyName("error_message")] 
    public string ErrorMessage { get; set; }

    public AcquiringBankPaymentResponse()
    {
        AuthorizationCode = string.Empty;
        ErrorMessage = string.Empty;
    }
}