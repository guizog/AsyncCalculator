using Worker.Model;
using Worker.Interfaces;
using Microsoft.Extensions.Logging;

namespace Worker.Services
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly IRecordRepository _recordRepo;
        private readonly ILogger<MessageProcessor> _logger;

        public MessageProcessor(IRecordRepository repo, ILogger<MessageProcessor> logger)
        {
            _recordRepo = repo;
            _logger = logger;
        }

        public async Task<bool> ProcessAsync(string id)
        {
            Record? mongoRecord = await _recordRepo.GetRecordAsync(id);

            if (mongoRecord == null)
                return false;

            int sum = (int)mongoRecord.number1 + (int)mongoRecord.number2;
            _logger.LogInformation("Sum result of: {sum}", sum);

            return await _recordRepo.UpdateRecordAsync(id, sum);
        }
    }
}
