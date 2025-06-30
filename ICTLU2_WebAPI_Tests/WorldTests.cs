using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICTLU2_Backend_WebAPI.DTO;
using Microsoft.AspNetCore.Mvc;

namespace ICTLU2_WebAPI_Tests
{
    internal class WorldTests
    {
        [TestMethod]
        public void CreateWorld_ReturnsCreated_WhenInputIsValid()
        {
            // Arrange
            var controller = new TestableWorldsController(
                new ConnectionStrings { Sql = "Fake" }, userId: 1);

            var dto = new WorldCreateDto("TestWorld", 50, 50);

            // Act
            var result = controller.CreateWorld(dto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(CreatedAtActionResult));
        }

        [TestMethod]
        public void CreateWorld_ReturnsBadRequest_WhenMaxWorldsReached()
        {
            // Arrange
            var controller = new TestableWorldsController(
                new ConnectionStrings { Sql = "Fake" }, userId: 1);
            controller.SetMaxWorlds(true); // Simulate user has 5 worlds

            var dto = new WorldCreateDto("AnotherWorld", 30, 30);

            // Act
            var result = controller.CreateWorld(dto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badReq = result as BadRequestObjectResult;
            Assert.AreEqual("Maximaal 5 werelden per gebruiker", badReq?.Value);
        }

        [TestMethod]
        public void CreateWorld_ReturnsBadRequest_WhenWorldNameExists()
        {
            // Arrange
            var controller = new TestableWorldsController(
                new ConnectionStrings { Sql = "Fake" }, userId: 1);
            controller.SetNameExists(true); // Simulate duplicate name

            var dto = new WorldCreateDto("ExistingWorld", 40, 40);

            // Act
            var result = controller.CreateWorld(dto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badReq = result as BadRequestObjectResult;
            Assert.AreEqual("Wereldnaam bestaat al", badReq?.Value);
        }


    }
}
