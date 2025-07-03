using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using ICTLU2_Backend_WebAPI.Controllers;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Models;
using ICTLU2_Backend_WebAPI.Services;

namespace ICTLU2_WebAPI_Tests
{
    [TestClass]
    public class WorldsControllerTests
    {
        // Mock van de IWorldService 
        private static WorldsController BuildController(
            IWorldService serviceMock,
            int userId = 1)
        {
            var con = new WorldsController(serviceMock);

            var http = new DefaultHttpContext();
            var idClaim = new Claim(ClaimTypes.NameIdentifier, userId.ToString());
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { idClaim }));
            con.ControllerContext = new ControllerContext { HttpContext = http };
            return con;
        }

        //Kijken of de wereld van een user of die goed wordt opgehaald. 
        [TestMethod]
        public async Task CreateWorld_ReturnsCreated_WhenInputValid()
        {
            var worldSvc = new Mock<IWorldService>();
            worldSvc
                .Setup(s => s.CreateWorldAsync(1, It.IsAny<WorldCreateDto>()))
                .ReturnsAsync(1);

            var controller = BuildController(worldSvc.Object);

            var dto = new WorldCreateDto("TestWorld", 50, 50);
            var result = await controller.CreateWorld(dto);

            Assert.IsInstanceOfType(result, typeof(CreatedAtActionResult));
        }

        // Kijken of de gebruiker al 5 werelden heeft of niet
        [TestMethod]
        public async Task CreateWorld_ReturnsBadRequest_WhenMaxWorldsReached()
        {
            var worldSvc = new Mock<IWorldService>();
            worldSvc
                .Setup(s => s.CreateWorldAsync(1, It.IsAny<WorldCreateDto>()))
                .ThrowsAsync(new InvalidOperationException("Maximaal 5 werelden per gebruiker"));

            var controller = BuildController(worldSvc.Object);

            var dto = new WorldCreateDto("Another", 30, 30);
            var result = await controller.CreateWorld(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.AreEqual("Maximaal 5 werelden per gebruiker",
                            ((BadRequestObjectResult)result).Value);
        }

        // Kijken of de gebruiker al een wereld heeft met eenzelfde wereldnaam of niet. 
        [TestMethod]
        public async Task CreateWorld_ReturnsBadRequest_WhenNameExists()
        {
            var worldSvc = new Mock<IWorldService>();
            worldSvc
                .Setup(s => s.CreateWorldAsync(1, It.IsAny<WorldCreateDto>()))
                .ThrowsAsync(new ArgumentException("Wereldnaam bestaat al"));

            var controller = BuildController(worldSvc.Object);

            var dto = new WorldCreateDto("ExistingWorld", 40, 40);
            var result = await controller.CreateWorld(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.AreEqual("Wereldnaam bestaat al",
                            ((BadRequestObjectResult)result).Value);
        }
    }
}
