using Worker.Model;

namespace Worker.Interfaces
{
    public interface IRecordRepository
    {
        Task<Record?> GetRecordAsync(string id);
        Task<bool> UpdateRecordAsync(string id, int result);
    }
}
