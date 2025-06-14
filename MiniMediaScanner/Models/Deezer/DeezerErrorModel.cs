namespace MiniMediaScanner.Models.Deezer;

public class DeezerErrorModel
{
    private readonly int[] RateLimiterErrorCodes = new int[]
    {
        4, //Quota
        700 //Service busy
    };
    
    public string Type { get; set; }
    public string Message { get; set; }
    public int Code { get; set; }

    public void ThrowExceptionOnRateLimiter()
    {
        if (RateLimiterErrorCodes.Contains(Code))
        {
            throw new HttpRequestException();
        }
    }
}