namespace LCAP.HRMS.Application.Companies.DTOs;

public sealed class CompanyResponse
{
    public Guid Id { get; init; }
    public string CompanyCode { get; init; } = "";
    public string CompanyName { get; init; } = "";
    public string? LegalName { get; init; } = null;
    public string? RegisteredAddress { get; init; } = null;
    public string? City { get; init; } = null;
    public string? State { get; init; } = null;
    public string Country { get; init; } = "India";
    public string? PinCode { get; init; } = null;
    public string? PAN { get; init; } = null;
    public string? TAN { get; init; } = null;
    public string? GSTIN { get; init; } = null;
    public string? PFRegistrationNumber { get; init; } = null;
    public string? ESIRegistrationNumber { get; init; } = null;
    public string PayrollCurrency { get; init; } = "INR";
    public int PayrollDay { get; init; } = 0;
    public int SalaryPaymentDay { get; init; } = 0;
    public string? LogoUrl { get; init; } = null;
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public bool IsDeleted { get; init; }
}
