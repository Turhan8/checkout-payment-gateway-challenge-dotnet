using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AcquiringBank.Service;

public class AcquiringBankPaymentRequest
{
    [Required(ErrorMessage = "Card number is required.")]
    [StringLength(19, MinimumLength = 14, ErrorMessage = "Card number must be between 14 and 19 digits.")]
    [RegularExpression("^[0-9]+$", ErrorMessage = "Card number must only contain numeric characters.")]
    [JsonPropertyName("card_number")]
    public string CardNumber { get; set; }

    [Required(ErrorMessage = "Expiry date is required.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{4}$", ErrorMessage = "Expiry date must be in the format MM/YYYY.")]
    [ExpiryDateInFutureValidation(ErrorMessage = "The card expiry date must be in the future.")]
    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [RegularExpression("^(USD|EUR|GBP)$", ErrorMessage = "Currency must be one of the following: USD, EUR, GBP.")]
    [StringLength(3, ErrorMessage = "Currency code must be 3 characters.")]
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be a positive integer.")]
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [Required(ErrorMessage = "CVV is required.")]
    [Range(100, 9999, ErrorMessage = "CVV must be 3 or 4 digits.")]
    [JsonPropertyName("cvv")]
    public int Cvv { get; set; }
}