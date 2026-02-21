namespace ControlHub.Application.Common.Interfaces.AI
{
    public interface IVectorDatabase
    {
        // Lưu (hoặc update) vector vào DB
        // collectionName: Tên bảng (VD: "LogDefinitions")
        // id: ID duy nhất của record (VD: "Account.SignIn.InvalidPassword")
        // vector: Dãy số float đại diện cho ngữ nghĩa
        // payload: Dữ liệu gốc đi kèm (VD: { "Description": "User typed wrong password" })
        Task UpsertAsync(string collectionName, string id, float[] vector, Dictionary<string, object> payload);

        // Tìm kiếm vector tương đồng
        // vector: Vector của câu truy vấn (VD: "Login error")
        // limit: Số lượng kết quả trả về
        Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int limit = 3);
    }

    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public double Score { get; set; } // Độ tương đồng (0.0 -> 1.0)
        public Dictionary<string, object> Payload { get; set; } = new();
    }
}
