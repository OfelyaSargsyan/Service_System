using BLSShared.Models;

namespace BLSWebAPI.Services;

public interface IPassengersRepository
{
    Task<int> AddProlongationDataAsync( ProlongationDto data);
    Task<Passenger> GetPassengerAsync(long id);
	Task<List<Passenger>> GetPassengersAsync(Guid userId);
    Task<int> AddPassengerAsync(Passenger passenger);
    Task<int> DeletePassengerAsync(Guid id);
    Task<int> DeleteByRowIDAsync(long id);
    Task<int> UpdatePassengerAsync(Passenger passenger);
	Task<int> UpdateProlongAsync(ProlongationDto data);
	Task<PagedDto> GetPagedPassengersAsync(PagedFilterDto filter);
    Task<int> SetPassengerNotifyedAsync(long id);
    Task<List<Passenger>> GetExpiredPassengersAsync(bool isNotified);
}
