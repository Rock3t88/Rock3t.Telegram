using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using GoogleApi;
using GoogleApi.Entities.Search.Image.Request;
using Microsoft.AspNetCore.Mvc;
using CseResource = Google.Apis.Customsearch.v1.CseResource;

namespace Rock3t.Telegram.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GoogleController 
{
    private readonly ILogger<GoogleController> _logger = null!;
    private readonly IConfiguration _config;
    //private GoogleSearch.ImageSearchApi _imageSearch;
    private CustomSearchAPIService _customSearchApiService;

    private GoogleController()
    {
        
    }

    public GoogleController(ILogger<GoogleController> logger, IConfiguration config) : this()
    {
        // Create the service.
        _customSearchApiService = new CustomSearchAPIService(new BaseClientService.Initializer
        {
            ApplicationName = "scary-terry-search",
            ApiKey = "AIzaSyArcseczA7kqLfbkndCBfiX165s-7SFMn8",
        });

        //_imageSearch = imageSearchSearch;
        _logger = logger;
        _config = config;
    }

    [HttpGet("search")]
    public async Task<Result> Search(string text)
    {
          string apiKey = "AIzaSyArcseczA7kqLfbkndCBfiX165s-7SFMn8";
        string cx = "f404807613db841a9";
        string query = $"{text}";

        var svc = new CustomsearchService(new BaseClientService.Initializer { ApiKey = apiKey, ApplicationName = "Scary Terry Search" });
        var listRequest = svc.Cse.List();
        
        int maxItems = 10;

        var textElements = text.Split(' ');

        listRequest.Cx = cx;
        if (textElements.Length > 0)
            listRequest.Q = textElements.First();
        else
            listRequest.Q = text;
        listRequest.Num = maxItems;
        listRequest.OrTerms = "comic";
        if (textElements.Length > 1)
            listRequest.OrTerms += " " + text.Substring(text.IndexOf(' ')).Trim();

        //listRequest.Safe = CseResource.ListRequest.SafeEnum.Active;
        listRequest.FileType = "png";
        listRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;
        listRequest.ImgType = CseResource.ListRequest.ImgTypeEnum.Clipart;
        //listRequest.ImgColorType = CseResource.ListRequest.ImgColorTypeEnum.Color;
        listRequest.ImgSize = CseResource.ListRequest.ImgSizeEnum.MEDIUM;

        var search = await listRequest.ExecuteAsync();

        if (search?.Items == null || search.Items.Count == 0)
        {
            listRequest.Q = text;
            search = await listRequest.ExecuteAsync();
        }

        if (search.Items.Count < maxItems)
            maxItems = search.Items.Count;

        int itemIndex = Random.Shared.Next(0, maxItems);

        return search.Items[itemIndex];

        //ImageSearchRequest imageSearchRequest = new ImageSearchRequest();
        //imageSearchRequest.Query = text;
        //var uri = imageSearchRequest.GetUri();

        //var response = await _imageSearch.QueryAsync(imageSearchRequest);
        //var response = await GoogleSearch.ImageSearch.QueryAsync(imageSearchRequest);
    }

    public struct SearchResult
    {
        public String jsonResult;
        public Dictionary<String, String> relevantHeaders;
    }
}