using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Linq;

public class DocumentParserService : IDocumentParserService
{
    public string ParseHtml(string htmlContent)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(htmlContent);

        return doc.DocumentNode.SelectNodes("//body//text()")?
            .Select(node => node.InnerText.Trim())
            .Where(text => !string.IsNullOrEmpty(text))
            .Aggregate(new StringBuilder(), (sb, text) => sb.AppendLine(text), sb => sb.ToString())
            ?? string.Empty;
    }

    public List<string> ParsePdfPerPage(byte[] pdfBytes)
    {
        using var memoryStream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(memoryStream);

        return document.GetPages()
            .Select(page => string.Join(" ", page.GetWords().Select(word => word.Text)).Trim())
            .ToList();
    }

    public string ParsePdf(byte[] pdfBytes)
    {
        return string.Join(Environment.NewLine, ParsePdfPerPage(pdfBytes));
    }
}
