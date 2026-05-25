using Users.Core;

namespace Users.Application;

public interface IJwtProvider
{
    string Generate(User user);
}
