using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICTLU2_Backend_WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Models;
using System.Security.Claims;

namespace ICTLU2_WebAPI_Tests
{
    public class TestableWorldsController : WorldsController
    {
        private readonly int _userId;
        private bool _hasMaxWorlds = false;
        private bool _nameExists = false;

        public TestableWorldsController(ConnectionStrings conString, int userId)
            : base(conString)
        {
            _userId = userId;
        }

        public void SetMaxWorlds(bool val) => _hasMaxWorlds = val;
        public void SetNameExists(bool val) => _nameExists = val;

        public int UserId => _userId;

        public new IActionResult CreateWorld(WorldCreateDto dto)
        {
            if (dto.Name.Length is < 1 or > 25)
                return BadRequest("Naam moet tussen 1-25 tekens zijn");

            if (dto.Width is < 20 or > 200)
                return BadRequest("Breedte moet tussen 20-200 zijn");

            if (dto.Height is < 10 or > 100)
                return BadRequest("Hoogte moet tussen 10-100 zijn");

            if (_hasMaxWorlds)
                return BadRequest("Maximaal 5 werelden per gebruiker");

            if (_nameExists)
                return BadRequest("Wereldnaam bestaat al");

            return CreatedAtAction(nameof(GetWorld), new { id = 1 }, new { id = 1 });
        }

        public new IActionResult GetWorld(int id) => Ok(); // Stub
    }

}
