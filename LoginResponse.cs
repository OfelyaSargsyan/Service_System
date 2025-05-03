namespace BLSShared.Models;

public class LoginResponse
{ 
	public int Code { get; set; }
	public long UserId { get; set; }
	public string UserName { get; set; }
	public List<string> Roles { get; set; } = [];
}
