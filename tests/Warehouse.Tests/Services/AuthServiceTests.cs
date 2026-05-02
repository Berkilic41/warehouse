using Moq;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.Helpers;
using Warehouse.Service.Services;
using Xunit;

namespace Warehouse.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo;
    private readonly AuthService           _service;

    public AuthServiceTests()
    {
        _userRepo = new Mock<IUserRepository>();
        _service  = new AuthService(_userRepo.Object);
    }

    private static User MakeUser(string password, bool isActive = true, string role = "Staff")
    {
        var (hash, salt) = PasswordHasher.Hash(password);
        return new User
        {
            Id = 1, Email = "user@test.com", Username = "testuser",
            PasswordHash = hash, PasswordSalt = salt,
            IsActive = isActive, Role = role
        };
    }

    // ─── LoginAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var user = MakeUser("Password123!");
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync("user@test.com", "Password123!");

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal(user.Id, result.User!.Id);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_EmailNotFound_ReturnsFail()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("ghost@test.com")).ReturnsAsync((User?)null);

        var result = await _service.LoginAsync("ghost@test.com", "any");

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFail()
    {
        var user = MakeUser("CorrectPass");
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync("user@test.com", "WrongPass");

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_InactiveAccount_ReturnsFail()
    {
        var user = MakeUser("Pass123!", isActive: false);
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync("user@test.com", "Pass123!");

        Assert.False(result.Success);
        Assert.Equal("This account is disabled.", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task LoginAsync_InactiveAccount_PasswordNeverChecked()
    {
        // Disabled accounts should fail before password check
        var user = MakeUser("Pass123!", isActive: false);
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(user);

        // Even with the correct password, disabled accounts fail immediately
        var result = await _service.LoginAsync("user@test.com", "Pass123!");

        Assert.False(result.Success);
        Assert.Equal("This account is disabled.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Staff")]
    [InlineData("Viewer")]
    public async Task LoginAsync_AnyActiveRole_Succeeds(string role)
    {
        var user = MakeUser("Pass123!", isActive: true, role: role);
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync("user@test.com", "Pass123!");

        Assert.True(result.Success);
        Assert.Equal(role, result.User!.Role);
    }

    [Fact]
    public async Task LoginAsync_EmailNotFound_PasswordNeverChecked()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _service.LoginAsync("noone@test.com", "anypass");

        // Should not throw; just return fail with same generic message
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_EmptyPassword_ReturnsFail()
    {
        var user = MakeUser("RealPass");
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync("user@test.com", "");

        Assert.False(result.Success);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsUser()
    {
        var user = MakeUser("pass");
        user.Id = 5;
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

        var result = await _service.GetByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _userRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }
}
