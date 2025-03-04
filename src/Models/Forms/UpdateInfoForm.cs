
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Herta.Models.Forms.UpdateInfoForm;

public class UpdateInfoForm
{
    public required int UserId { get; set; }

    [JsonConverter(typeof(DictionaryConverter))]
    public required Dictionary<string, object> UpdateInfo { get; set; }
}

public class DictionaryConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dictionary = new Dictionary<string, object>();
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(ref reader);
        foreach (var property in jsonElement.EnumerateObject())
        {
            dictionary[property.Name] = property.Value.ToString();
        }
        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
