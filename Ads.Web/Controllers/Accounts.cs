﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ads.Web.Models.Accounts;
using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ads.Web.Controllers
{
    public class Accounts : Controller
    {
        private readonly SignInManager<CognitoUser> _signInManager;
        private readonly UserManager<CognitoUser> _userManager;
        private readonly CognitoUserPool _pool;
        //private readonly CognitoUserManager<CognitoUser> _cognitoUserManager;

        public Accounts(
            SignInManager<CognitoUser> signInManager,
            UserManager<CognitoUser> userManager,
            CognitoUserPool pool            
        )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _pool = pool;
            //_cognitoUserManager = userManager as CognitoUserManager<CognitoUser>;
        }

        public async Task<IActionResult> Signup()
        {
            var model = new SignupModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignupModel model)
        {
            if(ModelState.IsValid)
            {
                var user = _pool.GetUser(model.Email);
                if(user.Status != null)
                {
                    ModelState.AddModelError("UserExists", "User with this email already exists");
                    return View(model);
                }
                //- add attributes defined in cognito user pool settings
                user.Attributes.Add(CognitoAttribute.Email.ToString(), model.Email);
                user.Attributes.Add(CognitoAttribute.GivenName.ToString(), model.GivenName);
                user.Attributes.Add(CognitoAttribute.FamilyName.ToString(), model.FamilyName);
                user.Attributes.Add(CognitoAttribute.PhoneNumber.ToString(), model.Phone);
                //- create user
                var createdUser = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
                //- confirm user
                if(createdUser.Succeeded)
                {
                    RedirectToAction("Confirm");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Confirm(ConfirmModel model)
        {
            return View(model);
        }

        [HttpPost]
        [ActionName("Confirm")]
        public async Task<IActionResult> ConfirmPost(ConfirmModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
                if(user == null)
                {
                    ModelState.AddModelError("NotFound", "A user with the given email address was not found");
                    return View(model);
                }
                //var result = await _userManager.ConfirmEmailAsync(user, model.Code).ConfigureAwait(false);
                //var result = await _cognitoUserManager.ConfirmSignUpAsync(user, model.Code, true);
                var result = await (_userManager as CognitoUserManager<CognitoUser>).ConfirmSignUpAsync(user, model.Code, true);
                if(result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach(var item in result.Errors)
                    {
                        ModelState.AddModelError(item.Code, item.Description);
                    }
                    return View(model);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Login(LoginModel model)
        {
            return View(model);
        }

        [HttpPost]
        [ActionName("Login")]
        public async Task<IActionResult> LoginPost(LoginModel model)
        {
            if(ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    false
                    );
                if(result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("LoginError", "Email and password do not match");
                }
            }
            return View("Login", model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}