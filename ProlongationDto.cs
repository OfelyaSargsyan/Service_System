using System.ComponentModel.DataAnnotations;

namespace BLSShared.Models;

public class ProlongationDto
{
    public long RowId { get; set; }
    public Guid UserId { get; set; }

	[Required(ErrorMessage = "Service Type is required")]
	public string ServiceType { get; set; } = string.Empty;

	[Required(ErrorMessage = "Status is required")]
	public string Status { get; set; } = string.Empty;

	[Required(ErrorMessage = "From Time is required")]
	public DateTime? FromTime { get; set; }

	[Required(ErrorMessage = "To Time is required")]
	public DateTime? ToTime { get; set; }

	[Required(ErrorMessage = "Flight Date is required")]
	public DateTime? FlightDate { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public int GuestQuantity { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int GuestAge { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string VoucherNumber { get; set; } = string.Empty;
    public int ChildAge { get; set; }
    public decimal Amount { get; set; }
    public bool CBChild { get; set; }
    public string PassRef { get; set; } = string.Empty;
}
