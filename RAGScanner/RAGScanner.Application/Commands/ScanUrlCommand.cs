using Hangfire;
using MediatR;

public class ScanUrlCommand : IRequest<Result>
{
    public List<string> Urls { get; set; } = new();

    public class ScanUrlCommandHandler : IRequestHandler<ScanUrlCommand, Result>
    {
        private readonly IBackgroundJobClient jobClient;
        private readonly IJobStatusStore jobStatusStore;

        public ScanUrlCommandHandler(IBackgroundJobClient jobClient, IJobStatusStore jobStatusStore)
        {
            this.jobClient = jobClient;
            this.jobStatusStore = jobStatusStore;
        }

        public Task<Result> Handle(ScanUrlCommand request, CancellationToken cancellationToken)
        {
            if (request.Urls == null || !request.Urls.Any())
            {
                return Task.FromResult(Result.Failure(new[] { "No URLs provided for scanning." }));
            }

            request.Urls
                .Select(url => (JobId: jobStatusStore.CreateJob([url]), Url: url))
                .ToList()
                .ForEach(job =>
                    jobClient.Enqueue<IUrlScanJobService>(svc =>
                        svc.ProcessAsync(new List<string> { job.Url }, new Guid(job.JobId))));


            return Task.FromResult(Result.Success);
        }
    }
}