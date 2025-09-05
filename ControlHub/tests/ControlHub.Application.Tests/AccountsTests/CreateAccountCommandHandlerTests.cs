using ControlHub.Application.Accounts.Commands.CreateAccount;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Results;
using Moq;
using Xunit;

public class CreateAccountCommandHandlerTests
{
    private readonly Mock<IAccountValidator> _accountValidatorMock = new();
    private readonly Mock<IAccountCommands> _accountCommandsMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly CreateAccountCommandHandler _handler;

    public CreateAccountCommandHandlerTests()
    {
        _handler = new CreateAccountCommandHandler(
            _accountValidatorMock.Object,
            _accountCommandsMock.Object,
            _passwordHasherMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenEmailInvalid()
    {
        // Arrange
        var command = new CreateAccountCommand("invalid-email", "Pass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("UC error", result.Error); // vì Email.Create() sẽ throw exception
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "Pass123!");

        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Email is exist", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPasswordInvalid()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "short");

        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ReturnsAsync(Result<bool>.Success(false));

        _accountValidatorMock
            .Setup(v => v.ValidatePassword(It.IsAny<string>()))
            .Returns(Result<bool>.Success(false)); // password không hợp lệ

        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns((new byte[] { 1 }, new byte[] { 2 }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Password is not validation", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenAllValid()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "ValidPass123!");

        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ReturnsAsync(Result<bool>.Success(false));

        _accountValidatorMock
            .Setup(v => v.ValidatePassword(It.IsAny<string>()))
            .Returns(Result<bool>.Success(true));

        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns((new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenExceptionThrown()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "Pass123!");

        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UC error", result.Error);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPasswordHasherReturnsNull()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "ValidPass123!");
        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ReturnsAsync(Result<bool>.Success(false));
        _accountValidatorMock
            .Setup(v => v.ValidatePassword(It.IsAny<string>()))
            .Returns(Result<bool>.Success(true));
        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns(((byte[]?)null, (byte[]?)null)!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UC error", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAccountCommandsReturnsFailure()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "ValidPass123!");
        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ReturnsAsync(Result<bool>.Success(false));
        _accountValidatorMock
            .Setup(v => v.ValidatePassword(It.IsAny<string>()))
            .Returns(Result<bool>.Success(true));
        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns((new byte[] { 1 }, new byte[] { 2 }));

        _accountCommandsMock
            .Setup(c => c.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync(Result<bool>.Failure("Insert error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Insert error", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAccountCommandsThrowsException()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "ValidPass123!");
        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ReturnsAsync(Result<bool>.Success(false));
        _accountValidatorMock
            .Setup(v => v.ValidatePassword(It.IsAny<string>()))
            .Returns(Result<bool>.Success(true));
        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns((new byte[] { 1 }, new byte[] { 2 }));

        _accountCommandsMock
            .Setup(c => c.AddAsync(It.IsAny<Account>()))
            .ThrowsAsync(new Exception("DB crash"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UC error", result.Error);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPasswordValidatorThrowsException()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "ValidPass123!");
        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ReturnsAsync(Result<bool>.Success(false));
        _accountValidatorMock
            .Setup(v => v.ValidatePassword(It.IsAny<string>()))
            .Throws(new Exception("Regex engine crash"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UC error", result.Error);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenEmailCheckReturnsFailure()
    {
        // Arrange
        var command = new CreateAccountCommand("test@example.com", "ValidPass123!");
        _accountValidatorMock
            .Setup(v => v.EmailIsExistAsync(It.IsAny<Email>()))
            .ReturnsAsync(Result<bool>.Failure("DB error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("DB error", result.Error);
    }
}