public interface IMCPServerRequester
{
    Task<Result<string>> RequestAsync(string prompt);
}