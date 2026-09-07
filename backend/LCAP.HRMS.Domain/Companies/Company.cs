using LCAP.HRMS.Domain.Shifts;
using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Domain.Branches;
using LCAP.HRMS.Domain.Departments;
using LCAP.HRMS.Domain.Designations;
using LCAP.HRMS.Domain.WorkLocations;

namespace LCAP.HRMS.Domain.Companies;

public sealed class Company : BaseEntity
{
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Designation> Designations { get; set; } = new List<Designation>();
    public ICollection<WorkLocation> WorkLocations { get; set; } = new List<WorkLocation>();
    public string CompanyCode { get; set; } = "";

    public string CompanyName { get; set; } = "";

    public string? LegalName { get; set; } = null;

    public string? RegisteredAddress { get; set; } = null;

    public string? City { get; set; } = null;

    public string? State { get; set; } = null;

    public string Country { get; set; } = "India";

    public string? PinCode { get; set; } = null;

    public string? PAN { get; set; } = null;

    public string? TAN { get; set; } = null;

    public string? GSTIN { get; set; } = null;

    public string? PFRegistrationNumber { get; set; } = null;

    public string? ESIRegistrationNumber { get; set; } = null;

    public string PayrollCurrency { get; set; } = "INR";

    public int PayrollDay { get; set; } = 0;

    public int SalaryPaymentDay { get; set; } = 0;

    public string? LogoUrl { get; set; } = null;

    public bool IsActive { get; set; } = true;
}
