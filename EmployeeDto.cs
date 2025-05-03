namespace BLSShared.Models;

public class EmployeeDto
{
	public long Id { get; set; }
	public string UserName { get; set; }
	public string Password { get; set; }
	public List<string> Roles { get; set; } = [];
}
