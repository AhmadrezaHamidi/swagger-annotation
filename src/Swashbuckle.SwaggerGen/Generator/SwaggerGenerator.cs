using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.Swagger.Model;

namespace Swashbuckle.SwaggerGen.Generator
{
    public class SwaggerGenerator : ISwaggerProvider
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionsProvider;
        private readonly ISchemaRegistryFactory _schemaRegistryFactory;
        private readonly SwaggerGeneratorOptions _options;

        public SwaggerGenerator(
            IApiDescriptionGroupCollectionProvider apiDescriptionsProvider,
            ISchemaRegistryFactory schemaRegistryFactory,
            SwaggerGeneratorOptions options = null)
        {
            _apiDescriptionsProvider = apiDescriptionsProvider;
            _schemaRegistryFactory = schemaRegistryFactory;
            _options = options ?? new SwaggerGeneratorOptions();
        }

        public SwaggerDocument GetSwagger(
            string apiVersion,
            string host = null,
            string basePath = null,
            string[] schemes = null)
        {
            var schemaRegistry = _schemaRegistryFactory.Create();

            var info = _options.ApiVersions.FirstOrDefault(v => v.Version == apiVersion);
            if (info == null)
                throw new UnknownApiVersion(apiVersion);

            var paths = GetApiDescriptionsFor(apiVersion)
                .Where(apiDesc => !(_options.IgnoreObsoleteActions && apiDesc.IsObsolete()))
                .OrderBy(_options.GroupNameSelector, _options.GroupNameComparer)
                .GroupBy(apiDesc => apiDesc.RelativePathSansQueryString())
                .ToDictionary(group => "/" + group.Key, group => CreatePathItem(group, schemaRegistry));

            var swaggerDoc = new SwaggerDocument
            {
                Info = info,
                Host = host,
                BasePath = basePath,
                Schemes = schemes,
                Paths = paths,
                Definitions = schemaRegistry.Definitions,
                SecurityDefinitions = _options.SecurityDefinitions
            };

            var filterContext = new DocumentFilterContext(
                _apiDescriptionsProvider.ApiDescriptionGroups,
                null);

            foreach (var filter in _options.DocumentFilters)
            {
                filter.Apply(swaggerDoc, filterContext);
            }

            return swaggerDoc;
        }

        private IEnumerable<ApiDescription> GetApiDescriptionsFor(string apiVersion)
        {
            var allDescriptions = _apiDescriptionsProvider.ApiDescriptionGroups.Items
                .SelectMany(group => group.Items);

            return (_options.VersionSupportResolver == null)
                ? allDescriptions
                : allDescriptions.Where(apiDesc => _options.VersionSupportResolver(apiDesc, apiVersion));
        }

        private PathItem CreatePathItem(IEnumerable<ApiDescription> apiDescriptions, ISchemaRegistry schemaRegistry)
        {
            var pathItem = new PathItem();

            // Group further by http method
            var perMethodGrouping = apiDescriptions
                .GroupBy(apiDesc => apiDesc.HttpMethod);
                
            foreach (var group in perMethodGrouping)
            {
                var httpMethod = group.Key;

                if (httpMethod == null)
                    throw new NotSupportedException(string.Format(
                        "Unbounded HTTP verbs for path '{0}'. Are you missing an HttpMethodAttribute?",
                        group.First().RelativePathSansQueryString()));

                if (group.Count() > 1)
                    throw new NotSupportedException(string.Format(
                        "Multiple operations with path '{0}' and method '{1}'. Are you overloading action methods?",
                        group.First().RelativePathSansQueryString(), httpMethod));

                var apiDescription = group.Single();

                switch (httpMethod)
                {
                    case "GET":
                        pathItem.Get = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "PUT":
                        pathItem.Put = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "POST":
                        pathItem.Post = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "DELETE":
                        pathItem.Delete = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "OPTIONS":
                        pathItem.Options = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "HEAD":
                        pathItem.Head = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "PATCH":
                        pathItem.Patch = CreateOperation(apiDescription, schemaRegistry);
                        break;
                }
            }

            return pathItem;
        }

        private Operation CreateOperation(ApiDescription apiDescription, ISchemaRegistry schemaRegistry)
        {
            var groupName = _options.GroupNameSelector(apiDescription);

            var parameters = apiDescription.ParameterDescriptions
                .Where(paramDesc => paramDesc.Source.IsFromRequest)
                .Select(paramDesc => CreateParameter(paramDesc, schemaRegistry))
                .ToList();

            var operation = new Operation
            {
                Tags = (groupName != null) ? new[] { groupName } : null,
                OperationId = apiDescription.FriendlyId(),
                Consumes = apiDescription.ConsumesMediaTypes().ToList(),
                Produces = apiDescription.ProducesMediaTypes().ToList(),
                Parameters = parameters.Any() ? parameters : null, // parameters can be null but not empty
                Responses = CreateResponses(apiDescription.SupportedResponseTypes, schemaRegistry),
                Deprecated = apiDescription.IsObsolete()
            };

            var filterContext = new OperationFilterContext(apiDescription, schemaRegistry);
            foreach (var filter in _options.OperationFilters)
            {
                filter.Apply(operation, filterContext);
            }

            return operation;
        }

        private IParameter CreateParameter(ApiParameterDescription paramDesc, ISchemaRegistry schemaRegistry)
        {
            var source = paramDesc.Source.Id.ToLower();
            var schema = (paramDesc.Type == null) ? null : schemaRegistry.GetOrRegister(paramDesc.Type);

            if (source == "body")
            {
                return new BodyParameter
                {
                    Name = paramDesc.Name,
                    In = source,
                    Schema = schema
                };
            }
            else
            {
                var nonBodyParam = new NonBodyParameter
                {
                    Name = paramDesc.Name,
                    In = source,
                    Required = (source == "path")
                };

                if (schema == null)
                    nonBodyParam.Type = "string";
                else
                    nonBodyParam.PopulateFrom(schema);

                if (nonBodyParam.Type == "array")
                    nonBodyParam.CollectionFormat = "multi";

                return nonBodyParam;
            }
        }

        private IDictionary<string, Response> CreateResponses(
            IList<ApiResponseType> supportedResponseTypes, ISchemaRegistry schemaRegistry)
        {
            if (!supportedResponseTypes.Any())
            {
                return new Dictionary<string, Response>
                {
                    { "204", new Response { Description = "No Content" } }
                };
            }

            return supportedResponseTypes.ToDictionary(
                responseType => responseType.StatusCode.ToString(),
                responseType => new Response
                {
                    Description = responseType.StatusCode.ToString(),
                    Schema = (responseType.Type == null) ? null : schemaRegistry.GetOrRegister(responseType.Type)
                }
            );
        }
    }
}