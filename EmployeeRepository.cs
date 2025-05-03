using BLSShared.Models;
using System.Security.Cryptography;
using System.Text;

namespace BLSWebAPI.Services;

public class EmployeeRepository : IEmployeeRepository
{
	private readonly DbService _dbService;

	public EmployeeRepository(DbService dbService)
	{
		_dbService = dbService;
	}

	public async Task<LoginResponse> LoginAsync(LoginViewModel data)
	{
		var res = await _dbService.GetEmployeeAsync(data.UserName);

		if (res is null)
		{
			return new LoginResponse
			{
				Code = -1
			};
		}

		if (Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(data.Password))) != res.Password)
		{
			return new LoginResponse
			{
				Code = -2
			};
		}

		return new LoginResponse
		{
			Code = 0,
			UserName = res.UserName,
			UserId = res.Id,
			Roles = res.Roles,
		};
	}

	public async Task<int> AddOrUpdateEmployeeAsync(EmployeeDto employee)
	{
		employee.Password = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(employee.Password)));
		return await _dbService.AddOrUpdateEmployeeAsync(employee);
	}

	public async Task<int> DeleteEmployeeAsync(string name)
	{
		return await _dbService.DeleteEmployeeAsync(name);
	}

	public async Task<List<EmployeeDto>> GetEmployeesAsync()
	{
		return await _dbService.GetEmployeesAsync();
	}
}