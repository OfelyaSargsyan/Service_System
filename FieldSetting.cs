namespace BLSShared.Models;

public class FieldSetting
{
    public long UserId { get; set; }
    public string FieldLabel { get; set; }
    public bool ShowInGrid { get; set; }
    public bool ShowInSearch { get; set; }
    public bool ShowInView { get; set; }
}
