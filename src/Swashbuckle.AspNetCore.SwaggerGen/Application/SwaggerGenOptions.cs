﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public class SwaggerGenOptions
    {
        private readonly SwaggerGeneratorSettings _swaggerGeneratorSettings;
        private readonly SchemaRegistrySettings _schemaRegistrySettings;

        private IList<Func<XPathDocument>> _xmlDocFactories;
        private List<FilterDescriptor<IOperationFilter>> _operationFilterDescriptors;
        private List<FilterDescriptor<IDocumentFilter>> _documentFilterDescriptors;
        private List<FilterDescriptor<ISchemaFilter>> _schemaFilterDescriptors;

        private struct FilterDescriptor<TFilter>
        {
            public Type Type;
            public object[] Arguments;
        }

        public SwaggerGenOptions()
        {
            _swaggerGeneratorSettings = new SwaggerGeneratorSettings();
            _schemaRegistrySettings = new SchemaRegistrySettings();

            _xmlDocFactories = new List<Func<XPathDocument>>();
            _operationFilterDescriptors = new List<FilterDescriptor<IOperationFilter>>();
            _documentFilterDescriptors = new List<FilterDescriptor<IDocumentFilter>>();
            _schemaFilterDescriptors = new List<FilterDescriptor<ISchemaFilter>>();

            // Enable Annotations
            OperationFilter<SwaggerAttributesOperationFilter>();
            OperationFilter<SwaggerResponseAttributeFilter>();
            SchemaFilter<SwaggerAttributesSchemaFilter>();
        }

        /// <summary>
        /// Define one or more documents to be created by the Swagger generator
        /// </summary>
        /// <param name="name">A URI-friendly name that uniquely identifies the document</param>
        /// <param name="info">Global metadata to be included in the Swagger output</param>
        public void SwaggerDoc(string name, Info info)
        {
            _swaggerGeneratorSettings.SwaggerDocs.Add(name, info);
        }

        /// <summary>
        /// Provide a custom strategy for selecting actions.
        /// </summary>
        /// <param name="predicate">
        /// A lambda that returns true/false based on document name and ApiDescription
        /// </param>
        public void DocInclusionPredicate(Func<string, ApiDescription, bool> predicate)
        {
            _swaggerGeneratorSettings.DocInclusionPredicate = predicate;
        }

        /// <summary>
        /// Ignore any actions that are decorated with the ObsoleteAttribute
        /// </summary>
        public void IgnoreObsoleteActions()
        {
            _swaggerGeneratorSettings.IgnoreObsoleteActions = true;
        }

        /// <summary>
        /// Provide a custom strategy for assigning a default "tag" to actions
        /// </summary>
        /// <param name="tagSelector"></param>
        public void TagActionsBy(Func<ApiDescription, string> tagSelector)
        {
            _swaggerGeneratorSettings.TagSelector = tagSelector;
        }

        /// <summary>
        /// Provide a custom strategy for sorting actions before they're transformed into the Swagger format
        /// </summary>
        /// <param name="sortKeySelector"></param>
        public void OrderActionsBy(Func<ApiDescription, string> sortKeySelector)
        {
            _swaggerGeneratorSettings.SortKeySelector = sortKeySelector;
        }

        /// <summary>
        /// Describe all parameters, regardless of how they appear in code, in camelCase
        /// </summary>
        public void DescribeAllParametersInCamelCase()
        {
            _swaggerGeneratorSettings.DescribeAllParametersInCamelCase = true;
        }

        /// <summary>
        /// Add one or more "securityDefinitions", describing how your API is protected, to the generated Swagger
        /// </summary>
        /// <param name="name">A unique name for the scheme, as per the Swagger spec.</param>
        /// <param name="securityScheme">
        /// A description of the scheme - can be an instance of BasicAuthScheme, ApiKeyScheme or OAuth2Scheme
        /// </param>
        public void AddSecurityDefinition(string name, SecurityScheme securityScheme)
        {
            _swaggerGeneratorSettings.SecurityDefinitions.Add(name, securityScheme);
        }

        /// <summary>
        /// Provide a custom mapping, for a given type, to the Swagger-flavored JSONSchema
        /// </summary>
        /// <param name="type">System type</param>
        /// <param name="schemaFactory">A factory method that generates Schema's for the provided type</param>
        public void MapType(Type type, Func<Schema> schemaFactory)
        {
            _schemaRegistrySettings.CustomTypeMappings.Add(type, schemaFactory);
        }

        /// <summary>
        /// Provide a custom mapping, for a given type, to the Swagger-flavored JSONSchema
        /// </summary>
        /// <typeparam name="T">System type</typeparam>
        /// <param name="schemaFactory">A factory method that generates Schema's for the provided type</param>
        public void MapType<T>(Func<Schema> schemaFactory)
        {
            MapType(typeof(T), schemaFactory);
        }

        /// <summary>
        /// Use the enum names, as opposed to their integer values, when describing enum types
        /// </summary>
        public void DescribeAllEnumsAsStrings()
        {
            _schemaRegistrySettings.DescribeAllEnumsAsStrings = true;
        }

        /// <summary>
        /// If applicable, describe all enum names, regardless of how they appear in code, in camelCase.
        /// </summary>
        public void DescribeStringEnumsInCamelCase()
        {
            _schemaRegistrySettings.DescribeStringEnumsInCamelCase = true;
        }

        /// <summary>
        /// Provide a custom strategy for generating the unique Id's that are used to reference object Schema's
        /// </summary>
        /// <param name="schemaIdSelector">
        /// A lambda that returns a unique identifier for the provided system type
        /// </param>
        public void CustomSchemaIds(Func<Type, string> schemaIdSelector)
        {
            _schemaRegistrySettings.SchemaIdSelector = schemaIdSelector;
        }

        /// <summary>
        /// Ignore any properties that are decorated with the ObsoleteAttribute
        /// </summary>
        public void IgnoreObsoleteProperties()
        {
            _schemaRegistrySettings.IgnoreObsoleteProperties = true;
        }

        /// <summary>
        /// Extend the Swagger Generator with "filters" that can modify Operations after they're initially generated
        /// </summary>
        /// <typeparam name="TFilter">A type that derives from IOperationFilter</typeparam>
        /// <param name="parameters">Optionally inject parameters through filter constructors</param>
        public void OperationFilter<TFilter>(params object[] parameters)
            where TFilter : IOperationFilter
        {
            _operationFilterDescriptors.Add(new FilterDescriptor<IOperationFilter>
            {
                Type = typeof(TFilter),
                Arguments = parameters
            });
        }

        /// <summary>
        /// Extend the Swagger Generator with "filters" that can modify SwaggerDocuments after they're initially generated
        /// </summary>
        /// <typeparam name="TFilter">A type that derives from IDocumentFilter</typeparam>
        /// <param name="parameters">Optionally inject parameters through filter constructors</param>
        public void DocumentFilter<TFilter>(params object[] parameters)
            where TFilter : IDocumentFilter
        {
            _documentFilterDescriptors.Add(new FilterDescriptor<IDocumentFilter>
            {
                Type = typeof(TFilter),
                Arguments = parameters
            });
        }

        /// <summary>
        /// Extend the Swagger Generator with "filters" that can modify Schemas after they're initially generated
        /// </summary>
        /// <typeparam name="TFilter">A type that derives from ISchemaFilter</typeparam>
        /// <param name="parameters">Optionally inject parameters through filter constructors</param>
        public void SchemaFilter<TFilter>(params object[] parameters)
            where TFilter : ISchemaFilter
        {
            _schemaFilterDescriptors.Add(new FilterDescriptor<ISchemaFilter>
            {
                Type = typeof(TFilter),
                Arguments = parameters
            });
        }

        /// <summary>
        /// Inject human-friendly descriptions for Operations, Parameters and Schemas based on XML Comment files
        /// </summary>
        /// <param name="xmlDocFactory">A factory method that returns XML Comments as an XPathDocument</param>
        public void IncludeXmlComments(Func<XPathDocument> xmlDocFactory)
        {
            _xmlDocFactories.Add(xmlDocFactory);
        }

        /// <summary>
        /// Inject human-friendly descriptions for Operations, Parameters and Schemas based on XML Comment files
        /// </summary>
        /// <param name="filePath">An abolsute path to the file that contains XML Comments</param>
        public void IncludeXmlComments(string filePath)
        {
            IncludeXmlComments(() => new XPathDocument(filePath));
        }

        internal ISwaggerProvider CreateSwaggerProvider(IServiceProvider serviceProvider)
        {
            var swaggerGeneratorSettings = CreateSwaggerGeneratorSettings(serviceProvider);
            var schemaRegistrySettings = CreateSchemaRegistrySettings(serviceProvider);

            // Instantiate & add the XML comments filters here so they're executed before any custom
            // filters AND so they can share the same XPathDocument (perf. optimization)
            foreach (var xmlDocFactory in _xmlDocFactories)
            {
                var xmlDoc = xmlDocFactory();
                swaggerGeneratorSettings.OperationFilters.Insert(0, new XmlCommentsOperationFilter(xmlDoc));
                schemaRegistrySettings.SchemaFilters.Insert(0, new XmlCommentsSchemaFilter(xmlDoc));
            }

            var schemaProvider = new SchemaGenerator(
                serviceProvider.GetRequiredService<IOptions<MvcJsonOptions>>().Value.SerializerSettings,
                schemaRegistrySettings
            );

            return new SwaggerGenerator(
                serviceProvider.GetRequiredService<IApiDescriptionGroupCollectionProvider>(),
                schemaProvider,
                swaggerGeneratorSettings
            );
        }

        private SchemaRegistrySettings CreateSchemaRegistrySettings(IServiceProvider serviceProvider)
        {
            var settings = _schemaRegistrySettings.Clone();

            foreach (var filter in CreateFilters(_schemaFilterDescriptors, serviceProvider))
            {
                settings.SchemaFilters.Add(filter);
            }

            return settings;
        }

        private SwaggerGeneratorSettings CreateSwaggerGeneratorSettings(IServiceProvider serviceProvider)
        {
            var settings = _swaggerGeneratorSettings.Clone();

            foreach (var filter in CreateFilters(_operationFilterDescriptors, serviceProvider))
            {
                settings.OperationFilters.Add(filter);
            }

            foreach (var filter in CreateFilters(_documentFilterDescriptors, serviceProvider))
            {
                settings.DocumentFilters.Add(filter);
            }

            return settings;
        }

        private IEnumerable<TFilter> CreateFilters<TFilter>(
            List<FilterDescriptor<TFilter>> _filterDescriptors,
            IServiceProvider serviceProvider)
        {
            return _filterDescriptors.Select(descriptor =>
            {
                return (TFilter)ActivatorUtilities.CreateInstance(serviceProvider, descriptor.Type, descriptor.Arguments);
            });
        }
    }
}