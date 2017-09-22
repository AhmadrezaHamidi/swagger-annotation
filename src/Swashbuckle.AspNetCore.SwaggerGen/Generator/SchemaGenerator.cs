using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public class SchemaGenerator : ISchemaProvider
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IContractResolver _jsonContractResolver;
        private readonly SchemaRegistrySettings _settings;
        private readonly IDictionary<Type, string> _typeSchemaIdMap;

        public SchemaGenerator(
            JsonSerializerSettings jsonSerializerSettings,
            SchemaRegistrySettings settings = null)
        {
            _jsonSerializerSettings = jsonSerializerSettings;
            _jsonContractResolver = _jsonSerializerSettings.ContractResolver ?? new DefaultContractResolver();
            _settings = settings ?? new SchemaRegistrySettings();

            // Bar config/setting, it would be ideal for this generator to be stateless.
            // But, schemaIds need to be unique and with the current implementation, this feels like
            // the most appropriate place to enforce that for now.
            _typeSchemaIdMap = new Dictionary<Type, string>();
        }

        public Schema GetSchema(Type type, IDictionary<string, Schema> definitions)
        {
            var typeQueue = new Queue<Type>();
            var schema = CreateSchema(type, definitions, typeQueue);

            // Ensure all referenced types have a corresponding definition
            while (typeQueue.Any())
            {
                var referencedType = typeQueue.Peek();
                var schemaId = _settings.SchemaIdSelector(referencedType);

                if (!definitions.ContainsKey(schemaId))
                    definitions[schemaId] = CreateInlineSchema(referencedType, definitions, typeQueue);

                typeQueue.Dequeue();
            }

            return schema;
        }

        private Schema CreateSchema(Type type, IDictionary<string, Schema> definitions, Queue<Type> typeQueue)
        {
            var jsonContract = _jsonContractResolver.ResolveContract(type);

            var createReference = !_settings.CustomTypeMappings.ContainsKey(type)
                && type != typeof(object)
                && (jsonContract is JsonObjectContract || jsonContract.IsSelfReferencingArrayOrDictionary());

            return createReference
                ? CreateReferenceSchema(type, definitions, typeQueue)
                : CreateInlineSchema(type, definitions, typeQueue);
        }

        private Schema CreateReferenceSchema(Type type, IDictionary<string, Schema> definitions, Queue<Type> typeQueue)
        {
            string schemaId;
            if (!_typeSchemaIdMap.TryGetValue(type, out schemaId))
            {
                schemaId = _settings.SchemaIdSelector(type);

                // Raise an exception if there's another type registered with same schemaId
                if (_typeSchemaIdMap.Any(entry => entry.Value == schemaId))
                    throw new InvalidOperationException(string.Format(
                        "Conflicting schemaIds: Identical schemaIds detected for types {0} and {1}. " +
                        "See config settings - \"UseFullTypeNameInSchemaIds\" or \"CustomSchemaIds\" for a workaround",
                        type.FullName, _typeSchemaIdMap.First(entry => entry.Value == schemaId).Key));

                _typeSchemaIdMap.Add(type, schemaId);
            }

            // If not already there, queue the type for subsequent generation and addition to the definitions dictionary
            if (!typeQueue.Contains(type)) typeQueue.Enqueue(type);

            return new Schema { Ref = "#/definitions/" + schemaId };
        }

        private Schema CreateInlineSchema(Type type, IDictionary<string, Schema> definitions, Queue<Type> typeQueue)
        {
            Schema schema;

            var jsonContract = _jsonContractResolver.ResolveContract(type);

            if (_settings.CustomTypeMappings.ContainsKey(type))
            {
                schema = _settings.CustomTypeMappings[type]();
            }
            else
            {
                // TODO: Perhaps a "Chain of Responsibility" would clean this up a little?
                if (jsonContract is JsonPrimitiveContract)
                    schema = CreatePrimitiveSchema((JsonPrimitiveContract)jsonContract);
                else if (jsonContract is JsonDictionaryContract)
                    schema = CreateDictionarySchema((JsonDictionaryContract)jsonContract, definitions, typeQueue);
                else if (jsonContract is JsonArrayContract)
                    schema = CreateArraySchema((JsonArrayContract)jsonContract, definitions, typeQueue);
                else if (jsonContract is JsonObjectContract && type != typeof(object))
                    schema = CreateObjectSchema((JsonObjectContract)jsonContract, definitions, typeQueue);
                else
                    // None of the above, fallback to abstract "object"
                    schema = new Schema { Type = "object" };
            }

            var filterContext = new SchemaFilterContext(type, jsonContract, this, definitions);
            foreach (var filter in _settings.SchemaFilters)
            {
                filter.Apply(schema, filterContext);
            }

            return schema;
        }

        private Schema CreatePrimitiveSchema(JsonPrimitiveContract primitiveContract)
        {
            var type = Nullable.GetUnderlyingType(primitiveContract.UnderlyingType)
                ?? primitiveContract.UnderlyingType;

            if (type.GetTypeInfo().IsEnum)
                return CreateEnumSchema(primitiveContract, type);

            if (PrimitiveTypeMap.ContainsKey(type))
                return PrimitiveTypeMap[type]();

            // None of the above, fallback to string
            return new Schema { Type = "string" };
        }

        private Schema CreateEnumSchema(JsonPrimitiveContract primitiveContract, Type type)
        {
            var stringEnumConverter = primitiveContract.Converter as StringEnumConverter
                ?? _jsonSerializerSettings.Converters.OfType<StringEnumConverter>().FirstOrDefault();

            if (_settings.DescribeAllEnumsAsStrings || stringEnumConverter != null)
            {
                var camelCase = _settings.DescribeStringEnumsInCamelCase
                    || (stringEnumConverter != null && stringEnumConverter.CamelCaseText);

                return new Schema
                {
                    Type = "string",
                    Enum = (camelCase)
                        ? Enum.GetNames(type).Select(name => name.ToCamelCase()).ToArray()
                        : Enum.GetNames(type)
                };
            }

            return new Schema
            {
                Type = "integer",
                Format = "int32",
                Enum = Enum.GetValues(type).Cast<object>().ToArray()
            };
        }

        private Schema CreateDictionarySchema(
            JsonDictionaryContract dictionaryContract,
            IDictionary<string, Schema> definitions,
            Queue<Type> typeQueue)
        {
            var keyType = dictionaryContract.DictionaryKeyType ?? typeof(object);
            var valueType = dictionaryContract.DictionaryValueType ?? typeof(object);

            if (keyType.GetTypeInfo().IsEnum)
            {
                return new Schema
                {
                    Type = "object",
                    Properties = Enum.GetNames(keyType).ToDictionary(
                        (name) => dictionaryContract.DictionaryKeyResolver(name),
                        (name) => CreateSchema(valueType, definitions, typeQueue)
                    )
                };
            }
            else
            {
                return new Schema
                {
                    Type = "object",
                    AdditionalProperties = CreateSchema(valueType, definitions, typeQueue)
                };
            }
        }

        private Schema CreateArraySchema(
            JsonArrayContract arrayContract,
            IDictionary<string, Schema> definitions,
            Queue<Type> typeQueue)
        {
            var itemType = arrayContract.CollectionItemType ?? typeof(object);
            return new Schema
            {
                Type = "array",
                Items = CreateSchema(itemType, definitions, typeQueue)
            };
        }

        private Schema CreateObjectSchema(
            JsonObjectContract jsonContract,
            IDictionary<string, Schema> definitions,
            Queue<Type> typeQueue)
        {
            var properties = jsonContract.Properties
                .Where(p => !p.Ignored)
                .Where(p => !(_settings.IgnoreObsoleteProperties && p.IsObsolete()))
                .ToDictionary(
                    prop => prop.PropertyName,
                    prop => CreateSchema(prop.PropertyType, definitions, typeQueue).AssignValidationProperties(prop)
                );

            var required = jsonContract.Properties.Where(prop => prop.IsRequired())
                .Select(propInfo => propInfo.PropertyName)
                .ToList();

            var schema = new Schema
            {
                Required = required.Any() ? required : null, // required can be null but not empty
                Properties = properties,
                Type = "object",
                Title = jsonContract.UnderlyingType.FriendlyId()
            };

            return schema;
        }

        private static readonly Dictionary<Type, Func<Schema>> PrimitiveTypeMap = new Dictionary<Type, Func<Schema>>
        {
            { typeof(short), () => new Schema { Type = "integer", Format = "int32" } },
            { typeof(ushort), () => new Schema { Type = "integer", Format = "int32" } },
            { typeof(int), () => new Schema { Type = "integer", Format = "int32" } },
            { typeof(uint), () => new Schema { Type = "integer", Format = "int32" } },
            { typeof(long), () => new Schema { Type = "integer", Format = "int64" } },
            { typeof(ulong), () => new Schema { Type = "integer", Format = "int64" } },
            { typeof(float), () => new Schema { Type = "number", Format = "float" } },
            { typeof(double), () => new Schema { Type = "number", Format = "double" } },
            { typeof(decimal), () => new Schema { Type = "number", Format = "double" } },
            { typeof(byte), () => new Schema { Type = "string", Format = "byte" } },
            { typeof(sbyte), () => new Schema { Type = "string", Format = "byte" } },
            { typeof(byte[]), () => new Schema { Type = "string", Format = "byte" } },
            { typeof(sbyte[]), () => new Schema { Type = "string", Format = "byte" } },
            { typeof(bool), () => new Schema { Type = "boolean" } },
            { typeof(DateTime), () => new Schema { Type = "string", Format = "date-time" } },
            { typeof(DateTimeOffset), () => new Schema { Type = "string", Format = "date-time" } },
            { typeof(Guid), () => new Schema { Type = "string", Format = "uuid" } }
        };
    }
}