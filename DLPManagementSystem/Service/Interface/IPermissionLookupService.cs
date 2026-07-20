namespace DLPManagementSystem.Service.Interface
{
    public interface IPermissionLookupService
    {
        Task<int> GetPermissionDecisionId(string name, CancellationToken cancellationToken);
        Task<int> GetPermissionGrantTypeId(string name, CancellationToken cancellationToken);
        Task<int> GetPermissionSubjectTypeId(string name, CancellationToken cancellationToken);
        Task<int> GetPermissionRequestStatusId(string name, CancellationToken cancellationToken);
        Task<int> GetPermissionRequestReviewDecisionId(string name, CancellationToken cancellationToken);
    }
}
