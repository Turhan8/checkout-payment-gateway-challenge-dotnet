using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Models.CustomAttributes;

/// <summary>
/// Custom validation attribute to ensure that the expiry year and month combination is in the future.
/// </summary>
public class FutureDateValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var paymentInfo = (PostPaymentRequest)validationContext.ObjectInstance;

        if (paymentInfo.ExpiryMonth == 0 || paymentInfo.ExpiryYear == 0)
        {
            return new ValidationResult(ErrorMessage);
        }

        var expiryMonth = paymentInfo.ExpiryMonth;

        // Get the last of day of the expiry month for the given year.
        int daysInMonth = DateTime.DaysInMonth(paymentInfo.ExpiryYear, expiryMonth);

        var expiryDate = new DateTime(paymentInfo.ExpiryYear, expiryMonth, daysInMonth);

        var currentDate = DateTime.UtcNow;

        if (expiryDate < currentDate)
        {
            return new ValidationResult(ErrorMessage,
             new[] { nameof(PostPaymentRequest.ExpiryYear), nameof(PostPaymentRequest.ExpiryMonth) }
         );
        }

        return ValidationResult.Success;
    }
}
