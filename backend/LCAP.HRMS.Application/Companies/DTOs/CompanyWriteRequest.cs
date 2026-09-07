using System.ComponentModel.DataAnnotations;

namespace LCAP.HRMS.Application.Companies.DTOs;

// Shared writable fields; callers cannot set identity, audit fields, or IsDeleted.
public abstract class CompanyWriteRequest
{
    [Required]
    [StringLength(50)]
    public string CompanyCode { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = "";

    [StringLength(250)]
    public string? LegalName { get; set; } = null;

    [StringLength(1000)]
    public string? RegisteredAddress { get; set; } = null;

    [StringLength(100)]
    public string? City { get; set; } = null;

    [StringLength(100)]
    public string? State { get; set; } = null;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = "India";

    [StringLength(20)]
    public string? PinCode { get; set; } = null;

    [StringLength(50)]
    public string? PAN { get; set; } = null;

    [StringLength(50)]
    public string? TAN { get; set; } = null;

    [StringLength(50)]
    public string? GSTIN { get; set; } = null;

    [StringLength(50)]
    public string? PFRegistrationNumber { get; set; } = null;

    [StringLength(50)]
    public string? ESIRegistrationNumber { get; set; } = null;

    [Required]
    [StringLength(3)]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "PayrollCurrency must be a three-letter currency code.")]
    public string PayrollCurrency { get; set; } = "INR";

    [Range(1, 31)]
    public int PayrollDay { get; set; } = 0;

    [Range(1, 31)]
    public int SalaryPaymentDay { get; set; } = 0;

    [StringLength(2048)]
    [Url]
    public string? LogoUrl { get; set; } = null;

    public bool IsActive { get; set; } = true;
}
