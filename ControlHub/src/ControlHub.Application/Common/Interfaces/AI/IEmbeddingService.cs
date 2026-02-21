namespace ControlHub.Application.Common.Interfaces.AI
{
    public interface IEmbeddingService
    {
        // Chuyển đổi text thành vector (Embedding)
        // Output: mảng float (VD: [0.1, 0.5, ...])
        Task<float[]> GenerateEmbeddingAsync(string text);
    }
}
