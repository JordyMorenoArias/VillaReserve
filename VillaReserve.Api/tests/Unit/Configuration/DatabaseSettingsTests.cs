using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using VillaReserve.Api.Infrastructure.Configuration;

namespace VillaReserve.Unit.Tests.Configuration;

/// <summary>
/// Unit tests for DatabaseSettings configuration validation.
/// Verifies that empty or missing connection strings fail DataAnnotations validation.
/// </summary>
public sealed class DatabaseSettingsTests
{
    [Fact]
    public void DatabaseSettings_WithValidConnectionString_PassesValidation()
    {
        // Arrange
        var settings = new DatabaseSettings
        {
            ConnectionString = "Host=localhost;Database=villareserve;Username=user;Password=pass"
        };
        var context = new ValidationContext(settings);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(settings, context, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DatabaseSettings_WithEmptyOrWhitespaceConnectionString_FailsValidation(string invalidConnectionString)
    {
        // Arrange
        var settings = new DatabaseSettings
        {
            ConnectionString = invalidConnectionString
        };
        var context = new ValidationContext(settings);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(settings, context, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(DatabaseSettings.ConnectionString));
    }
}

