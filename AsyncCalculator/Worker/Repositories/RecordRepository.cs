using Worker.Model;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Worker.Interfaces;
using Microsoft.Extensions.Logging;

namespace Worker.Repositories
{
    public class RecordRepository : IRecordRepository
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<RecordRepository> _logger;

        public RecordRepository(IMongoClient client, ILogger<RecordRepository> logger)
        {
            _database = client.GetDatabase("asynccalculator");
            _logger = logger;
        }

        public async Task<Record?> GetRecordAsync(string id)
        {
            try
            {
                BsonValue recordId = BsonValue.Create(id);
                var collection = _database.GetCollection<Record>("records");
                var filter = Builders<Record>.Filter.Eq("_id", BsonValue.Create(id));

                //  foi necessário sair do Find() e ir para o FindAsync(), pois o Find() é método de extensão e o moq não consegue mockar ele :(
                var cursor = await collection.FindAsync(filter, new FindOptions<Record>());
                Record? document = null;
                using (cursor)
                {
                    if (await cursor.MoveNextAsync())
                        document = cursor.Current.FirstOrDefault();
                }

                if (document == null)
                {
                    _logger.LogError("Record {id} not returned in the mongodb query.", id);
                    return null;
                }

                _logger.LogInformation("Record with Id {id} found", id);
                Console.WriteLine(document.ToJson(new JsonWriterSettings { Indent = true }));

                return document;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying record id: {id}", id);
                throw;
            }
        }

        public async Task<bool> UpdateRecordAsync(string id, int result)
        {
            try
            {
                var collection = _database.GetCollection<Record>("records");
                var filter = Builders<Record>.Filter.Eq("_id", BsonValue.Create(id));

                var update = Builders<Record>.Update
                    .Set("result", result)
                    .Set("status", "FINISHED");

                UpdateResult updateResult = await collection.UpdateOneAsync(filter, update);
                if (updateResult.ModifiedCount > 0)
                {
                    _logger.LogInformation("Updated with success id: {id}", id);
                    return true;
                }
                _logger.LogError("Failed to update id: {id}", id);
                return false;

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error updating record id: {id}", id);
                throw;
            }
        }
    }
}
