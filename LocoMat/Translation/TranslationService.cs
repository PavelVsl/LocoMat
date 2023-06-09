using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace LocoMat.Translation;

public class TranslationService : ITranslationService
{
    private readonly ConfigurationData _config;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(ConfigurationData config, ILogger<TranslationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task<Result<string>> TranslateText(string text, string targetLanguage)
    {
        // Replace 'en' with the source language code, if necessary
        var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair=en|{targetLanguage}&de={_config.Email}";

        try
        {
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetAsync(url);
                var jsonContent = await json.Content.ReadAsStringAsync();
                var response = JsonSerializer.Deserialize<Response>(jsonContent);

                if (response.ResponseStatus == 200)
                {
                    var translatedText = response.ResponseData.TranslatedText;
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


    public async Task<Result<string>> TranslateString(string text, string languageCode)
    {
        if (string.IsNullOrEmpty(text)) return Result<string>.Failure("Text to translate is empty");
        if (string.IsNullOrEmpty(languageCode)) return Result<string>.Failure("Language code is empty");

        var translationResult = await TranslateText(text, languageCode);

        if (translationResult.IsSuccess) return translationResult.Value;

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

    public async Task Translate()
    {
        await FileProcessor.ProcessFilesAsync(_config.Source, _config.Output, true, TranslateResourceFile, true, "*.resx");
    }


    private Task TranslateResourceFile(string fileName)
    {
        return TranslateResourceFile(fileName, Path.GetDirectoryName(fileName));
    }
    
    private bool IsValidCultureCode(string cultureCode)
    {
        return CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => c.Name.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));
    }

    private async Task TranslateResourceFile(string baseFileName, string outputPath)
    {
        //Check if languages are not empty
        if (string.IsNullOrEmpty(_config.TargetLanguages))
        {
            _logger.LogError("No target languages specified");
            return;
        }
        //Check if fileName do not contain any culture code in the name before extension e.g. "file.name.cs-CZ.resx"
        //1. get culture code from file name
        //2. If culture code is not empty, skip translation
        //3. If culture code is empty (base resource), translate file
        var lc = Path.GetFileNameWithoutExtension(baseFileName).Split('.').LastOrDefault();
        if (!string.IsNullOrEmpty(lc) && IsValidCultureCode(lc))
        {
            _logger.LogDebug($"Skipping translation of {baseFileName} because it is already translated");
            return;
        }
        

        var existingResources = Utilities.GetExistingResources(baseFileName);
        _logger.LogInformation($"Translating {existingResources.Count} resources in {baseFileName}");

        foreach (var languageCode in _config.TargetLanguages.Split(','))
        {
            var outputFilePath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(baseFileName)}.{languageCode}.resx");
            _logger.LogInformation($"Translating to {languageCode} in {outputFilePath}");
            var translatedResources = Utilities.GetOrCreateResxFile(outputFilePath);
            var translationCounter = 0;
            var errorCounter = 0;
            foreach (var resource in existingResources)
                if (!translatedResources.ContainsKey(resource.Key))
                {
                    var translate = resource.Value;
                    if (string.IsNullOrEmpty(translate))
                    {
                        _logger.LogDebug($"Skipping empty resource: {resource.Key}");
                        continue;
                    }

                    if (_config.TestMode)
                    {
                        translate = resource.Value;
                        _logger.LogDebug($"Translating resource:({languageCode}) {resource.Key} {translate}");
                    }
                    else
                    {
                        Result<string> result;
                        try
                        {
                            result = await TranslateString(resource.Value, languageCode);
                        }
                        finally
                        {
                            errorCounter++;
                        }

                        if (result.IsSuccess)
                        {
                            translate = result.Value;
                            if (translatedResources.TryAdd(resource.Key, translate)) translationCounter++;
                            errorCounter = 0;
                        }
                        else
                        {
                            _logger.LogError($"Failed to translate resource:({languageCode}) {resource.Key} {translate}");
                            errorCounter++;
                            if (errorCounter > 5)
                            {
                                _logger.LogError("Stopping translation due to too many errors");
                                return;
                            }
                        }
                    }
                }

            if (!_config.TestMode && translationCounter > 0)
            {
                Utilities.WriteResourcesToFile(translatedResources, outputFilePath);
                _logger.LogInformation($"Writing: {outputFilePath}");
            }
        }
    }
}
