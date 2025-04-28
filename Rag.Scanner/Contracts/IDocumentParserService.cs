public interface IDocumentParserService
{
    string ParseHtml(string htmlContent);
    string ParsePdf(byte[] pdfBytes);
}