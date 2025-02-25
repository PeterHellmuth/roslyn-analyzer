using Microsoft.Extensions.Logging;


namespace DemoProject.Services
{
    public interface ILoggingService
    {
        void LogValid(string userName, string action);
        void LogInvalid(string userName, string action);
    }
    public class LoggingService : ILoggingService
    {
        private readonly ILogger _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        // DEMO003: Use structured logging templates instead of string interpolation
        public void LogValid(string userName, string action)
        {
            // Valid structured logging
            _logger.LogInformation("User {userName} performed {action}", userName, action);
        }

        public void LogInvalid(string userName, string action)
        {
            // Invalid interpolated string
            _logger.LogInformation($"User {userName} performed {action}"); 
        }
    }
}