namespace BLSShared.Models;

public class Consts
{
	public const string HttpClientName = "BLSHttpClient";
	public const string HttpClientFlightName = "BLSHttpFlight";
	public const string HttpClientQRScanerName = "BLSHttpQRScaner";
	public static string Cookies { get; set; }
	public static List<string> ServiceTypes =>
	[
		"Converse Bank",
		"Priority Pass",
		"Lounge Key",
		"Voucher Holder",
		"Dragon Pass",
		"Diners Club",
		"From VIP",
		"Lounge Me",
		"Online Payment",
		"ONPASS",
		"Lounge Pass",
		"FT+BL via Converse",
		"Grey Well",
		"Comfort Pass",
		"Every Lounge",
		"Dreamfolks",
		"Persona.aero",
		"Golden Key",
		"Ruspriority",
		"Cash Payment",
		"Payment by Card"
	];

	public static List<string> Status =>
	[
		"Complete",
		"In time",
		"Overtime"
	];

	public static List<FieldSetting> DefaultFieldSettings =>
		[
			new FieldSetting { FieldLabel = nameof(Passenger.RowId), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.UserId), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.ServiceType), ShowInGrid = false, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.NameSurname), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.Airline), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.Status), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.Prolongation), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.FromTime), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.ToTime), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.FlightDate), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.CardNumber), ShowInGrid = false, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.TicketNumber), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.GuestsQuantity), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.PaxId), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.Notes), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.GuestAge), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.TransactionNumber), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.SeatNumber), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.VoucherNumber), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.ChildAge), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.DeclinedCode), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.Amount), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.CBChild), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.PassportNumber), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.ReceiptNumber), ShowInGrid = true, ShowInSearch = true, ShowInView = true },
			new FieldSetting { FieldLabel = nameof(Passenger.PassRef), ShowInGrid = true, ShowInSearch = true, ShowInView = true }
		];
}
