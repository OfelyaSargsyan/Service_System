public class RootObject
{
	public Result Result { get; set; }
	public object TargetUrl { get; set; }
	public bool Success { get; set; }
	public object Error { get; set; }
	public bool UnAuthorizedRequest { get; set; }
	public bool Abp { get; set; }
}

public class Result
{
	public int TotalCount { get; set; }
	public List<Item> Items { get; set; }
}

public class OneRootObject
{
	public Item Result { get; set; }
	public object TargetUrl { get; set; }
	public bool Success { get; set; }
	public object Error { get; set; }
	public bool UnAuthorizedRequest { get; set; }
	public bool Abp { get; set; }
}

public class Item
{
	public int FlightType { get; set; }
	public AirlineData AirlineData { get; set; }
	public AirportData AirportData { get; set; }
	public FlightStatusData FlightStatusData { get; set; }
	public string FlightNumber { get; set; }
	public DateTime ProgrammedTime { get; set; }
	public DateTime? EstimatedTime { get; set; }
	public object DepartedTime { get; set; }
	public string FlightStatus { get; set; }
	public object TerminalFrom { get; set; }
	public object TerminalTo { get; set; }
	public string CodeShare { get; set; }
	public string CompositeId { get; set; }
	public string Id { get; set; }
}

public class AirlineData
{
	public string Code { get; set; }
	public string LogoUrl { get; set; }
	public bool HideInFilter { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string WebSiteUrl { get; set; }
	public int Id { get; set; }
}

public class AirportData
{
	public string IataCode { get; set; }
	public string IcaoCode { get; set; }
	public float Latitude { get; set; }
	public float Longitude { get; set; }
	public City City { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int Id { get; set; }
}

public class City
{
	public string IataCode { get; set; }
	public Country Country { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int Id { get; set; }
}

public class Country
{
	public string IsoCode { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int Id { get; set; }
}

public class FlightStatusData
{
	public string Code { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int Id { get; set; }
}
