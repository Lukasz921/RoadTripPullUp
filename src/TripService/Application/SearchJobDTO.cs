namespace TripService.Application;

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
    public List<TripSummaryDTO>? Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
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

public class SearchTripsQueryDTO
{
    public double SourceLat { get; set; }
    public double SourceLng { get; set; }
    public double TargetLat { get; set; }
    public double TargetLng { get; set; }
    public string DateFrom { get; set; } = string.Empty;
    public string DateTo { get; set; } = string.Empty;
    public decimal? MaxPrice { get; set; }
    public int MinSeats { get; set; } = 1;
    public string SortBy { get; set; } = "departure";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SyncSearchResultDTO
{
    public List<TripSummaryDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
