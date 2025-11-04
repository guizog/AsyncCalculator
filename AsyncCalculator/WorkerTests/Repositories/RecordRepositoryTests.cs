using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading;
using System.Threading.Tasks;
using Worker.Model;
using Worker.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyModel;

namespace WorkerTests;

[TestClass]
public class RecordRepositoryTests
{
    private Mock<IMongoCollection<Record>> _mockCollection;
    private Mock<IMongoDatabase> _mockDatabase;
    private Mock<IAsyncCursor<Record>> _mockCursor;
    private Mock<IMongoClient> _mockClient;
    private Mock<ILogger<RecordRepository>> _mockLogger;
    private RecordRepository _repository;
    private Mock<IFindFluent<Record, Record>> _mockFindFluent;



    [TestInitialize]
    public void Setup()
    {
        _mockCollection = new Mock<IMongoCollection<Record>>();
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCursor = new Mock<IAsyncCursor<Record>>();
        _mockClient = new Mock<IMongoClient>();
        _mockLogger = new Mock<ILogger<RecordRepository>>();
        _mockFindFluent = new Mock<IFindFluent<Record, Record>>();

        _mockClient
            .Setup(c => c.GetDatabase("asynccalculator", null))
            .Returns(_mockDatabase.Object);

        _mockDatabase
            .Setup(db => db.GetCollection<Record>("records", null))
            .Returns(_mockCollection.Object);

        _repository = new RecordRepository(_mockClient.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetRecordAsync_ShouldReturnRecordWhenRecordExists()
    {
        // Arrange section
        Record record = new Record();
        record._id = Guid.NewGuid().ToString();
        record.number1 = 10;
        record.number2 = 7;
        record.result = 17;
        record.status = "FINISHED";

        _mockCursor
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCursor
            .SetupGet(c => c.Current)
            .Returns(new List<Record> { record });

        _mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Record>>(),
                It.IsAny<FindOptions<Record>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCursor.Object);


        // Act section
        var actResult = await _repository.GetRecordAsync(record._id);


        // Assert section
        Assert.IsNotNull(actResult);
        Assert.AreEqual(record._id, actResult._id);

        _mockLogger.Verify(
                log => log.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("found")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

    }

    [TestMethod]
    public async Task GetRecordAsync_ShouldReturnNullWhenRecordNotFound()
    {
        //  Arrange section
        _mockCursor
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockCursor
            .SetupGet(c => c.Current)
            .Returns(new List<Record>() );

        _mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Record>>(),
                It.IsAny<FindOptions<Record>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCursor.Object);

        //  Act section
        var actResult = await _repository.GetRecordAsync(Guid.NewGuid().ToString());

        //  Assert section
        Assert.IsNull(actResult);

        _mockLogger.Verify(
                log => log.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("not returned")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

    }
}
