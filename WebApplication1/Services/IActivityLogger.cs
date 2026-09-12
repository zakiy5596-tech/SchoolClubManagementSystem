namespace WebApplication1.Services
{
    public interface IActivityLogger
    {
        Task LogAsync(string? userId, string userName, string action, string description);
    }
}
