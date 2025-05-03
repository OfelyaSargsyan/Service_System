using BLSShared.Models;
using System.Text.Json;

namespace BLSWebAPI.Services;

public class SettingsRepository : ISettingsRepository
{
	private readonly DbService _dbService;

	public SettingsRepository(DbService dbService)
	{
		_dbService = dbService;
	}

	public async Task<Settings> GetSettingsAsync(long id)
	{
		var res = await _dbService.GetSettingsAsync(id);
		if (res is null)
		{
			var gridSettingsMap = Consts.DefaultFieldSettings.ToDictionary(f => f.FieldLabel);
			return new Settings
			{
				EmployeeId = id,
				GridSettingsJson = JsonSerializer.Serialize(gridSettingsMap)
			};
		}

		return res;
	}

	public async Task<int> AddSettingsAsync(Settings settings)
	{
		return await _dbService.AddSettingsAsync(settings);
	}
}
