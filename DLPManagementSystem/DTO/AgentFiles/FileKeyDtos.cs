namespace DLPManagementSystem.DTO.AgentFiles
{
    public class FileKeyWrapRequestDto
    {
        public Guid TenantId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid FileId { get; set; }
        public string PlainKeyBase64 { get; set; } = string.Empty;
    }

    public class FileKeyWrapResponseDto
    {
        public string KeyId { get; set; } = string.Empty;
        public string WrappedKeyBase64 { get; set; } = string.Empty;
    }

    public class FileKeyUnwrapRequestDto
    {
        public Guid TenantId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid FileId { get; set; }
        public string KeyId { get; set; } = string.Empty;
        public string WrappedKeyBase64 { get; set; } = string.Empty;
    }

    public class FileKeyUnwrapResponseDto
    {
        public string PlainKeyBase64 { get; set; } = string.Empty;
    }
}
