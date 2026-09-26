namespace DLPManagementSystem.DTO.AgentFileInventory
{
    public class RejectedFileInventoryChangeDto
    {
        public Guid ChangeId { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public bool Retryable { get; set; }
    }

    // No DuplicateChangeIds bucket, unlike AgentAuditBatchResultDto - see AgentFileInventoryService's
    // class comment: every change here is applied via idempotent upsert/soft-delete, so re-delivering
    // an already-applied change just re-produces the same end state and is reported Accepted, not
    // Duplicate. Only a genuine validation failure lands in RejectedChanges.
    public class AgentFileInventoryBatchResultDto
    {
        public List<Guid> AcceptedChangeIds { get; set; } = new();
        public List<RejectedFileInventoryChangeDto> RejectedChanges { get; set; } = new();
    }
}
