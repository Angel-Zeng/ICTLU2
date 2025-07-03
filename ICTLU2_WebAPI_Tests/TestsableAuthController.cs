using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICTLU2_Backend_WebAPI.Controllers;
using ICTLU2_Backend_WebAPI.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ICTLU2_WebAPI_Tests
{
    public class TestableAuthController : AuthController
    {
        private bool _usernameExists;

        public TestableAuthController(ConnectionStrings conString, IConfiguration config)
            : base(conString, config) { }

        public void SetUsernameExists(bool exists) => _usernameExists = exists;

        public new IActionResult Register(IAuthService dto)
        {
            if (!ValidatePassword(dto.Password))
                return BadRequest("Wachtwoord moet minimaal 10 tekens, 1 hoofdletters, 1 cijfers en 1 speciaal teken!");

            if (_usernameExists)
                return BadRequest("Gebruikersnaam is al in gebruik");

            return Ok("Registratie succesvol");
        }
    }

}
