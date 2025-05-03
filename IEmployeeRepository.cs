using BLSShared.Models;

namespace BLSWebAPI.Services;

public interface IEmployeeRepository
{
    Task<LoginResponse> LoginAsync(LoginViewModel data);
    Task<int> AddOrUpdateEmployeeAsync(EmployeeDto employee);
    Task<int> DeleteEmployeeAsync(string name);
    Task<List<EmployeeDto>> GetEmployeesAsync();
}