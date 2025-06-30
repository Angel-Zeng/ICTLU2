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
        //kijkt of de wereld input die aangemaakt wordt wel geldige regels heeft
        public void CreateWorld_ReturnsCreated_WhenInputIsValid()
        {
            var controller = new TestableWorldsController(
                new ConnectionStrings { Sql = "Fake" }, userId: 1);

            var dto = new WorldCreateDto("TestWorld", 50, 50);

            var result = controller.CreateWorld(dto);

            Assert.IsInstanceOfType(result, typeof(CreatedAtActionResult));
        }

        //kijkt of de user al 5 werelden heeft
        [TestMethod]
        public void CreateWorld_ReturnsBadRequest_WhenMaxWorldsReached()
        {
            var controller = new TestableWorldsController(
                new ConnectionStrings { Sql = "Fake" }, userId: 1);
            controller.SetMaxWorlds(true); 

            var dto = new WorldCreateDto("AnotherWorld", 30, 30);

            var result = controller.CreateWorld(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badReq = result as BadRequestObjectResult;
            Assert.AreEqual("Maximaal 5 werelden per gebruiker", badReq?.Value);
        }

        [TestMethod]
        //kijkt of de wereldnaam die ze kiezen al bestaat of niet
        public void CreateWorld_ReturnsBadRequest_WhenWorldNameExists()
        {
            var controller = new TestableWorldsController(
                new ConnectionStrings { Sql = "Fake" }, userId: 1);
            controller.SetNameExists(true); 

            var dto = new WorldCreateDto("ExistingWorld", 40, 40);

            var result = controller.CreateWorld(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badReq = result as BadRequestObjectResult;
            Assert.AreEqual("Wereldnaam bestaat al", badReq?.Value);
        }


    }
}
