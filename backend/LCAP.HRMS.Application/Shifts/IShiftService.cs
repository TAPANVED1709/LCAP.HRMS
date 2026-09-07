using LCAP.HRMS.Application.Shifts.DTOs;

namespace LCAP.HRMS.Application.Shifts;

public interface IShiftService
{
    Task<IReadOnlyList<ShiftResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftResponse>> ListByCompanyAsync(Guid companyId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<ShiftResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShiftResponse> CreateAsync(ShiftCreateRequest request, CancellationToken cancellationToken = default);
    Task<ShiftResponse> UpdateAsync(Guid id, ShiftUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
