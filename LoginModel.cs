using System.ComponentModel.DataAnnotations;

namespace BLSShared.Models;

public class LoginModel
{
	[Required]
	public string Password { get; set; }

	[Required(ErrorMessage = "The Check Point field is required.")]
	public string CheckPointNumber { get; set; }
}
