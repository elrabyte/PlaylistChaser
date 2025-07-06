using System.Text.Json.Serialization;

namespace PlaylistChaser.Model.BuiltInIds
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SourceId
    {
        Youtube = 1,
        Spotify = 2
    }
}
