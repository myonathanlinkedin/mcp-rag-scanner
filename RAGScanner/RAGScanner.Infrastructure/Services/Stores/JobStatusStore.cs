using Microsoft.Extensions.Logging;

internal class JobStatusStore : IJobStatusStore
{
    private readonly RAGDbContext context;
    private readonly ILogger<JobStatusStore> logger;

    public JobStatusStore(RAGDbContext context, ILogger<JobStatusStore> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public string CreateJob(List<string> urls)
    {
        var jobId = Guid.NewGuid().ToString();

        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Urls = urls,
            Status = JobStatusType.Pending.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            Message = "Started"
        };

        context.JobStatuses.Add(jobStatus);
        context.SaveChanges();

        logger.LogInformation("Created job with ID {JobId}", jobId);
        return jobId;
    }

    public JobStatus GetJobStatus(string jobId)
    {
        return context.JobStatuses.FirstOrDefault(js => js.JobId == jobId);
    }

    public void UpdateJobStatus(string jobId, JobStatusType status, string message = null)
    {
        try
        {
            var job = context.JobStatuses.FirstOrDefault(js => js.JobId == jobId);
            if (job is null)
            {
                logger.LogWarning("Attempted to update non-existent job with ID {JobId}", jobId);
                return;
            }

            job.Message = message ?? job.Message;
            job.UpdatedAt = DateTime.UtcNow;
            job.Status = status.ToString();

            context.SaveChanges();
            logger.LogInformation("Updated job {JobId} with message: {Message}", jobId, job.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update job status for JobId {JobId}", jobId);
        }
    }
}
