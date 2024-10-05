using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Responses;

public class GetPaymentResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("status")]
    public PaymentStatus Status { get; set; }

    [Required(ErrorMessage = "Card number last four is required.")]
    [StringLength(4, ErrorMessage = "Card number last four must be 4 digits.")]
    [RegularExpression("^[0-9]+$", ErrorMessage = "Card number must only contain numeric characters.")]
    [JsonPropertyName("card_number_las_four")]
    public string CardNumberLastFour { get; set; }

    [Required(ErrorMessage = "Expiry month is required.")]
    [Range(1, 12, ErrorMessage = "Expiry month must be between 1 and 12.")]
    [JsonPropertyName("expiry_month")]
    public int ExpiryMonth { get; set; }

    [Required(ErrorMessage = "Expiry year is required.")]
    [JsonPropertyName("expiry_year")]
    public int ExpiryYear { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [RegularExpression("^(USD|EUR|GBP)$", ErrorMessage = "Currency must be one of the following: USD, EUR, GBP.")]
    [StringLength(3, ErrorMessage = "Currency code must be 3 characters.")]
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be a positive integer.")]
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonIgnore]
    public string AuthorizationCode { get; set; }
}