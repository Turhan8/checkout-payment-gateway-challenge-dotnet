using AcquiringBank.Service;
using PaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration sources
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
// builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
//     {
//         options.SuppressModelStateInvalidFilter = true;
//     }); ;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddSingleton<IAcquiringBankWrapper, AcquiringBankWrapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
