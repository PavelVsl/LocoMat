using System.Xml;

namespace BlazorRazorLocalizer;

public static class ResourceGenerator
{ 

    // Implement code to create a resx file if it doesn't exist
    // and add the resource keys and values to it.
    public static async Task CreateResxFile(Dictionary<string, string> resourceKeys, string baseFileName)
    {
        // Check if there are any resource keys
        if (resourceKeys.Count == 0)
        {
            return;
        }

        // Get the resx file name from the razor file name
        string resxFileName;
        bool translate = false;
        // Check if the language is specified and modify file name accordingly
        string language = Translator.targetLanguage;
        if (!string.IsNullOrEmpty(language) && language != "en")
        {
            resxFileName = Path.ChangeExtension(baseFileName, $".{language}.resx");
            translate = true;
        }
        else
        {
            resxFileName = Path.ChangeExtension(baseFileName, ".resx");
        }

        // Create the resx file if it doesn't exist
        if (!File.Exists(resxFileName))
        {
            File.WriteAllText(resxFileName, "<root></root>");
        }

        // Read the resx file
        string resxContent = File.ReadAllText(resxFileName);

        // Create an XML document from the resx content
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(resxContent);

        // Get the root node
        XmlNode root = doc.DocumentElement;

        // Add the resource keys and values to the resx file
        foreach (var pair in resourceKeys)
        {
            // Check if the resource key already exists
            if (root.SelectSingleNode($"/root/data[@name='{pair.Key}']") == null)
            {
                // Add the resource key and value
                XmlNode dataNode = root.AppendChild(doc.CreateElement("data"));
                dataNode.Attributes.Append(doc.CreateAttribute("name")).Value = pair.Key;
                XmlNode valueNode = dataNode.AppendChild(doc.CreateElement("value"));

                if (translate)
                {
                    var response = await pair.Value.Translate();
                    valueNode.InnerText = response;
                }
                else
                    valueNode.InnerText = pair.Value;
            }
        }

        // Save the resx file
        doc.Save(resxFileName);
    }

// Implement code to translate a resx file  
// and save the translated file.
// The language parameter is the language to translate to.
// Check if target file exists and if it does, load it and translate only missing keys
// Do not create dictionary, just translate missing keys
// If it doesn't exist, create it and translate all keys
    public static async Task TranslateResourceFile(string Source)
    {
        string language = Translator.targetLanguage;
        if (string.IsNullOrEmpty(language) || language == "en")
        {
            Console.WriteLine("No language specified or language is English. No translation will be done.");
        }
        // Get the resx file name from the razor file name
        string resxFileName = Path.ChangeExtension(Source, ".resx");
        string resxFileNameTranslated = Path.ChangeExtension(Source, $".{language}.resx");

        // Create the resx file if it doesn't exist
        if (!File.Exists(resxFileNameTranslated))
        {
            File.WriteAllText(resxFileNameTranslated, "<root></root>");
        }

        // Read the resx file
        string resxContent = File.ReadAllText(resxFileName);
        string resxContentTranslated = File.ReadAllText(resxFileNameTranslated);

        // Create an XML document from the resx content
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(resxContent);

        // Create an XML document from the resx content
        XmlDocument docTranslated = new XmlDocument();
        docTranslated.LoadXml(resxContentTranslated);

        // Get the root node
        XmlNode root = doc.DocumentElement;
        XmlNode rootTranslated = docTranslated.DocumentElement;

        // Add the resource keys and values to the resx file
        foreach (XmlNode node in root.ChildNodes)
        {
            // check if node.InnerText is not empty
            if (string.IsNullOrEmpty(node.InnerText))
                continue;

            // Check if the resource key already exists
            if (rootTranslated.SelectSingleNode($"/root/data[@name='{node.Attributes["name"].Value}']") == null)
            {
                // Add the resource key and value
                XmlNode dataNode = rootTranslated.AppendChild(docTranslated.CreateElement("data"));
                dataNode.Attributes.Append(docTranslated.CreateAttribute("name")).Value = node.Attributes["name"].Value;
                XmlNode valueNode = dataNode.AppendChild(docTranslated.CreateElement("value"));

                var response = await node.InnerText.Translate();
                valueNode.InnerText = response;
            }
        }

        // Save the resx file
        docTranslated.Save(resxFileNameTranslated);
    }
}
          
