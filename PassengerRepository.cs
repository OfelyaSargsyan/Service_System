using BLSShared.Models;
using NLog;

namespace BLSWebAPI.Services;

public class PassengerRepository : IPassengersRepository
{
    private readonly DbService _dbService;
    private Logger _logger { get; } = LogManager.GetCurrentClassLogger();

    public PassengerRepository(DbService dbService)
    {
        _dbService = dbService;
    }

    public async Task<Passenger> GetPassengerAsync(long id)
    {
        return await _dbService.GetPassengerAsync(id);
    }

    public async Task<List<Passenger>> GetPassengersAsync(Guid userId)
    {
        return await _dbService.GetPassengersAsync(userId);
    }

    public async Task<int> AddPassengerAsync(Passenger passenger)
    {
        passenger.UserId = Guid.NewGuid();
        try
        {
            return await _dbService.AddPassengerAsync(passenger);
        }
        catch (Exception ex)
        {
			_logger.Error(ex);
			return 0;
        }
    }

    public async Task<int> DeletePassengerAsync(Guid id)
    {
        try
        {
            return await _dbService.DeletePassengerAsync(id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return 0;
        }
    }

    public async Task<int> DeleteByRowIDAsync(long id)
    {
        try
        {
            return await _dbService.DeleteByRowIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return 0;
        }
    }


    public async Task<int> UpdatePassengerAsync(Passenger passenger)
    {
        Console.WriteLine("Updating passenger with ID: " + passenger.RowId);
        try
        {
            return await _dbService.UpdatePassengerAsync(passenger);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return 0;
        }
    }

    public async Task<int> UpdateProlongAsync(ProlongationDto passenger)
    {
        try
        {
            return await _dbService.UpdateProlongAsync(passenger);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return 0;
        }
    }

    public async Task<int> AddProlongationDataAsync(ProlongationDto prolong)
    {
        try
        {
            var passenger = await _dbService.GetLastPassengerAsync(prolong.UserId);
            passenger.ServiceType = prolong.ServiceType;
            passenger.Status = prolong.Status;
            passenger.FromTime = prolong.FromTime;
            passenger.ToTime = prolong.ToTime;
            passenger.FlightDate = prolong.FlightDate;
            passenger.CardNumber = prolong.CardNumber;
            passenger.GuestsQuantity = prolong.GuestQuantity;
            passenger.Notes = prolong.Notes;
            passenger.GuestAge = prolong.GuestAge;
            passenger.TransactionNumber = prolong.TransactionNumber;
            passenger.SeatNumber = prolong.SeatNumber;
            passenger.VoucherNumber = prolong.VoucherNumber;
            passenger.ChildAge = prolong.ChildAge;
            passenger.Amount = prolong.Amount;
            passenger.CBChild = prolong.CBChild;
            passenger.PassRef = prolong.PassRef;
            passenger.Prolongation++;
            return await _dbService.AddPassengerAsync(passenger);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return 0;
        }
    }

    public async Task<PagedDto> GetPagedPassengersAsync(PagedFilterDto filter)
    {
        return await _dbService.GetPagedPassengersAsync(filter);
    }

    public async Task<int> SetPassengerNotifyedAsync(long id)
    {
        try
        {
            return await _dbService.SetPassengerNotifyedAsync(id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return 0;
        }
    }

    public async Task<List<Passenger>> GetExpiredPassengersAsync(bool isNotified)
    {
        return await _dbService.GetExpiredPassengersAsync(isNotified);
    }   

}

