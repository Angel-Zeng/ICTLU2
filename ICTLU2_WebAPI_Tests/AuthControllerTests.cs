using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ICTLU2_Backend_WebAPI.Controllers;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Services;

//Ik kan eindelijk die fuckass fake controllers weghalen en normale tests maken lmaooo
namespace ICTLU2_WebAPI_Tests
{
    [TestClass]
    public class AuthControllerTests
    {
        // Fake config met Jwt key
        private static IConfiguration FakeConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "0123456789ABCDEF0123456789ABCDEF" }
                })
                .Build();

        // Testen of het wachtwoord goed genoeg is met zwak wachtwoord. 
        [TestMethod]
        public async Task Register_ReturnsBadRequest_WhenPasswordIsWeak()
        {
            var authMock = new Mock<IAuthService>();
            authMock  // service gooit exceptie bij zwak wachtwoord
                .Setup(s => s.RegisterUserAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new ArgumentException(
                    "Wachtwoord moet minimaal 10 tekens, 1 hoofdletters, 1 cijfers en 1 speciaal teken!"));

            var controller = new AuthController(authMock.Object, FakeConfig());

            var dto = new LoginDto("user", "weak");
            var result = await controller.Register(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.AreEqual(
                "Wachtwoord moet minimaal 10 tekens, 1 hoofdletters, 1 cijfers en 1 speciaal teken!",
                ((BadRequestObjectResult)result).Value);
        }

        // Kijken of de registratie goed gaat met, deze keer, een sterk wachtwoord.
        [TestMethod]
        public async Task Register_ReturnsOk_WhenSuccessful()
        {
            var authMock = new Mock<IAuthService>();
            authMock
                .Setup(s => s.RegisterUserAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync("Registratie succesvol");

            var controller = new AuthController(authMock.Object, FakeConfig());

            var dto = new LoginDto("newuser", "ValidPass1!");
            var result = await controller.Register(dto);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            Assert.AreEqual("Registratie succesvol",
                            ((OkObjectResult)result).Value);
        }

        //Kijken of de registratie kan met een gebruikekersnaam die al bestaat. 
        [TestMethod]
        public async Task Register_ReturnsBadRequest_WhenUsernameExists()
        {
            var authMock = new Mock<IAuthService>();
            authMock
                .Setup(s => s.RegisterUserAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new ArgumentException("Gebruikersnaam is al in gebruik"));

            var controller = new AuthController(authMock.Object, FakeConfig());

            var dto = new LoginDto("existing", "ValidPass1!");
            var result = await controller.Register(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.AreEqual("Gebruikersnaam is al in gebruik",
                            ((BadRequestObjectResult)result).Value);
        }
    }
}
