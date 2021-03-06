﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCoreIdentity.Models;
using AspNetCoreIdentity.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AspNetCoreIdentity
{

    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public AccountController(UserManager<AppUser> userManager)
        {
            this._userManager = userManager;
        }

        [HttpPost]
        public async Task<ResultVM> Login([FromBody]LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                    identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                    return new ResultVM
                    {
                        Status = Status.Success,
                        Message = "Succesfull login",
                        Data = model
                    };
                }

                return new ResultVM
                {
                    Status = Status.Error,
                    Message = "Invalid data",
                    Data = "<li>Invalid Username or Password</li>"
                };
            }

            var errors = ModelState.Keys.Select(e => "<li>" + e + "</li>");
            return new ResultVM
            {
                Status = Status.Error,
                Message = "Invalid data",
                Data = string.Join("", errors)
            };
        }

        [HttpGet]
        [Authorize]
        public UserClaims Claims()
        {
            var claims = User.Claims.Select(c => new ClaimVM
            {
                Type = c.Type,
                Value = c.Value
            });

            return new UserClaims
            {
                UserName = User.Identity.Name,
                Claims = claims
            };
        }

        [HttpGet]
        public UserStateVM Authenticated()
        {
            return new UserStateVM
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                Username = User.Identity.IsAuthenticated ? User.Identity.Name : string.Empty
            };
        }

        [HttpPost]
        public async Task SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpPost]
        public async Task<ResultVM> Register([FromBody]RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                IdentityResult result = null;
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user != null)
                {
                    return new ResultVM
                    {
                        Status = Status.Error,
                        Message = "Invalid data",
                        Data = "<li>User already exists</li>"
                    };
                }

                user = new AppUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = model.UserName,
                    Email = model.Email
                };

                result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return new ResultVM
                    {
                        Status = Status.Success,
                        Message = "User Created",
                        Data = user
                    };
                }
                else
                {
                    var resultErrors = result.Errors.Select(e => "<li>" + e.Description + "</li>");
                    return new ResultVM
                    {
                        Status = Status.Error,
                        Message = "Invalid data",
                        Data = string.Join("", resultErrors)
                    };
                }
            }

            var errors = ModelState.Keys.Select(e => "<li>" + e + "</li>");
            return new ResultVM
            {
                Status = Status.Error,
                Message = "Invalid data",
                Data = string.Join("", errors)
            };
        }
    }
}
