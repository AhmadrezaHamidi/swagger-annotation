using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Xunit;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen.Test
{
    public class SchemaGeneratorTests
    {
        [Theory]
        [InlineData(typeof(short), "integer", "int32")]
        [InlineData(typeof(ushort), "integer", "int32")]
        [InlineData(typeof(int), "integer", "int32")]
        [InlineData(typeof(uint), "integer", "int32")]
        [InlineData(typeof(long), "integer", "int64")]
        [InlineData(typeof(ulong), "integer", "int64")]
        [InlineData(typeof(float), "number", "float")]
        [InlineData(typeof(double), "number", "double")]
        [InlineData(typeof(decimal), "number", "double")]
        [InlineData(typeof(byte), "string", "byte")]
        [InlineData(typeof(sbyte), "string", "byte")]
        [InlineData(typeof(byte[]), "string", "byte")]
        [InlineData(typeof(bool), "boolean", null)]
        [InlineData(typeof(DateTime), "string", "date-time")]
        [InlineData(typeof(DateTimeOffset), "string", "date-time")]
        [InlineData(typeof(Guid), "string", "uuid")]
        [InlineData(typeof(string), "string", null)]
        public void GetSchema_ReturnsPrimitiveSchema_ForSimpleTypes(
            Type systemType,
            string expectedType,
            string expectedFormat)
        {
            var definitions = new Dictionary<string, Schema>();
            var schema = Subject().GetSchema(systemType, definitions);

            Assert.Equal(expectedType, schema.Type);
            Assert.Equal(expectedFormat, schema.Format);
        }

        [Fact]
        public void GetSchema_ReturnsEnumSchema_ForEnumTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            var schema = Subject().GetSchema(typeof(AnEnum), definitions);

            Assert.Equal("integer", schema.Type);
            Assert.Equal("int32", schema.Format);
            Assert.Contains(AnEnum.Value1, schema.Enum);
            Assert.Contains(AnEnum.Value2, schema.Enum);
        }

        [Theory]
        [InlineData(typeof(int[]), "integer", "int32")]
        [InlineData(typeof(IEnumerable<string>), "string", null)]
        [InlineData(typeof(IEnumerable), "object", null)]
        public void GetSchema_ReturnsArraySchema_ForEnumerableTypes(
            Type systemType,
            string expectedItemsType,
            string expectedItemsFormat)
        {
            var definitions = new Dictionary<string, Schema>();
            var schema = Subject().GetSchema(systemType, definitions);

            Assert.Equal("array", schema.Type);
            Assert.Equal(expectedItemsType, schema.Items.Type);
            Assert.Equal(expectedItemsFormat, schema.Items.Format);
        }

        [Fact]
        public void GetSchema_ReturnsMapSchema_ForDictionaryTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            var schema = Subject().GetSchema(typeof(Dictionary<string, string>), definitions);

            Assert.Equal("object", schema.Type);
            Assert.Equal("string", schema.AdditionalProperties.Type);
        }

        [Fact]
        public void GetSchema_ReturnsObjectSchema_ForDictionarTypesWithEnumKeys()
        {
            var definitions = new Dictionary<string, Schema>();
            var schema = Subject().GetSchema(typeof(Dictionary<AnEnum, string>), definitions);

            Assert.Equal("object", schema.Type);
            Assert.NotNull(schema.Properties);
            Assert.Equal("string", schema.Properties["Value1"].Type);
            Assert.Equal("string", schema.Properties["Value2"].Type);
            Assert.Equal("string", schema.Properties["X"].Type);
        }

        [Fact]
        public void GetSchema_ReturnsRefSchema_ForComplexTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            var reference = Subject().GetSchema(typeof(ComplexType), definitions);

            Assert.Equal("#/definitions/ComplexType", reference.Ref);
            Assert.NotNull(definitions["ComplexType"]);
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(JToken))]
        [InlineData(typeof(JToken))]
        public void GetSchema_ReturnsEmptyObjectSchema_ForAmbiguousTypes(Type systemType)
        {
            var definitions = new Dictionary<string, Schema>();

            var schema = Subject().GetSchema(systemType, definitions);

            Assert.Equal("object", schema.Type);
            Assert.Null(schema.Properties);
        }

        [Fact]
        public void GetSchema_DefinesObjectSchema_ForComplexTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(ComplexType), definitions);

            var schema = definitions["ComplexType"];
            Assert.NotNull(schema);
            Assert.Equal("boolean", schema.Properties["Property1"].Type);
            Assert.Null(schema.Properties["Property1"].Format);
            Assert.Equal("string", schema.Properties["Property2"].Type);
            Assert.Equal("date-time", schema.Properties["Property2"].Format);
            Assert.Equal("string", schema.Properties["Property3"].Type);
            Assert.Equal("date-time", schema.Properties["Property3"].Format);
            Assert.Equal("string", schema.Properties["Property4"].Type);
            Assert.Null(schema.Properties["Property4"].Format);
            Assert.Equal("string", schema.Properties["Property5"].Type);
            Assert.Null(schema.Properties["Property5"].Format);
        }

        [Fact]
        public void GetSchema_IncludesInheritedProperties_ForSubTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(SubType), definitions);

            var schema = definitions["SubType"];
            Assert.Equal("string", schema.Properties["BaseProperty"].Type);
            Assert.Null(schema.Properties["BaseProperty"].Format);
            Assert.Equal("integer", schema.Properties["SubTypeProperty"].Type);
            Assert.Equal("int32", schema.Properties["SubTypeProperty"].Format);
        }

        [Fact]
        public void GetSchema_IncludesTypedProperties_ForDynamicObjectSubTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(DynamicObjectSubType), definitions);

            var schema = definitions["DynamicObjectSubType"];
            Assert.Equal(1, schema.Properties.Count);
            Assert.Equal("string", schema.Properties["Property1"].Type);
        }

        [Fact]
        public void GetSchema_IgnoresIndexerProperties_ForIndexedTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(IndexedType), definitions);

            var schema = definitions["IndexedType"];
            Assert.Equal(1, schema.Properties.Count);
            Assert.Contains("Property1", schema.Properties.Keys);
        }

        [Fact]
        public void GetSchema_HonorsJsonAttributes()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(JsonAnnotatedType), definitions);

            var schema = definitions["JsonAnnotatedType"];
            Assert.Equal(2, schema.Properties.Count);
            Assert.Contains("foobar", schema.Properties.Keys);
            Assert.Equal(new[] { "Property3" }, schema.Required.ToArray());
        }

        [Fact]
        public void GetSchema_HonorsDataAttributes()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(DataAnnotatedType), definitions);

            var schema = definitions["DataAnnotatedType"];
            Assert.Equal(1, schema.Properties["RangeProperty"].Minimum);
            Assert.Equal(12, schema.Properties["RangeProperty"].Maximum);
            Assert.Equal("^[3-6]?\\d{12,15}$", schema.Properties["PatternProperty"].Pattern);
            Assert.Equal(5, schema.Properties["StringProperty1"].MinLength);
            Assert.Equal(10, schema.Properties["StringProperty1"].MaxLength);
            Assert.Equal(1, schema.Properties["StringProperty2"].MinLength);
            Assert.Equal(3, schema.Properties["StringProperty2"].MaxLength);
            Assert.Equal("^[3-6]?\\d{12,15}$", schema.Properties["PatternProperty"].Pattern);
            Assert.Equal(new[] { "RangeProperty", "PatternProperty" }, schema.Required.ToArray());
            Assert.Equal("DefaultValue", schema.Properties["DefaultValueProperty"].Default);
        }

        [Fact]
        public void GetSchema_HonorsDataAttributes_ViaModelMetadataType()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(MetadataAnnotatedType), definitions);

            var schema = definitions["MetadataAnnotatedType"];
            Assert.Equal(1, schema.Properties["RangeProperty"].Minimum);
            Assert.Equal(12, schema.Properties["RangeProperty"].Maximum);
            Assert.Equal("^[3-6]?\\d{12,15}$", schema.Properties["PatternProperty"].Pattern);
            Assert.Equal(new[] { "RangeProperty", "PatternProperty" }, schema.Required.ToArray());
        }
 

        [Fact]
        public void GetSchema_HonorsStringEnumConverters_ConfiguredViaAttributes()
        {
            var definitions = new Dictionary<string, Schema>();
            var schema = Subject().GetSchema(typeof(JsonConvertedEnum), definitions);

            Assert.Equal("string", schema.Type);
            Assert.Equal(new[] { "Value1", "Value2", "X" }, schema.Enum);
        }

        [Fact]
        public void GetSchema_HonorsStringEnumConverters_ConfiguredViaSerializerSettings()
        {
            var subject = Subject(new JsonSerializerSettings
            {
                Converters = new[] { new StringEnumConverter { CamelCaseText = true } }
            });

            var definitions = new Dictionary<string, Schema>();
            var schema = subject.GetSchema(typeof(AnEnum), definitions);

            Assert.Equal("string", schema.Type);
            Assert.Equal(new[] { "value1", "value2", "x" }, schema.Enum);
        }

        [Fact]
        public void GetSchema_SupportsOptionToExplicitlyMapTypes()
        {
            var subject = Subject(c =>
                c.CustomTypeMappings.Add(typeof(ComplexType), () => new Schema { Type = "string" })
            );

            var definitions = new Dictionary<string, Schema>();
            var schema = subject.GetSchema(typeof(ComplexType), definitions);

            Assert.Equal("string", schema.Type);
            Assert.Null(schema.Properties);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(IDictionary<string, string>))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(ComplexType))]
        [InlineData(typeof(object))]
        public void GetSchema_SupportsOptionToPostModifySchemas(Type systemType)
        {
            var subject = Subject(c =>
                c.SchemaFilters.Add(new VendorExtensionsSchemaFilter())
            );

            var definitions = new Dictionary<string, Schema>();
            var schemaOrRef = subject.GetSchema(systemType, definitions);

            var schema = (schemaOrRef.Ref == null)
                ? schemaOrRef
                : definitions[schemaOrRef.Ref.Replace("#/definitions/", "")];

            Assert.True(schema.Extensions.ContainsKey("X-property1"));
        }

        [Fact]
        public void GetSchema_SupportsOptionToIgnoreObsoleteProperties()
        {
            var subject = Subject(c => c.IgnoreObsoleteProperties = true);

            var definitions = new Dictionary<string, Schema>();
            subject.GetSchema(typeof(ObsoletePropertiesType), definitions);

            var schema = definitions["ObsoletePropertiesType"];
            Assert.DoesNotContain("ObsoleteProperty", schema.Properties.Keys);
        }

        [Fact]
        public void GetSchema_SupportsOptionToCustomizeSchemaIds()
        {
            var subject = Subject(c =>
            {
                c.SchemaIdSelector = (type) => type.FriendlyId(true).Replace("Swashbuckle.AspNetCore.SwaggerGen.Test.", "");
            });

            var definitions = new Dictionary<string, Schema>();
            var jsonReference1 = subject.GetSchema(typeof(Namespace1.ConflictingType), definitions);
            var jsonReference2 = subject.GetSchema(typeof(Namespace2.ConflictingType), definitions);

            Assert.Equal("#/definitions/Namespace1.ConflictingType", jsonReference1.Ref);
            Assert.Equal("#/definitions/Namespace2.ConflictingType", jsonReference2.Ref);
        }

        [Fact]
        public void GetSchema_SupportsOptionToDescribeAllEnumsAsStrings()
        {
            var subject = Subject(c => c.DescribeAllEnumsAsStrings = true);

            var definitions = new Dictionary<string, Schema>();
            var schema = subject.GetSchema(typeof(AnEnum), definitions);

            Assert.Equal("string", schema.Type);
            Assert.Equal(new[] { "Value1", "Value2", "X" }, schema.Enum);
        }

        [Fact]
        public void GetSchema_SupportsOptionToDescribeStringEnumsInCamelCase()
        {
            var subject = Subject(c =>
            {
                c.DescribeAllEnumsAsStrings = true;
                c.DescribeStringEnumsInCamelCase = true;
            });

            var definitions = new Dictionary<string, Schema>();
            var schema = subject.GetSchema(typeof(AnEnum), definitions);

            Assert.Equal("string", schema.Type);
            Assert.Equal(new[] { "value1", "value2", "x" }, schema.Enum);
        }

        [Fact]
        public void GetSchema_HandlesMultiDemensionalArrays()
        {
            var definitions = new Dictionary<string, Schema>();
            var schema = Subject().GetSchema(typeof(int[][]), definitions);

            Assert.Equal("array", schema.Type);
            Assert.Equal("array", schema.Items.Type);
            Assert.Equal("integer", schema.Items.Items.Type);
            Assert.Equal("int32", schema.Items.Items.Format);
        }

        [Fact]
        public void GetSchema_HandlesCompositeTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(CompositeType), definitions);

            var rootSchema = definitions["CompositeType"];
            Assert.NotNull(rootSchema);
            Assert.Equal("object", rootSchema.Type);
            Assert.Equal("#/definitions/ComplexType", rootSchema.Properties["Property1"].Ref);
            Assert.Equal("array", rootSchema.Properties["Property2"].Type);
            Assert.Equal("#/definitions/ComplexType", rootSchema.Properties["Property2"].Items.Ref);
            var componentSchema = definitions["ComplexType"];
            Assert.NotNull(componentSchema);
            Assert.Equal("object", componentSchema.Type);
            Assert.Equal(5, componentSchema.Properties.Count);
        }

        [Fact]
        public void GetSchema_HandlesNestedTypes()
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(typeof(ContainingType), definitions);

            var rootSchema = definitions["ContainingType"];
            Assert.NotNull(rootSchema);
            Assert.Equal("object", rootSchema.Type);
            Assert.Equal("#/definitions/NestedType", rootSchema.Properties["Property1"].Ref);
            var nestedSchema = definitions["NestedType"];
            Assert.NotNull(nestedSchema);
            Assert.Equal("object", nestedSchema.Type);
            Assert.Equal(1, nestedSchema.Properties.Count);
        }

        [Theory]
        [InlineData(typeof(SelfReferencingType), "SelfReferencingType")]
        [InlineData(typeof(ListOfSelf), "ListOfSelf")]
        [InlineData(typeof(DictionaryOfSelf), "DictionaryOfSelf")]
        public void GetSchema_HandlesSelfReferencingTypes(
            Type systemType,
            string expectedSchemaId)
        {
            var definitions = new Dictionary<string, Schema>();
            Subject().GetSchema(systemType, definitions);

            Assert.Contains(expectedSchemaId, definitions.Keys);
        }

        [Fact]
        public void GetSchema_HandlesRecursion_IfCalledAgainWithinAFilter()
        {
            var subject = Subject(c => c.SchemaFilters.Add(new RecursiveCallSchemaFilter()));

            var definitions = new Dictionary<string, Schema>();
            subject.GetSchema(typeof(object), definitions);
        }

        [Fact]
        public void GetSchema_Errors_OnConflictingClassName()
        {
            var subject = Subject();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var definitions = new Dictionary<string, Schema>();
                subject.GetSchema(typeof(Namespace1.ConflictingType), definitions);
                subject.GetSchema(typeof(Namespace2.ConflictingType), definitions);
            });
        }

        private SchemaGenerator Subject(Action<SchemaRegistrySettings> configure = null)
        {
            var settings = new SchemaRegistrySettings();
            if (configure != null) configure(settings);

            return new SchemaGenerator(new JsonSerializerSettings(), settings);
        }

        private SchemaGenerator Subject(JsonSerializerSettings jsonSerializerSettings)
        {
            return new SchemaGenerator(jsonSerializerSettings);
        }
    }
}