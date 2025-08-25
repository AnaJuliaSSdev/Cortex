using Cortex.Models.DTO;
using Cortex.Models;

namespace Cortex.Services.Interfaces;

public interface IUserService
{
    Task<User?> RegisterAsync(UserRegisterDto userDto);
    Task<string?> LoginAsync(UserLoginDto loginDto);
}
