using System.ComponentModel.DataAnnotations;

public class RegisterDto
{
    [Required(ErrorMessage = "Please enter a username.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a password.")]
    public string Password { get; set; } = string.Empty;
}