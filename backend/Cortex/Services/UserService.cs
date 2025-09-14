using Cortex.Models.DTO;
using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Cortex.Exceptions;
using StockApp2._0.Mapper;

namespace Cortex.Services;

public class UserService(IUserRepository userRepository, IConfiguration configuration) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IConfiguration _configuration = configuration;

    private const int TIME_PER_TOKEN = 8;

    public async Task<User?> RegisterAsync(UserRegisterDto userDto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(userDto.Email);
        if (existingUser != null)
        {
            throw new EmailAlreadyInUseException();
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

        var newUser = Mapper.Map<User>(userDto);
        newUser.PasswordHash = passwordHash;

        await _userRepository.AddAsync(newUser);

        return newUser;
    }
    public async Task<string?> LoginAsync(UserLoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email) ?? throw new InvalidCredentialsException();

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new InvalidCredentialsException();
        }

        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName)
            ]),
            Expires = DateTime.UtcNow.AddHours(TIME_PER_TOKEN),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
