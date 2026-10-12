using System.ComponentModel.DataAnnotations;

public class LoginDto
{
    [Required(ErrorMessage = "Please enter a username.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a password.")]
    public string Password { get; set; } = string.Empty;
}