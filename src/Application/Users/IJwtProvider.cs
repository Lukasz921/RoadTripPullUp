using Core.Users;

namespace Application.Users;

public interface IJwtProvider
{
    string Generate(User user);
}
