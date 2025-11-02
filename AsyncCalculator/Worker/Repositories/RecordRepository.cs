using Worker.Model;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Worker.Interfaces;

namespace Worker.Repositories
{
    public class RecordRepository : IRecordRepository
    {
        private readonly IMongoDatabase _database;

        public RecordRepository(MongoClient client)
        {
            _database = client.GetDatabase("asynccalculator");
        }

        public async Task<Record?> GetRecordAsync(string id)
        {
            try
            {
                BsonValue recordId = BsonValue.Create(id);
                var collection = _database.GetCollection<Record>("records");
                var filter = Builders<Record>.Filter.Eq("_id", BsonValue.Create(id));

                Record document = await collection.Find(filter).FirstOrDefaultAsync();

                if(document == null)
                {
                    Console.WriteLine($" [!] Record ${id} not returned in the mongodb query.");
                }

                Console.WriteLine($" [x] Record with Id {id} found");
                Console.WriteLine(document.ToJson(new JsonWriterSettings { Indent = true }));

                return document;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error querying record: {ex.Message}");
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
                    Console.WriteLine($" [x] Updated with success id: {id}");
                    return true;
                }
                Console.WriteLine($" [!] Failed to update id: {id}");
                return false;

            }
            catch(Exception ex)
            {
                Console.WriteLine($"[!] Error updating record: {ex.Message}");
                throw;
            }
        }
    }
}
