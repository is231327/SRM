namespace SRMCore.Configuration;

public class RedmineOptions
{
    public const string SectionName = "Redmine";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ProjectIdentifier { get; set; } = string.Empty;
    public int TrackerId { get; set; } = 1;
    public int StatusId { get; set; } = 1;
    public int PollIntervalSeconds { get; set; } = 15;
    public int WarningPriorityId { get; set; } = 3;
    public int MajorPriorityId { get; set; } = 4;
    public int CriticalPriorityId { get; set; } = 5;
}
