﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.SwaggerGen
{
    public class SwaggerGenerator : ISwaggerProvider
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionsProvider;
        private readonly ISchemaProvider _schemaProvider;
        private readonly SwaggerGeneratorSettings _settings;

        public SwaggerGenerator(
            IApiDescriptionGroupCollectionProvider apiDescriptionsProvider,
            ISchemaProvider schemaProvider,
            SwaggerGeneratorSettings settings = null)
        {
            _apiDescriptionsProvider = apiDescriptionsProvider;
            _schemaProvider = schemaProvider;
            _settings = settings ?? new SwaggerGeneratorSettings();
        }

        public SwaggerDocument GetSwagger(
            string documentName,
            string host = null,
            string basePath = null,
            string[] schemes = null)
        {
            // Create a dictionary for collecting Schema definitions that are scoped to this document
            var definitions = new Dictionary<string, Schema>();

            Info info;
            if (!_settings.SwaggerDocs.TryGetValue(documentName, out info))
                throw new UnknownSwaggerDocument(documentName);

            var apiDescriptions = _apiDescriptionsProvider.ApiDescriptionGroups.Items
                .SelectMany(group => group.Items)
                .Where(apiDesc => _settings.DocInclusionPredicate(documentName, apiDesc))
                .Where(apiDesc => !_settings.IgnoreObsoleteActions || !apiDesc.IsObsolete())
                .OrderBy(_settings.SortKeySelector);

            var paths = apiDescriptions
                .GroupBy(apiDesc => apiDesc.RelativePathSansQueryString())
                .ToDictionary(group => "/" + group.Key, group => CreatePathItem(group, definitions));

            var swaggerDoc = new SwaggerDocument
            {
                Info = info,
                Host = host,
                BasePath = basePath,
                Schemes = schemes,
                Paths = paths,
                Definitions = definitions,
                SecurityDefinitions = _settings.SecurityDefinitions
            };

            var filterContext = new DocumentFilterContext(
                _apiDescriptionsProvider.ApiDescriptionGroups,
                _schemaProvider,
                definitions);

            foreach (var filter in _settings.DocumentFilters)
            {
                filter.Apply(swaggerDoc, filterContext);
            }

            return swaggerDoc;
        }

        private PathItem CreatePathItem(IEnumerable<ApiDescription> apiDescriptions, IDictionary<string, Schema> definitions)
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
                        "Ambiguous HTTP method for action - {0}. " +
                        "Actions require an explicit HttpMethod binding for Swagger",
                        group.First().ActionDescriptor.DisplayName));

                if (group.Count() > 1)
                    throw new NotSupportedException(string.Format(
                        "HTTP method \"{0}\" & path \"{1}\" overloaded by actions - {2}. " +
                        "Actions require unique method/path combination for Swagger",
                        httpMethod,
                        group.First().RelativePathSansQueryString(),
                        string.Join(",", group.Select(apiDesc => apiDesc.ActionDescriptor.DisplayName))));

                var apiDescription = group.Single();

                switch (httpMethod)
                {
                    case "GET":
                        pathItem.Get = CreateOperation(apiDescription, definitions);
                        break;
                    case "PUT":
                        pathItem.Put = CreateOperation(apiDescription, definitions);
                        break;
                    case "POST":
                        pathItem.Post = CreateOperation(apiDescription, definitions);
                        break;
                    case "DELETE":
                        pathItem.Delete = CreateOperation(apiDescription, definitions);
                        break;
                    case "OPTIONS":
                        pathItem.Options = CreateOperation(apiDescription, definitions);
                        break;
                    case "HEAD":
                        pathItem.Head = CreateOperation(apiDescription, definitions);
                        break;
                    case "PATCH":
                        pathItem.Patch = CreateOperation(apiDescription, definitions);
                        break;
                }
            }

            return pathItem;
        }

        private Operation CreateOperation(ApiDescription apiDescription, IDictionary<string, Schema> definitions)
        {
            var parameters = apiDescription.ParameterDescriptions
                .Where(paramDesc => paramDesc.Source.IsFromRequest && !paramDesc.IsPartOfCancellationToken())
                .Select(paramDesc => CreateParameter(apiDescription, paramDesc, definitions))
                .ToList();

            var responses = apiDescription.SupportedResponseTypes
                .DefaultIfEmpty(new ApiResponseType { StatusCode = 200 })
                .ToDictionary(
                    apiResponseType => apiResponseType.StatusCode.ToString(),
                    apiResponseType => CreateResponse(apiResponseType, definitions)
                 );

            var operation = new Operation
            {
                Tags = new[] { _settings.TagSelector(apiDescription) },
                OperationId = apiDescription.FriendlyId(),
                Consumes = apiDescription.SupportedRequestMediaTypes().ToList(),
                Produces = apiDescription.SupportedResponseMediaTypes().ToList(),
                Parameters = parameters.Any() ? parameters : null, // parameters can be null but not empty
                Responses = responses,
                Deprecated = apiDescription.IsObsolete() ? true : (bool?)null
            };

            var filterContext = new OperationFilterContext(apiDescription, _schemaProvider, definitions);
            foreach (var filter in _settings.OperationFilters)
            {
                filter.Apply(operation, filterContext);
            }

            return operation;
        }

        private IParameter CreateParameter(
            ApiDescription apiDescription,
            ApiParameterDescription paramDescription,
            IDictionary<string, Schema> definitions)
        {
            var location = GetParameterLocation(apiDescription, paramDescription);

            var name = _settings.DescribeAllParametersInCamelCase
                ? paramDescription.Name.ToCamelCase()
                : paramDescription.Name;

            var schema = (paramDescription.Type == null) ? null : _schemaProvider.GetSchema(paramDescription.Type, definitions);

            if (location == "body")
            {
                return new BodyParameter
                {
                    Name = name,
                    Schema = schema
                };
            }

            var nonBodyParam = new NonBodyParameter
            {
                Name = name,
                In = location,
                Required = (location == "path")
            };

            if (schema == null)
                nonBodyParam.Type = "string";
            else
                nonBodyParam.PopulateFrom(schema);

            if (nonBodyParam.Type == "array")
                nonBodyParam.CollectionFormat = "multi";

            return nonBodyParam;
        }

        private string GetParameterLocation(ApiDescription apiDescription, ApiParameterDescription paramDescription)
        {
            if (paramDescription.Source == BindingSource.Form)
                return "formData";
            else if (paramDescription.Source == BindingSource.Body)
                return "body";
            else if (paramDescription.Source == BindingSource.Header)
                return "header";
            else if (paramDescription.Source == BindingSource.Path)
                return "path";
            else if (paramDescription.Source == BindingSource.Query)
                return "query";

            // None of the above, default to "query"
            // Wanted to default to "body" for PUT/POST but ApiExplorer flattens out complex params into multiple
            // params for ALL non-bound params regardless of HttpMethod. So "query" across the board makes most sense
            return "query";
        }

        private Response CreateResponse(ApiResponseType apiResponseType, IDictionary<string, Schema> definitions)
        {
            var description = ResponseDescriptionMap
                .FirstOrDefault((entry) => Regex.IsMatch(apiResponseType.StatusCode.ToString(), entry.Key))
                .Value;

            return new Response
            {
                Description = description,
                Schema = (apiResponseType.Type != null && apiResponseType.Type != typeof(void))
                    ? _schemaProvider.GetSchema(apiResponseType.Type, definitions)
                    : null
            };
        }

        private static readonly Dictionary<string, string> ResponseDescriptionMap = new Dictionary<string, string>
        {
            { "1\\d{2}", "Information" },
            { "2\\d{2}", "Success" },
            { "3\\d{2}", "Redirect" },
            { "400", "Bad Request" },
            { "401", "Unauthorized" },
            { "403", "Forbidden" },
            { "404", "Not Found" },
            { "405", "Method Not Allowed" },
            { "406", "Not Acceptable" },
            { "408", "Request Timeout" },
            { "409", "Conflict" },
            { "4\\d{2}", "Client Error" },
            { "5\\d{2}", "Server Error" }
        };
    }
}
