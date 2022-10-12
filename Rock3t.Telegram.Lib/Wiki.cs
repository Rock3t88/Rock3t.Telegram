using System.ComponentModel.Design;
using Genbox.Wikipedia;
using Genbox.Wikipedia.Enums;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Rock3t.Telegram.Lib.Resources;

namespace Rock3t.Telegram.Lib;

public class Wiki
{
    private readonly WikipediaClient _wikipediaClient;

    public Wiki()
    {
        _wikipediaClient = new WikipediaClient();
        _wikipediaClient.DefaultLanguage = WikiLanguage.German;
    }

    public async Task<WikiAnswer?> SearchAsync(string text)
    {
        Uri baseUri = new Uri("https://www.stupidedia.org/stupi/");

        HttpClient client = new HttpClient();
        HtmlDocument htmlDocument = new HtmlDocument();

        var result0 = await client.GetAsync(new Uri(baseUri, text));

        htmlDocument.LoadHtml(result0.Content.ReadAsStringAsync().Result);

        //var noArticleTest = htmlDocument.GetElementbyId("noarticletext");

        //if (noArticleTest == null)
        //    return null;

        var contentElement = htmlDocument.GetElementbyId("mw-content-text");

        HtmlDocument htmlDocument2 = new HtmlDocument();
        htmlDocument2.LoadHtml(contentElement.InnerHtml);

        HtmlNodeCollection imgs = htmlDocument2.DocumentNode.SelectNodes("//img[@src]");
        HtmlNodeCollection divs = htmlDocument2.DocumentNode.SelectNodes("//div[@class]");

        string? answerText =  contentElement.ChildNodes.FirstOrDefault(node => node.Name == "p")?.InnerText;

        if (string.IsNullOrWhiteSpace(answerText))
            return null;

        WikiAnswer answer = new WikiAnswer(answerText);


        var thumbimage = imgs.FirstOrDefault(node => node.Attributes["class"]?.Value.Contains("thumbimage") == true);

        if (thumbimage != null)
        {
            string? imageUri = thumbimage.Attributes["src"]?.Value;

            if (!string.IsNullOrWhiteSpace(imageUri))
                answer.ImageUri = new Uri(baseUri, imageUri);

            answer.ImageCaption =
                divs.FirstOrDefault(node => node.Attributes["class"]?.Value.Contains("thumbcaption") == true)!
                    .InnerText;
        }

        return answer;
    }

    internal static string RemoveUnwantedTags(string data)
    {
        if (string.IsNullOrEmpty(data)) return string.Empty;

        var document = new HtmlDocument();
        document.LoadHtml(data);

        var acceptableTags = new String[] { "b", "i", "u", "s", "a", "em", "strong", "ins", "strike", "del"  };

        var nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()"));
        while (nodes.Count > 0)
        {
            var node = nodes.Dequeue();
            var parentNode = node.ParentNode;

            if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
            {
                var childNodes = node.SelectNodes("./*|./text()");

                if (childNodes != null)
                {
                    foreach (var child in childNodes)
                    {
                        nodes.Enqueue(child);
                        parentNode.InsertBefore(child, node);
                    }
                }

                parentNode.RemoveChild(node);

            }
        }

        return document.DocumentNode.InnerHtml;
    }
}

