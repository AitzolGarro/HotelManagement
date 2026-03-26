using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using HotelReservationSystem.Models;
using HotelReservationSystem.Services;
namespace HotelReservationSystem.Tests.Services;

// Trait constants (TestConfiguration excluded — NUnit dependency)
internal static class TestTraits { public const string Category = "Category"; public const string Feature = "Feature"; public const string Duration = "Duration"; public const string Priority = "Priority"; }
internal static class TestCategories { public const string Unit = "Unit"; public const string Integration = "Integration"; }
internal static class TestDurations { public const string Fast = "Fast"; public const string Slow = "Slow"; }

/// <summary>
/// Unit tests for TwoFactorService — EnableAsync, DisableAsync, VerifyCodeAsync, recovery codes.
/// Uses Moq to isolate UserManager operations.
/// </summary>
public class TwoFactorServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ILogger<TwoFactorService>> _loggerMock;
    private readonly TwoFactorService _service;

    public TwoFactorServiceTests()
    {
        // UserManager requires a store mock
        var storeMock = new Mock<IUserStore<User>>();
        var identityOptions = Options.Create(new IdentityOptions
        {
            Tokens = new TokenOptions { AuthenticatorTokenProvider = "Authenticator" }
        });
        _userManagerMock = new Mock<UserManager<User>>(
            storeMock.Object, identityOptions, null!, null!, null!, null!, null!, null!, null!);
        _loggerMock = new Mock<ILogger<TwoFactorService>>();

        _service = new TwoFactorService(_userManagerMock.Object, _loggerMock.Object);
    }

    private static User CreateTestUser(int id = 1, string email = "test@example.com")
    {
        return new User
        {
            Id = id,
            Email = email,
            UserName = email,
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };
    }

    // ─── EnableAsync Tests ────────────────────────────────────────────────────

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task EnableAsync_ValidTotpCode_EnablesTwoFactorAndReturnsRecoveryCodes()
    {
        // Arrange
        var user = CreateTestUser();
        var recoveryCodes = new[] { "CODE1", "CODE2", "CODE3", "CODE4", "CODE5", "CODE6", "CODE7", "CODE8" };

        _userManagerMock.Setup(um => um.VerifyTwoFactorTokenAsync(user, "Authenticator", "123456"))
            .ReturnsAsync(true);
        _userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(user, true))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(um => um.GenerateNewTwoFactorRecoveryCodesAsync(user, 8))
            .ReturnsAsync(recoveryCodes);

        // Act
        var (success, codes) = await _service.EnableAsync(user, "123456");

        // Assert
        success.Should().BeTrue();
        codes.Should().NotBeNull();
        codes!.Count().Should().Be(8);
        _userManagerMock.Verify(um => um.SetTwoFactorEnabledAsync(user, true), Times.Once);
        _userManagerMock.Verify(um => um.GenerateNewTwoFactorRecoveryCodesAsync(user, 8), Times.Once);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task EnableAsync_InvalidTotpCode_ReturnsFalseWithoutEnabling()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(um => um.VerifyTwoFactorTokenAsync(user, "Authenticator", "000000"))
            .ReturnsAsync(false);

        // Act
        var (success, codes) = await _service.EnableAsync(user, "000000");

        // Assert
        success.Should().BeFalse();
        codes.Should().BeNull();
        _userManagerMock.Verify(um => um.SetTwoFactorEnabledAsync(It.IsAny<User>(), true), Times.Never);
    }

    // ─── DisableAsync Tests ───────────────────────────────────────────────────

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task DisableAsync_CorrectPassword_DisablesTwoFactor()
    {
        // Arrange
        var user = CreateTestUser();

        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, "correct-password"))
            .ReturnsAsync(true);
        _userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.DisableAsync(user, "correct-password");

        // Assert
        result.Should().BeTrue();
        _userManagerMock.Verify(um => um.SetTwoFactorEnabledAsync(user, false), Times.Once);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task DisableAsync_WrongPassword_ReturnsFalseWithoutDisabling()
    {
        // Arrange
        var user = CreateTestUser();

        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, "wrong-password"))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DisableAsync(user, "wrong-password");

        // Assert
        result.Should().BeFalse();
        _userManagerMock.Verify(um => um.SetTwoFactorEnabledAsync(It.IsAny<User>(), false), Times.Never);
    }

    // ─── VerifyCodeAsync (User overload) Tests ────────────────────────────────

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task VerifyCodeAsync_ValidTotpCode_ReturnsTrue()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(um => um.VerifyTwoFactorTokenAsync(user, "Authenticator", "123456"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.VerifyCodeAsync(user, "123456");

        // Assert
        result.Should().BeTrue();
        // Recovery code should NOT be attempted when TOTP succeeds
        _userManagerMock.Verify(um => um.RedeemTwoFactorRecoveryCodeAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task VerifyCodeAsync_InvalidTotpButValidRecoveryCode_ReturnsTrue()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(um => um.VerifyTwoFactorTokenAsync(user, "Authenticator", "RECOVERY1"))
            .ReturnsAsync(false);
        _userManagerMock.Setup(um => um.RedeemTwoFactorRecoveryCodeAsync(user, "RECOVERY1"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.VerifyCodeAsync(user, "RECOVERY1");

        // Assert
        result.Should().BeTrue();
        _userManagerMock.Verify(um => um.RedeemTwoFactorRecoveryCodeAsync(user, "RECOVERY1"), Times.Once);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task VerifyCodeAsync_BothTotpAndRecoveryFail_ReturnsFalse()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(um => um.VerifyTwoFactorTokenAsync(user, "Authenticator", "000000"))
            .ReturnsAsync(false);
        _userManagerMock.Setup(um => um.RedeemTwoFactorRecoveryCodeAsync(user, "000000"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid recovery code" }));

        // Act
        var result = await _service.VerifyCodeAsync(user, "000000");

        // Assert
        result.Should().BeFalse();
    }

    // ─── Recovery Code Tests ──────────────────────────────────────────────────

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task GenerateRecoveryCodesAsync_Returns8Codes()
    {
        // Arrange
        var user = CreateTestUser();
        var codes = Enumerable.Range(1, 8).Select(i => $"RECOVERY{i:00}").ToArray();

        _userManagerMock.Setup(um => um.GenerateNewTwoFactorRecoveryCodesAsync(user, 8))
            .ReturnsAsync(codes);

        // Act
        var result = await _service.GenerateRecoveryCodesAsync(user);

        // Assert
        result.Should().HaveCount(8);
        _userManagerMock.Verify(um => um.GenerateNewTwoFactorRecoveryCodesAsync(user, 8), Times.Once);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task VerifyRecoveryCodeAsync_ConsumedCode_ReturnsFalse()
    {
        // Arrange
        var user = CreateTestUser();

        _userManagerMock.Setup(um => um.RedeemTwoFactorRecoveryCodeAsync(user, "USED-CODE"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Code already used" }));

        // Act
        var result = await _service.VerifyRecoveryCodeAsync(user, "USED-CODE");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task VerifyRecoveryCodeAsync_ValidCode_ReturnsTrue()
    {
        // Arrange
        var user = CreateTestUser();

        _userManagerMock.Setup(um => um.RedeemTwoFactorRecoveryCodeAsync(user, "VALID-CODE"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.VerifyRecoveryCodeAsync(user, "VALID-CODE");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Feature, "TwoFactor")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task GetRemainingRecoveryCodeCountAsync_ReturnsCount()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(um => um.CountRecoveryCodesAsync(user)).ReturnsAsync(5);

        // Act
        var count = await _service.GetRemainingRecoveryCodeCountAsync(user);

        // Assert
        count.Should().Be(5);
    }
}
