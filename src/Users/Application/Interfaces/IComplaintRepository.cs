using Users.Core;

namespace Users.Application.Interfaces;

public interface IComplaintRepository
{
    Task Save(Complaint complaint);
    Task<Complaint?> FindById(Guid id);
}
