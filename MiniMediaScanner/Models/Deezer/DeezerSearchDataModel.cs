namespace MiniMediaScanner.Models.Deezer;

public class DeezerSearchDataModel<T>
{
    public List<T>? Data { get; set; }
    public int? Total { get; set; }
    public string? Next { get; set; }
    public string? Prev { get; set; }
    public DeezerErrorModel? Error { get; set; }
}