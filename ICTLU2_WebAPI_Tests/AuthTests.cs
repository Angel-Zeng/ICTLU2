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
        //Deze kijkt of er in registreren een foutmelding geeft als het wachtwoord niet goed is
        public void Register_ReturnsBadRequest_WhenPasswordIsInvalid()
        {
            var connectionStrings = new ConnectionStrings { Sql = "FakeConnectionString" };
            var inMemorySettings = new Dictionary<string, string> { { "Jwt:Key", "testkey1234567890" } };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var controller = new AuthController(connectionStrings, configuration);

            var dto = new IAuthService
           ("testuser", "shortpass");

            var result = controller.Register(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequest = result as BadRequestObjectResult;
            Assert.AreEqual("Wachtwoord moet minimaal 10 tekens, 1 hoofdletters, 1 cijfers en 1 speciaal teken!", badRequest?.Value);
        }

        [TestMethod]

        //Deze kijkt wanneer de registratie succesvol is 
        public void Register_ReturnsOk_WhenRegistrationIsSuccessful()
        {
            var connectionStrings = new ConnectionStrings { Sql = "FakeConnectionString" };
            var inMemorySettings = new Dictionary<string, string> { { "Jwt:Key", "testkey1234567890" } };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var dto = new IAuthService("newuser", "ValidPass1!");

            var controller = new TestableAuthController(connectionStrings, configuration);
            controller.SetUsernameExists(false);

            var result = controller.Register(dto);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual("Registratie succesvol", okResult?.Value);
        }

        [TestMethod]
        //Deze controleert of de gebruikersnaam al bestaat of niet
        public void Register_ReturnsBadRequest_WhenUsernameAlreadyExists()
        {
            var connectionStrings = new ConnectionStrings { Sql = "FakeConnectionString" };
            var inMemorySettings = new Dictionary<string, string> { { "Jwt:Key", "testkey1234567890" } };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var dto = new IAuthService("existinguser", "ValidPass1!");

            var controller = new TestableAuthController(connectionStrings, configuration);
            controller.SetUsernameExists(true);

            var result = controller.Register(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequest = result as BadRequestObjectResult;
            Assert.AreEqual("Gebruikersnaam is al in gebruik", badRequest?.Value);
        }


    }
}