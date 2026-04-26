using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common;

public struct SaveInfo
{
    public const string FILE_EXTENSION = ".json";
    
    public SaveId SaveId { get; set; }
    public string Name { get; set; }
    public string CheckedOutByUserName { get; set; }
    public DateTime CheckedOutAt { get; set; }
    public string LastSyncedByUserName { get; set; }
    public DateTime LastSyncedAt { get; set; }
    
    public string Serialize()
    {
        JObject obj = JObject.FromObject(this);
        return JsonConvert.SerializeObject(this);
    }
    
    public static Result<SaveInfo> Deserialize(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<SaveInfo>(json);
        }
        catch (Exception e)
        {
            return Result<SaveInfo>.Failure(e.Message);
        }
    }

    public override string ToString()
    {
        return
            $"{nameof(SaveId)}: {SaveId}, {nameof(Name)}: {Name}, {nameof(CheckedOutByUserName)}: {CheckedOutByUserName}, {nameof(CheckedOutAt)}: {CheckedOutAt}, {nameof(LastSyncedByUserName)}: {LastSyncedByUserName}, {nameof(LastSyncedAt)}: {LastSyncedAt}";
    }
}

[JsonConverter(typeof(SaveIdConverter))]
public readonly struct SaveId(Guid id) : IEquatable<SaveId>
{
    private readonly Guid _id = id;

    public override string ToString()
    {
        return _id.ToString();
    }
    
    public bool Equals(SaveId other)
    {
        return _id.Equals(other._id);
    }

    public override bool Equals(object? obj)
    {
        return obj is SaveId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }
    
    public static SaveId Parse(string id) => new(Guid.Parse(id));
    public static SaveId NewSaveId() => Guid.NewGuid();

    public static implicit operator SaveId(Guid id) => new(id);
    public static bool operator ==(SaveId left, SaveId right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(SaveId left, SaveId right)
    {
        return !left.Equals(right);
    }
}

public class SaveIdConverter : JsonConverter<SaveId>
{
    public override void WriteJson(JsonWriter writer, SaveId value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    public override SaveId ReadJson(JsonReader reader, Type objectType, SaveId existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        return SaveId.Parse((string)reader.Value!);
    }
}
