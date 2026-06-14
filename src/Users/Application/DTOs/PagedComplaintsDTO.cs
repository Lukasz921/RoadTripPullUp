namespace Users.Application.DTOs;

public record PagedComplaintsDTO
{
    public List<ComplaintResponseDTO> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
