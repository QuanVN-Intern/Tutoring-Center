namespace EduCenterManagement.Services
{
    public interface IAuditLogger
    {
        void LogActivity(string userEmail, string action, string details);
    }

    public class AuditLogger : IAuditLogger
    {
        private readonly ILogger<AuditLogger> _logger;

        public AuditLogger(ILogger<AuditLogger> logger)
        {
            _logger = logger;
        }

        public void LogActivity(string userEmail, string action, string details)
        {
            _logger.LogInformation("[AUDIT LOG] [{Timestamp}] User: {User} | Action: {Action} | Details: {Details}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), userEmail, action, details);
        }
    }
}
