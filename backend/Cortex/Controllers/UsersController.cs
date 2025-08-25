using Cortex.Models.DTO;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var newUser = await _userService.RegisterAsync(userDto);

        return StatusCode(201, newUser);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var token = await _userService.LoginAsync(loginDto);

        return Ok(new { token });
    }
}
