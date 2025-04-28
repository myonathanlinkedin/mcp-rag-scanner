using MCPClient.MCPClientServices;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Commands.MCP
{
    public class UserPromptCommand : IRequest<Result<string>>
    {
        public string Prompt { get; }

        public UserPromptCommand(string prompt) => Prompt = prompt;

        public class UserPromptCommandHandler : IRequestHandler<UserPromptCommand, Result<string>>
        {
            private readonly IIdentity identity;
            private readonly IMCPServerRequester mCPServerRequester;

            public UserPromptCommandHandler(IIdentity identity, IMCPServerRequester mCPServerRequester)
            {
                this.identity = identity;
                this.mCPServerRequester = mCPServerRequester;
            }

            public async Task<Result<string>> Handle(UserPromptCommand request, CancellationToken cancellationToken)
            {
               return await this.mCPServerRequester.RequestAsync(request.Prompt);
            }
        }
    }
}