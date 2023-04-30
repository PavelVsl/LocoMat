using Newtonsoft.Json.Linq;

namespace BlazorRazorLocalizer;

public static class Translator
{
    const string email = "pavel@vesely.dev";
    public static async Task<string> Translate(this string text, string targetLanguage= "en")
    {
        if (targetLanguage == "en")
        {
            return text;
        }
        else
        {
            return await TranslateText(text, targetLanguage);
        }
    }

private static async Task<string> TranslateText(string text, string targetLanguage)
{
    // Replace 'en' with the source language code, if necessary
    string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair=en|{targetLanguage}&de={email}";

    using (var httpClient = new HttpClient())
    {
        var response = await httpClient.GetAsync(url);
        string jsonContent = await response.Content.ReadAsStringAsync();
        JObject json = JObject.Parse(jsonContent);
        var translatedText = json["responseData"]["translatedText"].ToString();
        Console.WriteLine($"{text} -> {translatedText} ({targetLanguage})");
        return translatedText;
    }
}
 
}
