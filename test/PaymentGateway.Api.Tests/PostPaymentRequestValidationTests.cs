using System.ComponentModel.DataAnnotations;

using PaymentGateway.Api.Models.Requests;

public class PostPaymentRequestTests
{
    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void CardNumber_ShouldBeRequired()
    {
        var model = new PostPaymentRequest();
        var result = ValidateModel(model);
        Assert.Contains(result, v => v.MemberNames.Contains("CardNumber"));
    }

    [Theory]
    [InlineData("123456789012345", true)]  // 15 digits
    [InlineData("1234567890123456", true)]  // 16 digits
    [InlineData("12345678901234567890", false)]  // 20 digits
    [InlineData("abcdefghijklmnop", false)]  // non-numeric
    public void CardNumber_ShouldHaveCorrectLength(string cardNumber, bool isValid)
    {
        var model = new PostPaymentRequest { CardNumber = cardNumber };
        var result = ValidateModel(model);
        Assert.Equal(isValid, !result.Any(v => v.MemberNames.Contains("CardNumber")));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(12, true)]
    [InlineData(13, false)]
    public void ExpiryMonth_ShouldBeInValidRange(int month, bool isValid)
    {
        var model = new PostPaymentRequest { ExpiryMonth = month };
        var result = ValidateModel(model);
        Assert.Equal(isValid, !result.Any(v => v.MemberNames.Contains("ExpiryMonth")));
    }

    [Theory]
    [InlineData(2023, false)]  // Past year
    [InlineData(2024, false)]  // Current year
    [InlineData(2025, true)]   // Next year
    [InlineData(2030, true)]   // Far future
    public void ExpiryYear_ShouldBeInFuture(int year, bool isValid)
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var model = new PostPaymentRequest { ExpiryYear = year, ExpiryMonth = 12 };  // Set December to avoid issues with current month

        // Act
        var result = ValidateModel(model);

        // Assert
        if (year < currentYear)
        {
            Assert.Contains(result, v => v.MemberNames.Contains("ExpiryYear"));
        }
        else if (year == currentYear)
        {
            // If it's the current year, it should be valid only if the expiry month is greater than the current month
            var currentMonth = DateTime.Now.Month;
            Assert.Equal(model.ExpiryMonth > currentMonth, !result.Any(v => v.MemberNames.Contains("ExpiryYear")));
        }
        else
        {
            Assert.Equal(isValid, !result.Any(v => v.MemberNames.Contains("ExpiryYear")));
        }
    }

    [Fact]
    public void ExpiryYear_ShouldBeValidForCurrentYearAndFutureMonth()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        var model = new PostPaymentRequest { ExpiryYear = currentYear, ExpiryMonth = currentMonth + 1 };

        // Act
        var result = ValidateModel(model);

        // Assert
        Assert.DoesNotContain(result, v => v.MemberNames.Contains("ExpiryYear"));
    }

    [Fact]
    public void ExpiryYear_ShouldBeInvalidForCurrentYearAndPastMonth()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        var model = new PostPaymentRequest { ExpiryYear = currentYear, ExpiryMonth = currentMonth - 1 };

        // Act
        var result = ValidateModel(model);

        // Assert
        Assert.Contains(result, v => v.MemberNames.Contains("ExpiryYear") || v.MemberNames.Contains("ExpiryMonth"));
    }

    [Theory]
    [InlineData("USD", true)]
    [InlineData("EUR", true)]
    [InlineData("GBP", true)]
    [InlineData("JPY", false)]
    public void Currency_ShouldBeValidCode(string currency, bool isValid)
    {
        var model = new PostPaymentRequest { Currency = currency };
        var result = ValidateModel(model);
        Assert.Equal(isValid, !result.Any(v => v.MemberNames.Contains("Currency")));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    public void Amount_ShouldBePositive(int amount, bool isValid)
    {
        var model = new PostPaymentRequest { Amount = amount };
        var result = ValidateModel(model);
        Assert.Equal(isValid, !result.Any(v => v.MemberNames.Contains("Amount")));
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    [InlineData(9999, true)]
    [InlineData(10000, false)]
    public void Cvv_ShouldBeValidLength(int cvv, bool isValid)
    {
        var model = new PostPaymentRequest { Cvv = cvv };
        var result = ValidateModel(model);
        Assert.Equal(isValid, !result.Any(v => v.MemberNames.Contains("Cvv")));
    }

    [Fact]
    public void ExpiryDate_ShouldSetMonthAndYear()
    {
        var model = new PostPaymentRequest { ExpiryDate = "12/2025" };
        Assert.Equal(12, model.ExpiryMonth);
        Assert.Equal(2025, model.ExpiryYear);
    }

    [Fact]
    public void ExpiryDate_ShouldGetFormattedString()
    {
        var model = new PostPaymentRequest { ExpiryMonth = 3, ExpiryYear = 2024 };
        Assert.Equal("03/2024", model.ExpiryDate);
    }
}
