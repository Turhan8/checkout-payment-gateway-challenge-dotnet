using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PaymentGateway.Api.Models.CustomAttributes;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    /// <summary>
    /// Card number
    /// </summary>
    /// <example>1234567812345678</example>
    [Required(ErrorMessage = "Card number is required.")]
    [StringLength(19, MinimumLength = 14, ErrorMessage = "Card number must be between 14 and 19 digits.")]
    [RegularExpression("^[0-9]+$", ErrorMessage = "Card number must only contain numeric characters.")]
    [JsonPropertyName("card_number")]
    public string CardNumber { get; set; }

    [Required(ErrorMessage = "Expiry month is required.")]
    [Range(1, 12, ErrorMessage = "Expiry month must be between 1 and 12.")]
    [JsonPropertyName("expiry_month")]
    public int ExpiryMonth { get; set; }

    [Required(ErrorMessage = "Expiry year is required.")]
    [FutureDateValidation(ErrorMessage = "The card expiry date must be in the future.")]
    [JsonPropertyName("expiry_year")]
    public int ExpiryYear { get; set; }

    [JsonIgnore]
    public string ExpiryDate
    {
        get
        {
            return $"{ExpiryMonth:D2}/{ExpiryYear}";
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Parse the expiry date in MM/yyyy format
                var parts = value.Split('/');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int month) &&
                    int.TryParse(parts[1], out int year))
                {
                    ExpiryMonth = month;
                    ExpiryYear = year;
                }
            }
        }
    }

    [Required(ErrorMessage = "Currency is required.")]
    [RegularExpression("^(USD|EUR|GBP)$", ErrorMessage = "Currency must be one of the following: USD, EUR, GBP.")]
    [StringLength(3, ErrorMessage = "Currency code must be 3 characters.")]
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    /// <summary>
    /// Represents the amount in the minor currency unit. 
    /// $0.01 would be supplied as 1 and $10.50 would be supplied as 1050
    /// </summary>
    [Required(ErrorMessage = "Amount is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be a positive integer.")]
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [Required(ErrorMessage = "CVV is required.")]
    [Range(100, 9999, ErrorMessage = "CVV must be 3 or 4 digits.")]
    [JsonPropertyName("cvv")]
    public int Cvv { get; set; }
}