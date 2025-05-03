using System.ComponentModel.DataAnnotations;

namespace BLSShared.Models;

public class Passenger : IValidatableObject
{
    public long RowId { get; set; }
    public Guid UserId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string NameSurname { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Prolongation { get; set; }
    public DateTime? FromTime { get; set; }
    public DateTime? ToTime { get; set; }
    public DateTime? FlightDate { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public int GuestsQuantity { get; set; }
    public int PaxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int GuestAge { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string VoucherNumber { get; set; } = string.Empty;
    public int ChildAge { get; set; }
    public string DeclinedCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool CBChild { get; set; }
    public string PassportNumber { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string PassRef { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsNotifyed { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool ShowDetails { get; set; }
    public List<ProlongationDto> Prolongations { get; set; } = new List<ProlongationDto>();

    public ProlongationDto GetProlongationDto() => new()
    {
        RowId = RowId,
        UserId = UserId,
        ServiceType = ServiceType,
        Status = Status,
        FromTime = FromTime,
        ToTime = ToTime,
        FlightDate = FlightDate,
        CardNumber = CardNumber,
        GuestQuantity = GuestsQuantity,
        Notes = Notes,
        GuestAge = GuestAge,
        TransactionNumber = TransactionNumber,
        SeatNumber = SeatNumber,
        VoucherNumber = VoucherNumber,
        ChildAge = ChildAge,
        Amount = Amount,
        CBChild = CBChild,
        PassRef = PassRef
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NameSurname))
        {
            yield return new ValidationResult("Name Surname is required", [nameof(NameSurname)]);
        }

        if (string.IsNullOrWhiteSpace(Airline))
        {
            yield return new ValidationResult("Airline is required", [nameof(Airline)]);
        }

        if (string.IsNullOrWhiteSpace(Status))
        {
            yield return new ValidationResult("Status is required", [nameof(Status)]);
        }

        if (!FromTime.HasValue)
        {
            yield return new ValidationResult("From Time is required", [nameof(FromTime)]);
        }

        if (!ToTime.HasValue)
        {
            yield return new ValidationResult("To Time is required", [nameof(ToTime)]);
        }

        if (!FlightDate.HasValue)
        {
            yield return new ValidationResult("Flight Date is required", [nameof(FlightDate)]);
        }

        if (ServiceType == "Voucher Holder" && string.IsNullOrWhiteSpace(VoucherNumber))
        {
            yield return new ValidationResult("Voucher Number is required", [nameof(VoucherNumber)]);
        }
    }
}
