using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Devices;
using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using DLPManagementSystem.Service.Interface;

namespace DLPManagementSystem.Service.Service
{
    public class DeviceService : IDeviceService
    {
        private readonly DLPSystemContext _db;

        public DeviceService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<PagedResultDto<DeviceListItemDto>>> GetDevicesAsync(
            Guid organizationId,
            string? search,
            int? statusId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Devices
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.MachineName.Contains(search));
            }

            if (statusId.HasValue)
            {
                query = query.Where(x => x.StatusId == statusId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.MachineName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new DeviceListItemDto
                {
                    Id = x.Id,
                    MachineName = x.MachineName,
                    OperatingSystem = x.OperatingSystem,
                    StatusId = x.StatusId,
                    StatusName = x.Status.DisplayName,
                    LastSeenAtUtc = x.LastSeenAtUtc,
                    CurrentPolicyVersion = x.CurrentPolicyVersion,
                    AssignedEmployeeName = x.DeviceUserAssignments
                        .Where(a => a.UnassignedAtUtc == null)
                        .Select(a => a.Employee.DisplayName)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<DeviceListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<DeviceListItemDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<DeviceDetailDto>> GetDeviceByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            var device = await _db.Devices
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == id)
                .Select(x => new DeviceDetailDto
                {
                    Id = x.Id,
                    MachineName = x.MachineName,
                    OperatingSystem = x.OperatingSystem,
                    StatusId = x.StatusId,
                    StatusName = x.Status.DisplayName,
                    LastSeenAtUtc = x.LastSeenAtUtc,
                    CurrentPolicyVersion = x.CurrentPolicyVersion,
                    DeviceKey = x.DeviceKey,
                    OsVersion = x.OsVersion,
                    SerialNumber = x.SerialNumber,
                    MacAddress = x.MacAddress,
                    AgentVersion = x.AgentVersion,
                    EnrolledAtUtc = x.EnrolledAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    AssignedEmployeeId = x.DeviceUserAssignments
                        .Where(a => a.UnassignedAtUtc == null)
                        .Select(a => (Guid?)a.EmployeeId)
                        .FirstOrDefault(),
                    AssignedEmployeeName = x.DeviceUserAssignments
                        .Where(a => a.UnassignedAtUtc == null)
                        .Select(a => a.Employee.DisplayName)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (device == null)
            {
                return ApiResponse<DeviceDetailDto>.FailureResponse("Device was not found.", "الجهاز غير موجود");
            }

            return ApiResponse<DeviceDetailDto>.SuccessResponse(device);
        }

        public async Task<ApiResponse<DeviceDetailDto>> UpdateDeviceAsync(Guid organizationId, Guid id, UpdateDeviceDto request, CancellationToken cancellationToken = default)
        {
            var device = await _db.Devices
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (device == null)
            {
                return ApiResponse<DeviceDetailDto>.FailureResponse("Device was not found.", "الجهاز غير موجود");
            }

            device.MachineName = request.MachineName;
            device.StatusId = request.StatusId;
            device.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return await GetDeviceByIdAsync(organizationId, id, cancellationToken);
        }

        public async Task<ApiResponse<bool>> DeleteDeviceAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            var device = await _db.Devices
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (device == null)
            {
                return ApiResponse<bool>.FailureResponse("Device was not found.", "الجهاز غير موجود");
            }

            var retiredStatus = await _db.DeviceStatuses.FirstOrDefaultAsync(x => x.Name == "Retired", cancellationToken);
            if (retiredStatus == null)
            {
                return ApiResponse<bool>.FailureResponse("Retired device status is not configured.", "حالة الإيقاف غير مهيأة");
            }

            device.StatusId = retiredStatus.Id;
            device.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Device decommissioned successfully.", "تم إيقاف الجهاز بنجاح");
        }

        public async Task<ApiResponse<bool>> AssignDeviceAsync(Guid organizationId, Guid id, Guid assignedByUserId, AssignDeviceDto request, CancellationToken cancellationToken = default)
        {
            var device = await _db.Devices
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (device == null)
            {
                return ApiResponse<bool>.FailureResponse("Device was not found.", "الجهاز غير موجود");
            }

            var employee = await _db.Employees
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == request.EmployeeId, cancellationToken);

            if (employee == null)
            {
                return ApiResponse<bool>.FailureResponse("Employee was not found.", "الموظف غير موجود");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var activeAssignments = await _db.DeviceUserAssignments
                .Where(x => x.DeviceId == id && x.UnassignedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var activeAssignment in activeAssignments)
            {
                activeAssignment.UnassignedAtUtc = nowUtc;
            }

            _db.DeviceUserAssignments.Add(new DeviceUserAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DeviceId = id,
                EmployeeId = employee.Id,
                UserSid = string.Empty,
                IsPrimary = true,
                AssignedAtUtc = nowUtc,
                AssignedByUserId = assignedByUserId
            });

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Device assigned successfully.", "تم تعيين الجهاز بنجاح");
        }

        public async Task<ApiResponse<bool>> UnassignDeviceAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            var device = await _db.Devices
                .AnyAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (!device)
            {
                return ApiResponse<bool>.FailureResponse("Device was not found.", "الجهاز غير موجود");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var activeAssignments = await _db.DeviceUserAssignments
                .Where(x => x.DeviceId == id && x.UnassignedAtUtc == null)
                .ToListAsync(cancellationToken);

            if (activeAssignments.Count == 0)
            {
                return ApiResponse<bool>.FailureResponse("Device has no active assignment.", "لا يوجد تعيين نشط لهذا الجهاز");
            }

            foreach (var activeAssignment in activeAssignments)
            {
                activeAssignment.UnassignedAtUtc = nowUtc;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Device unassigned successfully.", "تم إلغاء تعيين الجهاز بنجاح");
        }
    }
}
