using System.Reflection;
using System.Text.Json;
using Moq;
using TWL.Server.Security;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Constants;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Net.Network;
using TWL.Server.Persistence.Database;

namespace TWL.Tests.Market;

public class MarketContractTests
{
    private readonly Mock<IMarketService> _marketMock;
    private readonly Mock<MarketQueryService> _marketQueryMock;
    private readonly TestClientSession _session;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MarketContractTests()
    {
        _marketMock = new Mock<IMarketService>();
        _marketQueryMock = new Mock<MarketQueryService>(_marketMock.Object);
        _session = new TestClientSession(_marketMock.Object, _marketQueryMock.Object);

        // Manually set Character and UserId for testing via TestClientSession helper
        _session.SetCharacter(new ServerCharacter { Id = 123, Name = "TestPlayer" });
        _session.SetUserId(123);
    }

    [Fact]
    public async Task HandleMarketSearchRequest_Calls_MarketService_And_SendsResponse()
    {
        // Arrange
        var request = new MarketSearchRequest
        {
            Query = "Sword",
            Page = 1,
            PageSize = 10
        };
        var response = new MarketSearchResponse
        {
            Listings = new List<MarketListingDTO> { new() { ItemName = "Iron Sword", PricePerUnit = 100 } },
            TotalCount = 1,
            Page = 1,
            TotalPages = 1
        };

        _marketQueryMock.Setup(m => m.SearchAsync(It.IsAny<MarketSearchRequest>()))
            .ReturnsAsync(response);

        var msg = new NetMessage
        {
            Op = Opcode.MarketSearchRequest,
            JsonPayload = JsonSerializer.Serialize(request, _jsonOptions),
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion
        };

        // Act
        await InvokeHandleMessageAsync(msg);

        // Assert
        _marketQueryMock.Verify(m => m.SearchAsync(It.Is<MarketSearchRequest>(r => r.Query == "Sword")), Times.Once);
        Assert.Single(_session.SentMessages);
        Assert.Equal(Opcode.MarketSearchResponse, _session.SentMessages[0].Op);
        var sentResponse = JsonSerializer.Deserialize<MarketSearchResponse>(_session.SentMessages[0].JsonPayload, _jsonOptions);
        Assert.NotNull(sentResponse);
        Assert.Single(sentResponse.Listings);
        Assert.Equal("Iron Sword", sentResponse.Listings[0].ItemName);
    }

    [Fact]
    public async Task HandleMarketCreateRequest_Calls_MarketService_And_SendsResponse()
    {
        // Arrange
        var request = new CreateMarketListingRequest
        {
            ItemId = 1,
            Quantity = 5,
            PricePerUnit = 50,
            OperationId = "op-123"
        };
        var response = new MarketOperationResponse
        {
            Success = true,
            Message = "Listing created",
            ListingId = "listing-456"
        };

        _marketMock.Setup(m => m.CreateListingAsync(It.IsAny<ServerCharacter>(), It.IsAny<CreateMarketListingRequest>()))
            .ReturnsAsync(response);

        var msg = new NetMessage
        {
            Op = Opcode.MarketCreateRequest,
            JsonPayload = JsonSerializer.Serialize(request, _jsonOptions),
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion
        };

        // Act
        await InvokeHandleMessageAsync(msg);

        // Assert
        _marketMock.Verify(m => m.CreateListingAsync(It.IsAny<ServerCharacter>(), It.Is<CreateMarketListingRequest>(r => r.ItemId == 1 && r.Quantity == 5)), Times.Once);
        Assert.Single(_session.SentMessages);
        Assert.Equal(Opcode.MarketOperationResponse, _session.SentMessages[0].Op);
        var sentResponse = JsonSerializer.Deserialize<MarketOperationResponse>(_session.SentMessages[0].JsonPayload, _jsonOptions);
        Assert.NotNull(sentResponse);
        Assert.True(sentResponse.Success);
        Assert.Equal("listing-456", sentResponse.ListingId);
    }

    [Fact]
    public async Task HandleMarketBuyRequest_Calls_MarketService_And_SendsResponse()
    {
        // Arrange
        var request = new BuyMarketListingRequest
        {
            ListingId = "listing-456",
            OperationId = "op-789"
        };
        var response = new MarketOperationResponse
        {
            Success = true,
            Message = "Purchased successfully",
            NewBalance = 1000
        };

        _marketMock.Setup(m => m.BuyListingAsync(It.IsAny<ServerCharacter>(), It.IsAny<BuyMarketListingRequest>()))
            .ReturnsAsync(response);

        var msg = new NetMessage
        {
            Op = Opcode.MarketBuyRequest,
            JsonPayload = JsonSerializer.Serialize(request, _jsonOptions),
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion
        };

        // Act
        await InvokeHandleMessageAsync(msg);

        // Assert
        _marketMock.Verify(m => m.BuyListingAsync(It.IsAny<ServerCharacter>(), It.Is<BuyMarketListingRequest>(r => r.ListingId == "listing-456")), Times.Once);
        Assert.Single(_session.SentMessages);
        Assert.Equal(Opcode.MarketOperationResponse, _session.SentMessages[0].Op);
        var sentResponse = JsonSerializer.Deserialize<MarketOperationResponse>(_session.SentMessages[0].JsonPayload, _jsonOptions);
        Assert.NotNull(sentResponse);
        Assert.True(sentResponse.Success);
        Assert.Equal(1000, sentResponse.NewBalance);
    }

    [Fact]
    public async Task HandleMarketCancelRequest_Calls_MarketService_And_SendsResponse()
    {
        // Arrange
        var request = new CancelMarketListingRequest
        {
            ListingId = "listing-456"
        };
        var response = new MarketOperationResponse
        {
            Success = true,
            Message = "Listing cancelled"
        };

        _marketMock.Setup(m => m.CancelListingAsync(It.IsAny<ServerCharacter>(), It.IsAny<CancelMarketListingRequest>()))
            .ReturnsAsync(response);

        var msg = new NetMessage
        {
            Op = Opcode.MarketCancelRequest,
            JsonPayload = JsonSerializer.Serialize(request, _jsonOptions),
            SchemaVersion = ProtocolConstants.CurrentSchemaVersion
        };

        // Act
        await InvokeHandleMessageAsync(msg);

        // Assert
        _marketMock.Verify(m => m.CancelListingAsync(It.IsAny<ServerCharacter>(), It.Is<CancelMarketListingRequest>(r => r.ListingId == "listing-456")), Times.Once);
        Assert.Single(_session.SentMessages);
        Assert.Equal(Opcode.MarketOperationResponse, _session.SentMessages[0].Op);
        var sentResponse = JsonSerializer.Deserialize<MarketOperationResponse>(_session.SentMessages[0].JsonPayload, _jsonOptions);
        Assert.NotNull(sentResponse);
        Assert.True(sentResponse.Success);
    }

    private async Task InvokeHandleMessageAsync(NetMessage msg)
    {
        var method = typeof(ClientSession).GetMethod("HandleMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null) throw new InvalidOperationException("HandleMessageAsync not found");
        await (Task)method.Invoke(_session, new object[] { msg, "trace-id" })!;
    }

    public class TestClientSession : ClientSession
    {
        public List<NetMessage> SentMessages { get; } = new();

        public TestClientSession(IMarketService marketService, MarketQueryService marketQueryService) : base()
        {
            SetField("_marketService", marketService);
            SetField("_marketQueryService", marketQueryService);
            SetField("_rateLimiter", new RateLimiter(new RateLimiterOptions()));
            SetField("_replayGuard", new ReplayGuard(new ReplayGuardOptions()));
        }

        private void SetField(string fieldName, object? value)
        {
            var field = typeof(ClientSession).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(this, value);
            }
        }

        public void SetCharacter(ServerCharacter character)
        {
            Character = character;
        }

        public void SetUserId(int userId)
        {
            UserId = userId;
        }

        public override Task SendAsync(NetMessage msg)
        {
            SentMessages.Add(msg);
            return Task.CompletedTask;
        }
    }
}
