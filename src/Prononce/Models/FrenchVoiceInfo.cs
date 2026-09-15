namespace Prononce.Models;

/// <summary>
/// Represents an installed French speech synthesis voice.
/// </summary>
public class FrenchVoiceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Language { get; set; } = "fr-FR";
    public string Region { get; set; } = "France";
    public string Gender { get; set; } = "Female";
    public bool IsDefault { get; set; }

    public override string ToString() => DisplayName;
}
