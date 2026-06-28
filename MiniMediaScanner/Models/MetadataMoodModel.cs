namespace MiniMediaScanner.Models;

public class MetadataMoodModel
{
    public Guid MetadataId { get; set; }
    public string Mood_Happy { get; set; }
    public string Mood_Sad { get; set; }
    public string Mood_Aggressive { get; set; }
    public string Mood_Relaxed { get; set; }
    public string Mood_Acoustic { get; set; }
    public string Mood_Electronic { get; set; }
    public string Mood_Party { get; set; }
    public string Ability_Approach { get; set; }
    public string Ability_Dance { get; set; }
    public string Voice_Instrumental { get; set; }
    public string Timbre { get; set; }
    public string Engagement_3c { get; set; }
    public string Engagement_Regression { get; set; }
    public string Gender { get; set; }
    public string Genre_Json { get; set; }
}