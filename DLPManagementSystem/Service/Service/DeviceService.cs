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
        private readonly IPolicyVersionService _policyVersionService;
        private readonly IAdminAuditLogService _adminAuditLogService;

        public DeviceService(DLPSystemContext db, IPolicyVersionService policyVersionService, IAdminAuditLogService adminAuditLogService)
        {
            _db = db;
            _policyVersionService = policyVersionService;
            _adminAuditLogService = adminAuditLogService;
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
                    // A device can now have more than one active assignment (shared-device support) -
                    // ordered by IsPrimary then earliest-assigned so this single-name display field
                    // stays deterministic and, for the common single-assignment case, identical to
                    // before. The full list is available via GetDeviceAssignmentsAsync.
                    AssignedEmployeeName = x.DeviceUserAssignments
                        .Where(a => a.UnassignedAtUtc == null)
                        .OrderByDescending(a => a.IsPrimary)
                        .ThenBy(a => a.AssignedAtUtc)
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
                        .OrderByDescending(a => a.IsPrimary)
                        .ThenBy(a => a.AssignedAtUtc)
                        .Select(a => (Guid?)a.EmployeeId)
                        .FirstOrDefault(),
                    AssignedEmployeeName = x.DeviceUserAssignments
                        .Where(a => a.UnassignedAtUtc == null)
                        .OrderByDescending(a => a.IsPrimary)
                        .ThenBy(a => a.AssignedAtUtc)
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

        public async Task<ApiResponse<DeviceDetailDto>> UpdateDeviceAsync(Guid organizationId, Guid id, Guid callerUserId, UpdateDeviceDto request, CancellationToken cancellationToken = default)
        {
            var device = await _db.Devices
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (device == null)
            {
                return ApiResponse<DeviceDetailDto>.FailureResponse("Device was not found.", "الجهاز غير موجود");
            }

            var oldStatusId = device.StatusId;
            var oldStatusName = device.Status.Name;

            device.MachineName = request.MachineName;
            device.StatusId = request.StatusId;
            device.UpdatedAtUtc = DateTimeOffset.UtcNow;

            if (oldStatusId != request.StatusId)
            {
                var newStatus = await _db.DeviceStatuses.FirstOrDefaultAsync(x => x.Id == request.StatusId, cancellationToken);

                await _adminAuditLogService.LogAsync(
                    organizationId, callerUserId, "DeviceStatusChanged", "Device", device.Id, device.MachineName,
                    $"{oldStatusName} -> {newStatus?.Name ?? request.StatusId.ToString()}", cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetDeviceByIdAsync(organizationId, id, cancellationToken);
        }

        public async Task<ApiResponse<bool>> DeleteDeviceAsync(Guid organizationId, Guid id, Guid callerUserId, CancellationToken cancellationToken = default)
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

            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "DeviceRetired", "Device", device.Id, device.MachineName,
                null, cancellationToken);

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

            var assignment = new DeviceUserAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DeviceId = id,
                EmployeeId = employee.Id,
                UserSid = string.Empty,
                IsPrimary = true,
                AssignedAtUtc = nowUtc,
                AssignedByUserId = assignedByUserId
            };

            _db.DeviceUserAssignments.Add(assignment);

            await _policyVersionService.BumpAsync(
                organizationId,
                assignedByUserId,
                "DeviceAssigned",
                "Device",
                id,
                $"Device '{device.MachineName}' assigned to employee '{employee.DisplayName}'.",
                cancellationToken);

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

            await _policyVersionService.BumpAsync(
                organizationId,
                null,
                "DeviceUnassigned",
                "Device",
                id,
                "Device unassigned from its employee.",
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Device unassigned successfully.", "تم إلغاء تعيين الجهاز بنجاح");
        }

        public async Task<ApiResponse<List<DeviceAssignmentDto>>> GetDeviceAssignmentsAsync(
            Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            var deviceExists = await _db.Devices.AnyAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (!deviceExists)
            {
                return ApiResponse<List<DeviceAssignmentDto>>.FailureResponse("Device was not found.", "الجهاز غير موجود");
            }

            var assignments = await _db.DeviceUserAssignments
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.DeviceId == id && x.UnassignedAtUtc == null)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.AssignedAtUtc)
                .Select(x => new DeviceAssignmentDto
                {
                    Id = x.Id,
                    DeviceId = x.DeviceId,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.DisplayName,
                    IsPrimary = x.IsPrimary,
                    AssignedAtUtc = x.AssignedAtUtc
                })
                .ToListAsync(cancellationToken);

            return ApiResponse<List<DeviceAssignmentDto>>.SuccessResponse(assignments);
        }

        // Adds an ADDITIONAL active assignment for a shared device without ending any existing one -
        // distinct from AssignDeviceAsync above (which replaces the single assignment; left completely
        // unchanged for the common single-employee-per-device case). The first-ever assignment for a
        // device (via either endpoint) is IsPrimary; every assignment added afterward through this
        // endpoint is not, purely for the deterministic single-name display fallback in
        // GetDevicesAsync/GetDeviceByIdAsync - IsPrimary has no bearing on which grants actually apply,
        // AgentPolicyService.BuildGrantsForDeviceAsync treats every active assignment equally and
        // resolves by UserSid once there is more than one.
        public async Task<ApiResponse<bool>> AddDeviceAssignmentAsync(
            Guid organizationId, Guid id, Guid assignedByUserId, AssignDeviceDto request, CancellationToken cancellationToken = default)
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

            var activeAssignments = await _db.DeviceUserAssignments
                .Where(x => x.DeviceId == id && x.UnassignedAtUtc == null)
                .ToListAsync(cancellationToken);

            if (activeAssignments.Any(x => x.EmployeeId == employee.Id))
            {
                return ApiResponse<bool>.FailureResponse(
                    "This employee is already actively assigned to this device.",
                    "هذا الموظف مُعيَّن بالفعل لهذا الجهاز");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var assignment = new DeviceUserAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DeviceId = id,
                EmployeeId = employee.Id,
                UserSid = string.Empty,
                IsPrimary = activeAssignments.Count == 0,
                AssignedAtUtc = nowUtc,
                AssignedByUserId = assignedByUserId
            };

            _db.DeviceUserAssignments.Add(assignment);

            await _policyVersionService.BumpAsync(
                organizationId,
                assignedByUserId,
                "DeviceAssignmentAdded",
                "Device",
                id,
                $"Device '{device.MachineName}' additionally assigned to employee '{employee.DisplayName}' (shared device).",
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Employee added to device successfully.", "تمت إضافة الموظف للجهاز بنجاح");
        }

        // Ends exactly ONE active assignment (by employee), leaving any other active assignments on
        // this device untouched - distinct from UnassignDeviceAsync above (which ends all of them; left
        // unchanged for the common single-employee-per-device case).
        public async Task<ApiResponse<bool>> RemoveDeviceAssignmentAsync(
            Guid organizationId, Guid id, Guid employeeId, CancellationToken cancellationToken = default)
        {
            var assignment = await _db.DeviceUserAssignments
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.DeviceId == id
                    && x.EmployeeId == employeeId && x.UnassignedAtUtc == null, cancellationToken);

            if (assignment == null)
            {
                return ApiResponse<bool>.FailureResponse("No active assignment was found for this employee on this device.", "لا يوجد تعيين نشط لهذا الموظف على هذا الجهاز");
            }

            var nowUtc = DateTimeOffset.UtcNow;
            assignment.UnassignedAtUtc = nowUtc;

            await _policyVersionService.BumpAsync(
                organizationId,
                null,
                "DeviceAssignmentRemoved",
                "Device",
                id,
                $"Employee {employeeId} unassigned from device (other active assignments, if any, are unaffected).",
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Employee removed from device successfully.", "تمت إزالة الموظف من الجهاز بنجاح");
        }
    }
}
