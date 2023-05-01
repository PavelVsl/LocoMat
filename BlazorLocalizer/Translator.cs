using Newtonsoft.Json.Linq;

namespace BlazorLocalizer;

public static class Translator
{
    public static string Email = "";
    public static string targetLanguage { get; set; } = "en";


    public static async Task<string> Translate(this string text, string languageCode)
    {
        if (languageCode == "en")
        {
            return text;
        }
        else
        {
            return await TranslateText(text, languageCode);
        }
    }

    private static async Task<string> TranslateText(string text, string targetLanguage)
    {
        // Replace 'en' with the source language code, if necessary
        string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair=en|{targetLanguage}&de={Email}";

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(url);
            string jsonContent = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(jsonContent);
            var translatedText = json["responseData"]["translatedText"].ToString();
            Console.WriteLine($"Translate: {text} -> {translatedText}");
            return translatedText;
        }
    }
}
