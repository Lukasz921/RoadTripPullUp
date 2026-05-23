namespace Application.TripPlanner;

public class SearchJobCreatedDTO
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string StatusUrl { get; set; } = string.Empty;
    public int EstimatedDurationMs { get; set; }
}

public class SearchJobProgressDTO
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public SearchProgressDetailsDTO Progress { get; set; } = new();
}

public class SearchProgressDetailsDTO
{
    public string Phase { get; set; } = string.Empty;
    public int CandidatesFound { get; set; }
    public int CandidatesProcessed { get; set; }
}

public class SearchJobResultDTO
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public List<TripSummaryV1DTO>? Items { get; set; }
    public int? Count { get; set; }
    public SearchJobErrorDTO? Error { get; set; }
}

public class SearchJobErrorDTO
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SearchJobPollResult
{
    public bool IsProcessing { get; set; }
    public SearchJobProgressDTO? Progress { get; set; }
    public SearchJobResultDTO? Result { get; set; }
}
