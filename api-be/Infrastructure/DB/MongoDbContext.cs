using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace api_be.Infrastructure.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDb:ConnectionString"];

            var databaseName = configuration["MongoDb:DatabaseName"];
            var client = new MongoClient(connectionString);
            // Sử dụng tên database tùy ý (ví dụ: "ChatDb")
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<api_be.Core.Entities.Message> Messages =>
            _database.GetCollection<api_be.Core.Entities.Message>("Messages");
    }
}
