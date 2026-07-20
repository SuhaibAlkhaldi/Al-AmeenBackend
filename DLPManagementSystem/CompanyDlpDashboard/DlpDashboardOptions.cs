namespace DLPManagementSystem.CompanyDlpDashboard;

public sealed class DlpDashboardOptions
{
    public string ConnectionStringName { get; set; } = "DefaultConnection";
    public int DefaultLookbackHours { get; set; } = 24;
    public int MaximumLookbackDays { get; set; } = 30;
}
