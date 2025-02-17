using Microsoft.Extensions.Logging;


namespace DemoProject.Services
{
    public interface ILoggingService
    {
        void LogValid(string userName);
        void LogInvalid(string userName);
    }
    public class LoggingService : ILoggingService
    {
        private readonly ILogger _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        // DEMO003: Use structured logging templates instead of string interpolation
        public void LogValid(string userName)
        {
            // Valid structured logging
            _logger.LogInformation("User {userName} logged in", userName);
        }

        public void LogInvalid(string userName)
        {
            // Invalid interpolated string
            _logger.LogInformation($"User {userName} logged in"); 
        }
    }
}