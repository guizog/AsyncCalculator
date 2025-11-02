using Worker.Model;
using Worker.Interfaces;

namespace Worker.Services
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly IRecordRepository _recordRepo;

        public MessageProcessor(IRecordRepository repo)
        {
            _recordRepo = repo;
        }

        public async Task<bool> ProcessAsync(string id)
        {
            Record? mongoRecord = await _recordRepo.GetRecordAsync(id);

            if (mongoRecord == null)
                return false;

            int sum = (int)mongoRecord.number1 + (int)mongoRecord.number2;
            Console.WriteLine($" [x] Sum result of: {sum}");

            return await _recordRepo.UpdateRecordAsync(id, sum);
        }
    }
}
