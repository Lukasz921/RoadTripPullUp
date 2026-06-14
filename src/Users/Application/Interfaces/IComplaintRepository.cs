using Users.Core;

namespace Users.Application.Interfaces;

public interface IComplaintRepository
{
    Task Save(Complaint complaint);
    Task<Complaint?> FindById(Guid id);
    Task<(List<Complaint> Items, int TotalCount)> GetAll(int page, int pageSize);
}
