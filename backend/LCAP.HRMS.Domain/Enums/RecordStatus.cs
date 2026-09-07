namespace LCAP.HRMS.Domain.Enums;

// Business status is separate from the IsDeleted persistence flag.
public enum RecordStatus
{
    Unknown = 0,
    Draft = 1,
    Active = 2,
    Inactive = 3
}
