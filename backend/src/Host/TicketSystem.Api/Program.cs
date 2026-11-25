using TicketSystem.Booking;
using TicketSystem.IdentityAccess;
using TicketSystem.Inventory;
using TicketSystem.Payment;
using TicketSystem.Scheduling;
using TicketSystem.Shared.Infrastructure.Persistence;
using TicketSystem.Ticketing;

var builder = WebApplication.CreateBuilder(args);

var postgres = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "No ConnectionStrings:Postgres found. Please add it to appsettings.json or user secrets.");
var redis = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Redis.");

builder.Services.AddUnitOfWork(postgres);

builder.Services
    .AddSchedulingModule(builder.Configuration)
    .AddInventoryModule(builder.Configuration)
    .AddBookingModule(builder.Configuration)
    .AddPaymentModule(builder.Configuration)
    .AddTicketingModule(builder.Configuration)
    .AddIdentityAccessModule(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddNpgSql(postgres)
    .AddRedis(redis);

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;