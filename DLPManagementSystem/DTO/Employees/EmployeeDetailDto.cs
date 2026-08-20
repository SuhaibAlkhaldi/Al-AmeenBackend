namespace DLPManagementSystem.DTO.Employees
{
    public sealed class EmployeeDetailDto : EmployeeListItemDto
    {
        public DateTimeOffset? UpdatedAtUtc { get; set; }

        // Set only on the response to CreateEmployeeAsync, and only when no password was supplied
        // in the request (i.e. the backend generated one) - the plaintext exists nowhere after this
        // response is sent, so the admin must copy it now. Never populated by any GET endpoint.
        public string? GeneratedPassword { get; set; }
    }
}
