namespace BLSShared.Models;

public class SearchOutputModel
{
	public string CardholderName { get; set; }
	public string Phone { get; set; }
	public string Email { get; set; }
	public string OrderNumber { get; set; }
	public string ServiceType { get; set; }
	public string Amount { get; set; }
	public string PaymentStatus { get; set; }
	public string ServingStatus { get; set; }
	public bool IsServed { get; set; }
	public string Age { get; set; }
	public bool IsError { get; set; }
	public string ErrorMessage { get; set; }
}
