namespace ControlHub.Application.Common.Interfaces.AI
{
    public interface IAIAnalysisService
    {
        // Gửi prompt đã được "lắp ghép" (RAG Context + Logs) cho AI xử lý
        Task<string> AnalyzeLogsAsync(string prompt);
    }
}
