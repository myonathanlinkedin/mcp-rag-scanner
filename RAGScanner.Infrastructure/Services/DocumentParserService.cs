using HtmlAgilityPack;
using System.Text;
using UglyToad.PdfPig;

public class DocumentParserService : IDocumentParserService
{
    public string ParseHtml(string htmlContent)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);
        var sb = new StringBuilder();

        foreach (var node in doc.DocumentNode.SelectNodes("//body//text()") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = node.InnerText.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                sb.AppendLine(text);
            }
        }

        return sb.ToString();
    }

    public string ParsePdf(byte[] pdfBytes)
    {
        using var memoryStream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(memoryStream);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }
}