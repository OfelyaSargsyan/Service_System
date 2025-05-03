namespace BLSShared.Models;

public class FilterDto
{
	public string ServiceType { get; set; }
	public string NameSurname { get; set; }
	public string Airline { get; set; }
	public string Status { get; set; }
	public DateTime? FromTimeBegin { get; set; }
	public DateTime? FromTimeEnd { get; set; }
	public DateTime? ToTimeBegin { get; set; }
	public DateTime? ToTimeEnd { get; set; }
	public DateTime? FlightDateBegin { get; set; }
	public DateTime? FlightDateEnd { get; set; }
	public string CardNumber { get; set; }
	public string TicketNumber { get; set; }
	public int? GuestQuantity { get; set; }
	public int? PaxId { get; set; }
	public int? GuestAge { get; set; }
	public string TransactionNumber { get; set; }
	public string SeatNumber { get; set; }
	public string VoucherNumber { get; set; }
	public int? ChildAge { get; set; }
	public string DeclinedCode { get; set; }
	public decimal? Amount { get; set; }
	public string PassportNumber { get; set; }
	public string ReceiptNumber { get; set; }
	public string PassRef { get; set; }
}
