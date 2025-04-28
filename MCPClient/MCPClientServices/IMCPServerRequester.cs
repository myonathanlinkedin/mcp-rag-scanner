using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCPClient.MCPClientServices
{
    public interface IMCPServerRequester
    {
        Task<Result<string>> RequestAsync(string prompt);
    }
}
