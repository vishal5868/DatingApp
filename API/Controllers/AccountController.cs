using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AccountController(Data.DataContext context, Interfaces.ITokenService tokenService)
        {
            this._context = context;
            this._tokenService = tokenService;
        }
        [HttpPost("register")]
         public async Task<ActionResult<DTOs.UserDto>> Register(DTOs.RegisterDto registerDto)
         {
            if (await UserExists(registerDto.username)) return BadRequest("Username is taken");
            using var hmac = new HMACSHA512();
            
            var user = new AppUser
            {
                UserName = registerDto.username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.password)),
                PasswordSalt = hmac.Key
            };
             _context.Users.Add(user);
             await _context.SaveChangesAsync();

             return new DTOs.UserDto
             {
                 username= user.UserName,
                 token = _tokenService.CreatToken(user)
             };

         }

         [HttpPost("login")]
         public async Task<ActionResult<DTOs.UserDto>>Login(DTOs.LoginDto loginDto)
         {
             var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.username);

             if (user ==  null) return Unauthorized("Invalid username");

             using var hmac = new HMACSHA512(user.PasswordSalt);

             var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.password));

             for(int i =0; i < computedHash.Length; i++)
             {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password"); 
             }

            return new DTOs.UserDto
             {
                 username= user.UserName,
                 token = _tokenService.CreatToken(user)
             };


         }

         private async Task<bool> UserExists(string username)
         {
             return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
         }
    }
}