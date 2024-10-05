using AcquiringBank.Service;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IAcquiringBankWrapper _acquiringBank;

    public PaymentsController(IPaymentsRepository paymentsRepository, IAcquiringBankWrapper acquiringBank)
    {
        _paymentsRepository = paymentsRepository;
        _acquiringBank = acquiringBank;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPaymentAsync(Guid id)
    {
        var payment = await _paymentsRepository.GetAsync(id);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);  // Returns 200 OK with the payment object
    }

    /// <summary>
    /// Process a payment
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///     POST /api/Payments
    ///     {
    ///        "expiry_date":"11/2030",
    ///        "amount":"100",
    ///        "card_number":"1000000000000001",
    ///        "currency":"GBP",
    ///        "cvv":"100"
    ///     }
    /// </remarks>
    /// <param name="request">Payment details</param>
    /// <returns>A payment response</returns>
    /// <response code="200">Returns the payment result</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    public async Task<IActionResult> MakePaymentAsync([FromBody] PostPaymentRequest request)
    {
        var result = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = request.ExpiryYear,
            ExpiryMonth = request.ExpiryMonth,
            Amount = request.Amount,
            CardNumberLastFour = new string(request.CardNumber.TakeLast(4).ToArray()),
            Currency = "GBP"
        };

        if (!ModelState.IsValid)
        {
            var errors = ModelState
            .SelectMany(x => x.Value.Errors)
            .Select(x => x.ErrorMessage)
            .ToList();

            var errorMessage = string.Join(Environment.NewLine, errors);
            Console.WriteLine($"Model validation failed: {errorMessage}");
            result.Status = PaymentStatus.Rejected;
            _paymentsRepository.Add(result);

            return BadRequest(result);
        }

        try
        {
            var acquiringBankPaymentRequest = new AcquiringBankPaymentRequest
            {
                CardNumber = request.CardNumber,
                ExpiryDate = request.ExpiryDate,
                Cvv = request.Cvv,
                Amount = request.Amount,
                Currency = request.Currency
            };

            var bankResponse = await _acquiringBank.MakePaymentAsync(acquiringBankPaymentRequest);

            if (bankResponse.Authorized)
            {
                result.Status = PaymentStatus.Authorized;
                result.AuthorizationCode = bankResponse.AuthorizationCode;
            }
            else if (bankResponse.ErrorMessage != string.Empty)
            {
                Console.WriteLine($"Bank error: {bankResponse.ErrorMessage}");
                result.Status = PaymentStatus.BankError;
            }
            else
            {
                result.Status = PaymentStatus.Declined;
            }

            _paymentsRepository.Add(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Payment Gateway unhandled exception: {ex.Message}");
            result.Status = PaymentStatus.GatewayError;
            _paymentsRepository.Add(result);
            //TODO: Here there is danger of attempting to save the payment twice as we do not know 
            // where exception is thrown.
            return StatusCode(500, result);
        }
        return Ok(result);
    }
}