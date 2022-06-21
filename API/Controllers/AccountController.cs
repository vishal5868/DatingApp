using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        private readonly IMapper _mapper;

        public AccountController(Data.DataContext context, Interfaces.ITokenService tokenService, IMapper mapper)
        {
            _mapper = mapper;
            this._context = context;
            this._tokenService = tokenService;
        }
        [HttpPost("register")]
        public async Task<ActionResult<DTOs.UserDto>> Register(DTOs.RegisterDto registerDto)
        {
            if (await UserExists(registerDto.username)) return BadRequest("Username is taken");

            var user = _mapper.Map<AppUser>(registerDto);
            using var hmac = new HMACSHA512();


            user.UserName = registerDto.username.ToLower();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.password));
            user.PasswordSalt = hmac.Key;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new DTOs.UserDto
            {
                username = user.UserName,
                token = _tokenService.CreatToken(user),
                KnownAs= user.KnownAs

            };

        }

        [HttpPost("login")]
        public async Task<ActionResult<DTOs.UserDto>> Login(DTOs.LoginDto loginDto)
        {
            var user = await _context.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == loginDto.username);

            if (user == null) return Unauthorized("Invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }

            return new DTOs.UserDto
            {
                username = user.UserName,
                token = _tokenService.CreatToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs
            };


        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}