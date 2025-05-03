namespace BLSShared.Models;

public class PagedFilterDto
{
	public int PageNumber { get; set; }
	public int PageSize { get; set; }
	public string SortColumn { get; set; }
	public FilterDto Filter{ get; set; }
}

public class PagedDto
{
	public int PageNumber { get; set; }
	public int PageSize { get; set; }
	public long TotalCount { get; set; }
	public List<Passenger> Items { get; set; } = [];
	public string SortColumn { get; set; }
}
