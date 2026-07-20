namespace DLPManagementSystem.Data.Seed
{
    public interface IDatabaseSeeder
    {
        Task Seed(CancellationToken cancellationToken = default);
    }
}
