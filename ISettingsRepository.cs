using BLSShared.Models;

namespace BLSWebAPI.Services;

public interface ISettingsRepository
{
    Task<Settings> GetSettingsAsync(long id);
    Task<int> AddSettingsAsync(Settings settings);
}
