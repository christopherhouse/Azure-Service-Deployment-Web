using Microsoft.AspNetCore.Authorization;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using Billing.Api.Services;
using FluentValidation;
using System.Security.Claims;

namespace Billing.Api.Endpoints;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/billing")
            .WithTags("Billing & Subscriptions")
            .RequireAuthorization();

        // Subscription management
        group.MapGet("/subscription", GetSubscription)
            .WithName("GetSubscription")
            .WithOpenApi();

        group.MapPost("/subscription", CreateSubscription)
            .WithName("CreateSubscription")
            .WithOpenApi();

        group.MapPut("/subscription/plan", UpdateSubscriptionPlan)
            .WithName("UpdateSubscriptionPlan")
            .WithOpenApi();

        group.MapPost("/subscription/cancel", CancelSubscription)
            .WithName("CancelSubscription")
            .WithOpenApi();

        // Invoice management
        group.MapGet("/invoices", GetInvoices)
            .WithName("GetInvoices")
            .WithOpenApi();

        group.MapGet("/invoices/{id:guid}", GetInvoice)
            .WithName("GetInvoice")
            .WithOpenApi();

        // Usage tracking
        group.MapGet("/usage", GetUsage)
            .WithName("GetUsage")
            .WithOpenApi();

        // Payment methods
        group.MapGet("/payment-method", GetPaymentMethod)
            .WithName("GetPaymentMethod")
            .WithOpenApi();

        group.MapPut("/payment-method", UpdatePaymentMethod)
            .WithName("UpdatePaymentMethod")
            .WithOpenApi();
    }

    private static async Task<IResult> GetSubscription(
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var subscription = await billingService.GetSubscriptionAsync(tenantId);
            
            if (subscription == null)
                return Results.NotFound(new { error = "Subscription not found", tenantId });

            return Results.Ok(subscription);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get subscription");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving subscription");
            return Results.Problem("Failed to retrieve subscription", statusCode: 500);
        }
    }

    private static async Task<IResult> CreateSubscription(
        CreateSubscriptionRequest request,
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger,
        IValidator<CreateSubscriptionRequest> validator)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(request);
            
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new { 
                    error = "Validation failed", 
                    errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
                });
            }

            var tenantId = GetTenantId(user);
            var subscription = await billingService.CreateSubscriptionAsync(tenantId, request.Plan);
            return Results.Created($"/api/v1/billing/subscription", subscription);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to create subscription");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating subscription");
            return Results.Problem("Failed to create subscription", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateSubscriptionPlan(
        UpdateSubscriptionPlanRequest request,
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger,
        IValidator<UpdateSubscriptionPlanRequest> validator)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(request);
            
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new { 
                    error = "Validation failed", 
                    errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
                });
            }

            var tenantId = GetTenantId(user);
            var subscription = await billingService.UpdateSubscriptionPlanAsync(tenantId, request.NewPlan);
            return Results.Ok(subscription);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument for subscription plan update");
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to update subscription plan");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating subscription plan");
            return Results.Problem("Failed to update subscription plan", statusCode: 500);
        }
    }

    private static async Task<IResult> CancelSubscription(
        CancelSubscriptionRequest request,
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger,
        IValidator<CancelSubscriptionRequest> validator)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(request);
            
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new { 
                    error = "Validation failed", 
                    errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
                });
            }

            var tenantId = GetTenantId(user);
            var cancelled = await billingService.CancelSubscriptionAsync(tenantId, request.Reason);
            
            if (!cancelled)
                return Results.BadRequest(new { error = "Unable to cancel subscription" });

            return Results.NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to cancel subscription");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling subscription");
            return Results.Problem("Failed to cancel subscription", statusCode: 500);
        }
    }

    private static async Task<IResult> GetInvoices(
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenantId = GetTenantId(user);
            var invoices = await billingService.GetInvoicesAsync(tenantId, page, pageSize);
            return Results.Ok(invoices);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get invoices");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving invoices");
            return Results.Problem("Failed to retrieve invoices", statusCode: 500);
        }
    }

    private static async Task<IResult> GetInvoice(
        Guid id,
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var invoice = await billingService.GetInvoiceAsync(tenantId, id);
            
            if (invoice == null)
                return Results.NotFound(new { error = "Invoice not found", invoiceId = id });

            return Results.Ok(invoice);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get invoice {InvoiceId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
            return Results.Problem("Failed to retrieve invoice", statusCode: 500);
        }
    }

    private static async Task<IResult> GetUsage(
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            if (from >= to)
                return Results.BadRequest(new { error = "From date must be before to date" });

            var usage = await billingService.GetUsageAsync(tenantId, from, to);
            return Results.Ok(usage);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get usage");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving usage");
            return Results.Problem("Failed to retrieve usage", statusCode: 500);
        }
    }

    private static async Task<IResult> GetPaymentMethod(
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var paymentMethod = await billingService.GetPaymentMethodAsync(tenantId);
            
            if (paymentMethod == null)
                return Results.NotFound(new { error = "Payment method not found", tenantId });

            return Results.Ok(paymentMethod);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get payment method");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment method");
            return Results.Problem("Failed to retrieve payment method", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdatePaymentMethod(
        UpdatePaymentMethodRequest request,
        IBillingService billingService,
        ClaimsPrincipal user,
        ILogger<IBillingService> logger,
        IValidator<UpdatePaymentMethodRequest> validator)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(request);
            
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new { 
                    error = "Validation failed", 
                    errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
                });
            }

            var tenantId = GetTenantId(user);
            var paymentMethod = new PaymentMethodDto
            {
                TenantId = tenantId,
                Type = request.Type,
                LastFourDigits = request.LastFourDigits,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                IsDefault = true
            };

            var updatedPaymentMethod = await billingService.UpdatePaymentMethodAsync(tenantId, paymentMethod);
            return Results.Ok(updatedPaymentMethod);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to update payment method");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating payment method");
            return Results.Problem("Failed to update payment method", statusCode: 500);
        }
    }

    private static Guid GetTenantId(ClaimsPrincipal user)
    {
        var tenantClaim = user.FindFirst("tenant_id")?.Value ?? user.FindFirst("extension_tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Invalid tenant information");
        }
        return tenantId;
    }
}

// Request models
public class CreateSubscriptionRequest
{
    public SubscriptionPlan Plan { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    public SubscriptionPlan NewPlan { get; set; }
}

public class CancelSubscriptionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdatePaymentMethodRequest
{
    public PaymentMethodType Type { get; set; }
    public string LastFourDigits { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
}

// Validators
public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Plan)
            .IsInEnum().WithMessage("Invalid subscription plan");
    }
}

public class UpdateSubscriptionPlanRequestValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanRequestValidator()
    {
        RuleFor(x => x.NewPlan)
            .IsInEnum().WithMessage("Invalid subscription plan");
    }
}

public class CancelSubscriptionRequestValidator : AbstractValidator<CancelSubscriptionRequest>
{
    public CancelSubscriptionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}

public class UpdatePaymentMethodRequestValidator : AbstractValidator<UpdatePaymentMethodRequest>
{
    public UpdatePaymentMethodRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid payment method type");

        RuleFor(x => x.LastFourDigits)
            .NotEmpty().WithMessage("Last four digits are required")
            .Length(4).WithMessage("Last four digits must be exactly 4 characters")
            .Matches("^[0-9]+$").WithMessage("Last four digits must be numeric");

        RuleFor(x => x.ExpiryMonth)
            .GreaterThanOrEqualTo(1).WithMessage("Expiry month must be between 1 and 12")
            .LessThanOrEqualTo(12).WithMessage("Expiry month must be between 1 and 12");

        RuleFor(x => x.ExpiryYear)
            .GreaterThanOrEqualTo(DateTime.Now.Year).WithMessage("Expiry year cannot be in the past")
            .LessThanOrEqualTo(DateTime.Now.Year + 20).WithMessage("Expiry year is too far in the future");
    }
}