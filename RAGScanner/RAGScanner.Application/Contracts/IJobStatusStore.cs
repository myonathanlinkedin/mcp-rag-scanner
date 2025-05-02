public interface IJobStatusStore
{
    string CreateJob(List<string> urls);
    JobStatus GetJobStatus(string jobId);
    void UpdateJobStatus(string jobId, JobStatusType status, string message = null);
}