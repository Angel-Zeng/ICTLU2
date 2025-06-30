using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using ICTLU2_Backend_WebAPI.Controllers;
using ICTLU2_Backend_WebAPI.DTO;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ICTLU2_WebAPI_Tests
{
    [TestClass]
    public class AuthControllerTests
    {   
        private Mock<IConfiguration> _configurationMock;
        private Mock<ConnectionStrings> _connectionStringsMock;

        [TestMethod]
        public void Register_ReturnsBadRequest_WhenPasswordIsInvalid()
        {
           
            var connectionStrings = new ConnectionStrings { Sql = "FakeConnectionString" };
            var inMemorySettings = new Dictionary<string, string> { { "Jwt:Key", "testkey1234567890" } };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var controller = new AuthController(connectionStrings, configuration);

            var dto = new LoginDto
           ("testuser", "short");

            // Act
            var result = controller.Register(dto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequest = result as BadRequestObjectResult;
            Assert.AreEqual("Wachtwoord moet minimaal 10 tekens, 1 hoofdletters, 1 cijfers en 1 speciaal teken!", badRequest?.Value);
        }

        [TestMethod]
        public void Register_ReturnsOk_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var connectionStrings = new ConnectionStrings { Sql = "FakeConnectionString" };
            var inMemorySettings = new Dictionary<string, string> { { "Jwt:Key", "testkey1234567890" } };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var dto = new LoginDto("newuser", "ValidPass1!");

            // Use a mock DB connection - this example assumes you’ll adapt it to use in-memory DB or mocking SqlCommand in real setup
            var controller = new TestableAuthController(connectionStrings, configuration);
            controller.SetUsernameExists(false); // Simulate username does not exist

            // Act
            var result = controller.Register(dto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual("Registratie succesvol", okResult?.Value);
        }

        [TestMethod]
        public void Register_ReturnsBadRequest_WhenUsernameAlreadyExists()
        {
            // Arrange
            var connectionStrings = new ConnectionStrings { Sql = "FakeConnectionString" };
            var inMemorySettings = new Dictionary<string, string> { { "Jwt:Key", "testkey1234567890" } };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var dto = new LoginDto("existinguser", "ValidPass1!");

            var controller = new TestableAuthController(connectionStrings, configuration);
            controller.SetUsernameExists(true); // Simulate username already in use

            // Act
            var result = controller.Register(dto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequest = result as BadRequestObjectResult;
            Assert.AreEqual("Gebruikersnaam is al in gebruik", badRequest?.Value);
        }


    }
}