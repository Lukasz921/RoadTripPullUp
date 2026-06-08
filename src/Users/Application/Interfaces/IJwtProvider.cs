using Users.Core;

namespace Users.Application.Interfaces;

public interface IJwtProvider
{
    string Generate(User user);
}
