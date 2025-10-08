using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HotelReservationSystem.Services.BookingCom;
using HotelReservationSystem.Models.BookingCom;

namespace HotelReservationSystem.Tests.Services.BookingCom;

public class XmlSerializationServiceTests
{
    private readonly Mock<ILogger<XmlSerializationService>> _loggerMock;
    private readonly XmlSerializationService _service;

    public XmlSerializationServiceTests()
    {
        _loggerMock = new Mock<ILogger<XmlSerializationService>>();
        _service = new XmlSerializationService(_loggerMock.Object);
    }

    [Fact]
    public void Serialize_ValidObject_ReturnsXmlString()
    {
        // Arrange
        var request = new BookingComRequest
        {
            Version = "1.0",
            Product = "BookingCom",
            Authentication = new BookingComAuthentication
            {
                Username = "testuser",
                Password = "testpass"
            }
        };

        // Act
        var result = _service.Serialize(request);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<?xml version=\"1.0\"");
        result.Should().Contain("<request");
        result.Should().Contain("version=\"1.0\"");
        result.Should().Contain("product=\"BookingCom\"");
        result.Should().Contain("<authentication>");
        result.Should().Contain("<username>testuser</username>");
        result.Should().Contain("<password>testpass</password>");
    }

    [Fact]
    public void Serialize_NullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _service.Serialize<BookingComRequest>(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Deserialize_ValidXml_ReturnsObject()
    {
        // Arrange
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<request version=""1.0"" product=""BookingCom"">
  <authentication>
    <username>testuser</username>
    <password>testpass</password>
  </authentication>
</request>";

        // Act
        var result = _service.Deserialize<BookingComRequest>(xml);

        // Assert
        result.Should().NotBeNull();
        result.Version.Should().Be("1.0");
        result.Product.Should().Be("BookingCom");
        result.Authentication.Should().NotBeNull();
        result.Authentication.Username.Should().Be("testuser");
        result.Authentication.Password.Should().Be("testpass");
    }

    [Fact]
    public void Deserialize_InvalidXml_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidXml = "<invalid>xml</structure>";

        // Act & Assert
        var action = () => _service.Deserialize<BookingComRequest>(invalidXml);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deserialize_NullOrEmptyXml_ThrowsArgumentException()
    {
        // Act & Assert
        var action1 = () => _service.Deserialize<BookingComRequest>(null!);
        action1.Should().Throw<ArgumentException>();

        var action2 = () => _service.Deserialize<BookingComRequest>("");
        action2.Should().Throw<ArgumentException>();

        var action3 = () => _service.Deserialize<BookingComRequest>("   ");
        action3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryDeserialize_ValidXml_ReturnsTrueAndObject()
    {
        // Arrange
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<request version=""1.0"" product=""BookingCom"">
  <authentication>
    <username>testuser</username>
    <password>testpass</password>
  </authentication>
</request>";

        // Act
        var success = _service.TryDeserialize<BookingComRequest>(xml, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0");
        result.Authentication.Username.Should().Be("testuser");
    }

    [Fact]
    public void TryDeserialize_InvalidXml_ReturnsFalseAndNull()
    {
        // Arrange
        var invalidXml = "<invalid>xml</structure>";

        // Act
        var success = _service.TryDeserialize<BookingComRequest>(invalidXml, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryDeserialize_NullOrEmptyXml_ReturnsFalseAndNull()
    {
        // Act & Assert
        var success1 = _service.TryDeserialize<BookingComRequest>(null!, out var result1);
        success1.Should().BeFalse();
        result1.Should().BeNull();

        var success2 = _service.TryDeserialize<BookingComRequest>("", out var result2);
        success2.Should().BeFalse();
        result2.Should().BeNull();
    }

    [Fact]
    public void Serialize_ComplexReservationObject_ReturnsValidXml()
    {
        // Arrange
        var reservation = new BookingComReservation
        {
            Id = "12345",
            Status = "confirmed",
            HotelId = 1,
            RoomId = 101,
            CheckIn = "2024-01-15",
            CheckOut = "2024-01-18",
            GuestName = "John Doe",
            GuestEmail = "john.doe@example.com",
            GuestPhone = "+1234567890",
            NumberOfGuests = 2,
            TotalAmount = 450.00m,
            Currency = "USD",
            SpecialRequests = "Late check-in",
            CreatedAt = "2024-01-01T10:00:00Z",
            UpdatedAt = "2024-01-01T10:00:00Z"
        };

        // Act
        var result = _service.Serialize(reservation);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<reservation");
        result.Should().Contain("id=\"12345\"");
        result.Should().Contain("status=\"confirmed\"");
        result.Should().Contain("<guest_name>John Doe</guest_name>");
        result.Should().Contain("<total_amount>450.00</total_amount>");
    }

    [Fact]
    public void Deserialize_ReservationSyncResponse_ReturnsObjectWithReservations()
    {
        // Arrange
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<response version=""1.0"">
  <ok />
  <reservations>
    <reservation id=""12345"" status=""confirmed"">
      <hotel_id>1</hotel_id>
      <room_id>101</room_id>
      <checkin>2024-01-15</checkin>
      <checkout>2024-01-18</checkout>
      <guest_name>John Doe</guest_name>
      <guest_email>john.doe@example.com</guest_email>
      <number_of_guests>2</number_of_guests>
      <total_amount>450.00</total_amount>
      <currency>USD</currency>
    </reservation>
    <reservation id=""67890"" status=""pending"">
      <hotel_id>1</hotel_id>
      <room_id>102</room_id>
      <checkin>2024-01-20</checkin>
      <checkout>2024-01-22</checkout>
      <guest_name>Jane Smith</guest_name>
      <guest_email>jane.smith@example.com</guest_email>
      <number_of_guests>1</number_of_guests>
      <total_amount>200.00</total_amount>
      <currency>USD</currency>
    </reservation>
  </reservations>
</response>";

        // Act
        var result = _service.Deserialize<ReservationSyncResponse>(xml);

        // Assert
        result.Should().NotBeNull();
        result.Reservations.Should().HaveCount(2);
        
        var firstReservation = result.Reservations[0];
        firstReservation.Id.Should().Be("12345");
        firstReservation.Status.Should().Be("confirmed");
        firstReservation.GuestName.Should().Be("John Doe");
        firstReservation.TotalAmount.Should().Be(450.00m);
        
        var secondReservation = result.Reservations[1];
        secondReservation.Id.Should().Be("67890");
        secondReservation.Status.Should().Be("pending");
        secondReservation.GuestName.Should().Be("Jane Smith");
        secondReservation.TotalAmount.Should().Be(200.00m);
    }
}