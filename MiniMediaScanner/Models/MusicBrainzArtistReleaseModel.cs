using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models;

public class MusicBrainzArtistReleaseModel
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    
    [JsonPropertyName("status-id")]
    public string? StatusId { get; set; }
    
    public string? Date { get; set; }
    public string? Barcode { get; set; }
    public string? Country { get; set; }
    public string? Disambiguation { get; set; }
    public string? Quality { get; set; }
    
    public List<MusicBrainzReleaseMediaModel>? Media { get; set; }
}