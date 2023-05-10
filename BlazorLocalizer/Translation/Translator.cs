using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace BlazorLocalizer.Translation
{
    public class Translator
    {
        private readonly ConfigurationData _config;
        private readonly ILogger<Translator> _logger;

        public Translator(ConfigurationData config, ILogger<Translator> logger)
        {
            _config = config;
            _logger = logger;
        }

private async Task<Result<string>> TranslateText(string text, string targetLanguage)
{
    // Replace 'en' with the source language code, if necessary
    string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair=en|{targetLanguage}&de={_config.Email}";

    try
    {
        using (var httpClient = new HttpClient())
        {
            var json = await httpClient.GetAsync(url);
            string jsonContent = await json.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<Response>(jsonContent);

            if (response.ResponseStatus == 200)
            {
                string translatedText = response.ResponseData.TranslatedText;
                _logger.LogInformation($"Translated: '{text}' -> '{translatedText}'");
                return Result<string>.Success(translatedText);
            }

            _logger.LogError($"Failed to translate '{text}' with error: ({response.ResponseStatus}) {response.ResponseDetails}");
            return Result<string>.Failure(response.ResponseDetails);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError($"Failed to translate '{text}' with error: {ex.Message}");
        return Result<string>.Failure(ex.Message);
    }
}


        public async Task<Result<string>> Translate(string text, string languageCode)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Result<string>.Failure("Text to translate is empty");
            }
            if (string.IsNullOrEmpty(languageCode))
            {
                return Result<string>.Failure("Language code is empty");
            }

            var translationResult = await TranslateText(text, languageCode);

            if (translationResult.IsSuccess)
            {
                return translationResult.Value;
            }

            return Result<string>.Failure(translationResult.ErrorMessage);
        }

        public class Response
        {
            [JsonPropertyName("responseData")] public ResponseData ResponseData { get; set; }
        //    [JsonPropertyName("quotaFinished")] public string QuotaFinished { get; set; }
        //    [JsonPropertyName("mtLangSupported")] public object MtLangSupported { get; set; }
            [JsonPropertyName("responseDetails")] public string ResponseDetails { get; set; }
            [JsonPropertyName("responseStatus")] public int ResponseStatus { get; set; }
        //    [JsonPropertyName("responderId")] public object ResponderId { get; set; }
        //    [JsonPropertyName("exception_code")] public object ExceptionCode { get; set; }
        }

        public class ResponseData
        {
            [JsonPropertyName("translatedText")] public string TranslatedText { get; set; }
      //      [JsonPropertyName("match")] public string Match { get; set; }
        }
    }
}

    
