using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using NSubstitute.ReturnsExtensions;
using Users.Api.Logging;
using Users.Api.Models;
using Users.Api.Repositories;
using Users.Api.Services;
using Xunit;

namespace Users.Api.Tests.Unit.Application;

public class UserServiceTests
{
    private readonly UserService _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ILoggerAdapter<UserService> _logger = Substitute.For<ILoggerAdapter<UserService>>();

    public UserServiceTests()
    {
        _sut = new UserService(_userRepository, _logger);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        // Arrange
        _userRepository.GetAllAsync().Returns(Enumerable.Empty<User>());

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsers_WhenSomeUsersExist()
    {
        // Arrange
        var nickChapsas = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Brayan Giraldo"
        };
        var expectedUsers = new[]
        {
            nickChapsas
        };
        _userRepository.GetAllAsync().Returns(expectedUsers);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        //result.Single().Should().BeEquivalentTo(nickChapsas);
        result.Should().BeEquivalentTo(expectedUsers);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogMessages_WhenInvoked()
    {
        // Arrange
        _userRepository.GetAllAsync().Returns(Enumerable.Empty<User>());

        // Act
        await _sut.GetAllAsync();

        // Assert
        _logger.Received(1).LogInformation(Arg.Is("Retrieving all users"));
        _logger.Received(1).LogInformation(Arg.Is("All users retrieved in {0}ms"), Arg.Any<long>());
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogMessageAndException_WhenExceptionIsThrown()
    {
        // Arrange
        var sqliteException = new SqliteException("Something went wrong", 500);
        _userRepository.GetAllAsync()
            .Throws(sqliteException);

        // Act
        var requestAction = async () => await _sut.GetAllAsync();

        // Assert
        await requestAction.Should()
            .ThrowAsync<SqliteException>().WithMessage("Something went wrong");
        _logger.Received(1).LogError(Arg.Is(sqliteException), Arg.Is("Something went wrong while retrieving all users"));
    }
    

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        //Arrange
        var expectedUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Brayan Giraldo"
        };

        _userRepository.GetByIdAsync(expectedUser.Id).Returns(expectedUser);

        //Act
        var userResult = await _sut.GetByIdAsync(expectedUser.Id);

        //Assert
        userResult.Should().NotBeNull();
        userResult.Should().BeEquivalentTo(expectedUser);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        //Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId).ReturnsNull();
        
        //Act
        var userResult = await _sut.GetByIdAsync(userId);
        
        //Assert
        userResult.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogMessages_WhenRetrievingUsers()
    {
        //Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId).Returns(new User(){Id = userId});

        //Act
        await _sut.GetByIdAsync(userId);

        //Assert
        _logger.Received(1).LogInformation(Arg.Is("Retrieving user with id: {0}"), Arg.Is(userId));
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} retrieved in {1}ms"), Arg.Is(userId), Arg.Any<long>());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogErrorMessage_WhenExceptionIsThrown()
    {
        //Arrange
        var expectedException = new SqliteException("General Exception in the DB", 500);
        _userRepository.GetByIdAsync(Arg.Any<Guid>()).Throws(expectedException);

        //Act
        var exceptionResult = async () => await _sut.GetByIdAsync(Guid.NewGuid());

        //Assert
        await exceptionResult.Should().ThrowAsync<SqliteException>()
            .WithMessage("General Exception in the DB");;
        
        _logger.Received(1).LogError(Arg.Is(expectedException), Arg.Is("Something went wrong while retrieving user with id {0}"), Arg.Any<Guid>());
    }
    
    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ShouldCreateUser_WhenUserDetailsAreValid()
    {
        //Arrange
        User newUser = new() { Id = Guid.NewGuid(), FullName = "Brayan Giraldo" }; 
        _userRepository.CreateAsync(newUser).Returns(true);

        //Act
        var result = await _sut.CreateAsync(newUser);

        //Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task CreateAsync_ShouldLogMessages_WhenCreatingUser()
    {
        //Arrang
        _userRepository.CreateAsync(Arg.Any<User>()).Returns(true);

        //Act
        var result = await _sut.CreateAsync(new User());

        //Assert
        _logger.Received(1).LogInformation(Arg.Is("Creating user with id {0} and name: {1}"), Arg.Any<Guid>(), Arg.Any<string>());
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} created in {1}ms"), Arg.Any<Guid>(), Arg.Any<long>());
    }

    [Fact]
    public async Task CreateAsync_ShouldLogErrorMessage_WhenExceptionIsThrown()
    {
        //Arrang
        var expectedException = new SqliteException("DB exception", 500);
        _userRepository.CreateAsync(Arg.Any<User>()).Throws(expectedException);

        //Act
        var exceptionResult = async () => await _sut.CreateAsync(new User());

        //Assert
        await exceptionResult.Should().ThrowAsync<SqliteException>()
            .WithMessage("DB exception");
        
        _logger.Received(1).LogError(expectedException, Arg.Is("Something went wrong while creating a user"));
    }

    #endregion

    #region DeleteByIdAsync

    [Fact]
    public async Task DeleteByIdAsync_ShouldDeleteTheUser_WhenUserExists()
    {
        //Arrange
        User existingUser = new() { Id = Guid.NewGuid(), FullName = "Brayan Giraldo" };
        _userRepository.DeleteByIdAsync(existingUser.Id).Returns(true);

        //Act
        var result = await _sut.DeleteByIdAsync(existingUser.Id);

        //Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task DeleteByIdAsync_ShouldNotDeleteTheUser_WhenUserDoesNotExist()
    {
        //Arrange
        _userRepository.DeleteByIdAsync(Arg.Any<Guid>()).Returns(false);

        //Act
        var result = await _sut.DeleteByIdAsync(Guid.NewGuid());

        //Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task DeleteByIdAsync_ShouldLogMessages_WhenDeletingUser()
    {
        //Arrange
        _userRepository.DeleteByIdAsync(Arg.Any<Guid>()).Returns(true);

        //Act
        var result = await _sut.DeleteByIdAsync(Guid.NewGuid());

        //Assert
        _logger.Received(1).LogInformation(Arg.Is("Deleting user with id: {0}"), Arg.Any<Guid>());
        _logger.Received(1).LogInformation(Arg.Is("User with id {0} deleted in {1}ms"), Arg.Any<Guid>(), Arg.Any<long>());
    }
    
    [Fact]
    public async Task DeleteByIdAsync_ShouldLogErrorMessage_WhenExceptionIsThrown()
    {
        //Arrange
        var expectedException = new SqliteException("DB Exception", 500);
        _userRepository.DeleteByIdAsync(Arg.Any<Guid>()).Throws(expectedException);

        //Act
        var exceptionResult = async() => await _sut.DeleteByIdAsync(Guid.NewGuid());

        //Assert
        await exceptionResult.Should().ThrowAsync<SqliteException>()
            .WithMessage("DB Exception");
        
        _logger.Received(1).LogError(expectedException, Arg.Is("Something went wrong while deleting user with id {0}"), Arg.Any<Guid>());
    }

    #endregion
}
 