using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureDeploymentSaaS.Shared.Infrastructure.Services;

/// <summary>
/// Interface for Azure AI Search service
/// </summary>
public interface IAzureSearchService
{
    Task<IEnumerable<T>> SearchAsync<T>(string searchText, string indexName, int skip = 0, int take = 20) where T : class;
    Task IndexDocumentAsync<T>(T document, string indexName) where T : class;
    Task IndexDocumentsAsync<T>(IEnumerable<T> documents, string indexName) where T : class;
    Task DeleteDocumentAsync(string documentId, string indexName);
    Task CreateOrUpdateIndexAsync<T>(string indexName) where T : class;
}

/// <summary>
/// Azure AI Search service implementation
/// </summary>
public class AzureSearchService : IAzureSearchService
{
    private readonly SearchIndexClient _indexClient;
    private readonly ILogger<AzureSearchService> _logger;
    private readonly Dictionary<string, SearchClient> _searchClients = new();

    public AzureSearchService(IConfiguration configuration, ILogger<AzureSearchService> logger)
    {
        _logger = logger;
        var endpoint = configuration["AzureSearch:Endpoint"] ?? throw new ArgumentNullException("AzureSearch:Endpoint");
        var apiKey = configuration["AzureSearch:ApiKey"] ?? throw new ArgumentNullException("AzureSearch:ApiKey");
        
        var credential = new Azure.AzureKeyCredential(apiKey);
        _indexClient = new SearchIndexClient(new Uri(endpoint), credential);
    }

    public async Task<IEnumerable<T>> SearchAsync<T>(string searchText, string indexName, int skip = 0, int take = 20) where T : class
    {
        try
        {
            var searchClient = GetSearchClient(indexName);
            var options = new SearchOptions
            {
                Size = take,
                Skip = skip,
                IncludeTotalCount = true
            };

            var response = await searchClient.SearchAsync<T>(searchText, options);
            return response.Value.GetResults().Select(r => r.Document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents in index {IndexName} with query: {SearchText}", indexName, searchText);
            throw;
        }
    }

    public async Task IndexDocumentAsync<T>(T document, string indexName) where T : class
    {
        try
        {
            var searchClient = GetSearchClient(indexName);
            await searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(new[] { document }));
            _logger.LogInformation("Successfully indexed document in {IndexName}", indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document in {IndexName}", indexName);
            throw;
        }
    }

    public async Task IndexDocumentsAsync<T>(IEnumerable<T> documents, string indexName) where T : class
    {
        try
        {
            var searchClient = GetSearchClient(indexName);
            var batch = IndexDocumentsBatch.Upload(documents);
            await searchClient.IndexDocumentsAsync(batch);
            _logger.LogInformation("Successfully indexed {Count} documents in {IndexName}", documents.Count(), indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing {Count} documents in {IndexName}", documents.Count(), indexName);
            throw;
        }
    }

    public async Task DeleteDocumentAsync(string documentId, string indexName)
    {
        try
        {
            var searchClient = GetSearchClient(indexName);
            var batch = IndexDocumentsBatch.Delete("id", new[] { documentId });
            await searchClient.IndexDocumentsAsync(batch);
            _logger.LogInformation("Successfully deleted document {DocumentId} from {IndexName}", documentId, indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId} from {IndexName}", documentId, indexName);
            throw;
        }
    }

    public async Task CreateOrUpdateIndexAsync<T>(string indexName) where T : class
    {
        try
        {
            var fieldBuilder = new FieldBuilder();
            var definition = new SearchIndex(indexName, fieldBuilder.Build(typeof(T)));
            
            await _indexClient.CreateOrUpdateIndexAsync(definition);
            _logger.LogInformation("Successfully created or updated index {IndexName}", indexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or updating index {IndexName}", indexName);
            throw;
        }
    }

    private SearchClient GetSearchClient(string indexName)
    {
        if (!_searchClients.ContainsKey(indexName))
        {
            _searchClients[indexName] = _indexClient.GetSearchClient(indexName);
        }
        return _searchClients[indexName];
    }
}