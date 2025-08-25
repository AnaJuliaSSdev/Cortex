using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class UserRegisterDto
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "The password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;
}
