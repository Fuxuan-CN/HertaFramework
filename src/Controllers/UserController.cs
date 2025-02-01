using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Herta.Responses.Response;
using Herta.Models.DataModels;
using Herta.Exceptions.HttpException;

namespace Herta.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        [HttpGet("{name}")]
        public Response GetUser(string name)
        {
            return new Response(new { name = name });
        }

        [HttpPost]
        public Response LoginUser([FromBody] LoginUserForm form)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                                   .SelectMany(v => v.Errors)
                                   .Select(e => e.ErrorMessage)
                                   .ToList();

                string errorDetails = string.Join(", ", errors);
                throw new HttpException(StatusCodes.Status400BadRequest, "Invalid input", errorDetails);
            }
            else
            {
                return new Response(new { message = "Login successful" });
            }
        }
    }
}
