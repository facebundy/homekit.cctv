namespace homekit.cctv;

public class AppConfig
{
    public string RootUrl { get; set; }
    public string VideoUrl { get; set; }
    public string AudioUrl { get; set; }
    public long AutoDeleteInterval { get; set; }
    public long MaxSizeLimitMb { get; set; }
}