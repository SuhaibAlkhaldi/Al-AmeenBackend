namespace DLPManagementSystem.Service.Interface
{
    public interface IPolicyVersionService
    {
        Task BumpAsync(
            Guid organizationId,
            Guid? changedByUserId,
            string changeType,
            string entityType,
            Guid? entityId,
            string description,
            CancellationToken cancellationToken = default);
    }
}
