using System.Text.Json;
using System.Text.Json.Serialization;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Ecs.Serialization
{
    /// <summary>快照 JSON 的默认选项与转换器。</summary>
    public static class SnapshotJson
    {
        /// <summary>创建默认 JSON 选项。</summary>
        public static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            options.Converters.Add(new EntityJsonConverter());
            options.Converters.Add(new CartesianVector2DJsonConverter());
            options.Converters.Add(new CartesianDoubleVector2DJsonConverter());
            return options;
        }
    }

    /// <summary><see cref="Entity"/> 的 JSON 转换器。</summary>
    public sealed class EntityJsonConverter : JsonConverter<Entity>
    {
        /// <inheritdoc />
        public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Entity 必须是对象。");

            int index = 0;
            uint generation = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Entity(index, generation);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                string name = reader.GetString() ?? "";
                reader.Read();

                if (name.Equals("Index", StringComparison.OrdinalIgnoreCase))
                    index = reader.GetInt32();
                else if (name.Equals("Generation", StringComparison.OrdinalIgnoreCase))
                    generation = reader.GetUInt32();
                else
                    reader.Skip();
            }

            throw new JsonException("Entity 对象不完整。");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Index", value.Index);
            writer.WriteNumber("Generation", value.Generation);
            writer.WriteEndObject();
        }
    }

    /// <summary><see cref="CartesianVector2D"/> 的 JSON 转换器。</summary>
    public sealed class CartesianVector2DJsonConverter : JsonConverter<CartesianVector2D>
    {
        /// <inheritdoc />
        public override CartesianVector2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("CartesianVector2D 必须是对象。");

            float x = 0f;
            float y = 0f;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new CartesianVector2D(x, y);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                string name = reader.GetString() ?? "";
                reader.Read();

                if (name.Equals("X", StringComparison.OrdinalIgnoreCase))
                    x = reader.GetSingle();
                else if (name.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    y = reader.GetSingle();
                else
                    reader.Skip();
            }

            throw new JsonException("CartesianVector2D 对象不完整。");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, CartesianVector2D value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }

    /// <summary><see cref="CartesianDoubleVector2D"/> 的 JSON 转换器。</summary>
    public sealed class CartesianDoubleVector2DJsonConverter : JsonConverter<CartesianDoubleVector2D>
    {
        /// <inheritdoc />
        public override CartesianDoubleVector2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("CartesianDoubleVector2D 必须是对象。");

            double x = 0.0;
            double y = 0.0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new CartesianDoubleVector2D(x, y);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                string name = reader.GetString() ?? "";
                reader.Read();

                if (name.Equals("X", StringComparison.OrdinalIgnoreCase))
                    x = reader.GetDouble();
                else if (name.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    y = reader.GetDouble();
                else
                    reader.Skip();
            }

            throw new JsonException("CartesianDoubleVector2D 对象不完整。");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, CartesianDoubleVector2D value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }
}
