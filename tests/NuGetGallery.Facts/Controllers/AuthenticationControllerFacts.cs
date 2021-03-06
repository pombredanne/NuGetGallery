﻿using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Moq;
using Xunit;
using System.Net.Mail;
using NuGetGallery.Framework;

namespace NuGetGallery.Controllers
{
    public class AuthenticationControllerFacts
    {
        public class TheLogOffAction : TestContainer
        {
            [Fact]
            public void WillLogTheUserOff()
            {
                var controller = GetController<AuthenticationController>();

                controller.LogOff("theReturnUrl");

                GetMock<IFormsAuthenticationService>().Verify(x => x.SignOut());
            }

            [Fact]
            public void WillRedirectToTheReturnUrl()
            {
                var controller = GetController<AuthenticationController>();
                
                var result = controller.LogOff("theReturnUrl");
                ResultAssert.IsRedirectTo(result, "/");
            }
        }

        public class TheSignInAction : TestContainer
        {
            [Fact]
            public void WillShowTheViewWithErrorsIfTheModelStateIsInvalid()
            {
                var controller = GetController<AuthenticationController>();
                controller.ModelState.AddModelError(String.Empty, "aFakeError");

                var result = controller.SignIn(null, null);

                ResultAssert.IsView(result, viewData: new
                {
                    ReturnUrl = (string)null
                });
            }

            [Fact]
            public void CanLogTheUserOnWithUserName()
            {
                var controller = GetController<AuthenticationController>();
                var user = new User("theUsername") { EmailAddress = "confirmed@example.com" };
                GetMock<IUserService>()
                    .Setup(x => x.FindByUsernameOrEmailAddressAndPassword("theUsername", "thePassword"))
                    .Returns(user);

                controller.SignIn(
                    new SignInRequest { UserNameOrEmail = "theUsername", Password = "thePassword" },
                    "theReturnUrl");

                GetMock<IFormsAuthenticationService>().Verify(
                    x => x.SetAuthCookie(
                        "theUsername",
                        true,
                        null));
            }

            [Fact]
            public void CanLogTheUserOnWithEmailAddress()
            {
                var controller = GetController<AuthenticationController>();
                var user = new User("theUsername") { EmailAddress = "confirmed@example.com" };
                GetMock<IUserService>()
                    .Setup(x => x.FindByUsernameOrEmailAddressAndPassword("confirmed@example.com", "thePassword"))
                    .Returns(user);

                controller.SignIn(
                    new SignInRequest { UserNameOrEmail = "confirmed@example.com", Password = "thePassword" },
                    "theReturnUrl");

                GetMock<IFormsAuthenticationService>().Verify(
                    x => x.SetAuthCookie(
                        "theUsername",
                        true,
                        null));
            }

            [Fact]
            public void WillLogTheUserOnWithUsernameEvenWithoutConfirmedEmailAddress()
            {
                var controller = GetController<AuthenticationController>();
                var user = new User { Key = 42, Username = "theUsername", UnconfirmedEmailAddress = "anAddress@email.org" };
                GetMock<IUserService>()
                    .Setup(x => x.FindByUsernameOrEmailAddressAndPassword("theUsername", "thePassword"))
                    .Returns(user);

                controller.SignIn(
                    new SignInRequest { UserNameOrEmail = "theUsername", Password = "thePassword" },
                    "theReturnUrl");

                GetMock<IFormsAuthenticationService>()
                    .Verify(
                        x => x.SetAuthCookie(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IEnumerable<string>>()));
            }

            [Fact]
            public void WillLogTheUserOnWithRoles()
            {
                var controller = GetController<AuthenticationController>();
                var user = new User("theUsername")
                {
                    Roles = new[] { new Role { Name = "Administrators" } },
                    EmailAddress = "confirmed@example.com"
                };
                GetMock<IUserService>()
                    .Setup(x => x.FindByUsernameOrEmailAddressAndPassword("theUsername", "thePassword"))
                    .Returns(user);

                controller.SignIn(
                    new SignInRequest { UserNameOrEmail = "theUsername", Password = "thePassword" },
                    "theReturnUrl");

                GetMock<IFormsAuthenticationService>().Verify(
                    x => x.SetAuthCookie(
                        "theUsername",
                        true,
                        new[] { "Administrators" }));
            }

            [Fact]
            public void WillInvalidateModelStateAndShowTheViewWithErrorsWhenTheUsernameAndPasswordAreNotValid()
            {
                var controller = GetController<AuthenticationController>();
                GetMock<IUserService>()
                    .Setup(x => x.FindByUsernameOrEmailAddressAndPassword(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsNull();

                var result = controller.SignIn(new SignInRequest(), "theReturnUrl") as ViewResult;

                Assert.NotNull(result);
                Assert.Empty(result.ViewName);
                Assert.False(controller.ModelState.IsValid);
                Assert.Equal(Strings.UsernameAndPasswordNotFound, controller.ModelState[String.Empty].Errors[0].ErrorMessage);
            }
            
            [Fact]
            public void WillRedirectToTheReturnUrl()
            {
                var controller = GetController<AuthenticationController>();
                GetMock<IUserService>()
                    .Setup(x => x.FindByUsernameOrEmailAddressAndPassword(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(new User("theUsername") { EmailAddress = "confirmed@example.com" });

                var result = controller.SignIn(new SignInRequest(), "theReturnUrl");

                ResultAssert.IsRedirectTo(result, "/");
            }
        }

        public class TheRegisterAction : TestContainer
        {
            [Fact]
            public void WillShowTheViewWithErrorsIfTheModelStateIsInvalid()
            {
                var controller = GetController<AuthenticationController>();
                controller.ModelState.AddModelError(String.Empty, "aFakeError");

                var result = controller.Register(null, null);

                ResultAssert.IsView(result, viewData: new
                {
                    ReturnUrl = (string)null
                });
            }

            [Fact]
            public void WillCreateTheUser()
            {
                var controller = GetController<AuthenticationController>();
                var user = new User("theUsername");
                GetMock<IUserService>()
                    .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(user);

                controller.Register(
                    new RegisterRequest
                    {
                        Username = "theUsername",
                        Password = "thePassword",
                        EmailAddress = "theEmailAddress",
                    }, null);

                GetMock<IUserService>()
                    .Verify(x => x.Create("theUsername", "thePassword", "theEmailAddress"));
            }

            [Fact]
            public void WillInvalidateModelStateAndShowTheViewWhenAnEntityExceptionIsThrow()
            {
                var controller = GetController<AuthenticationController>();
                GetMock<IUserService>()
                    .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(new EntityException("aMessage"));

                var request = new RegisterRequest
                {
                    Username = "theUsername",
                    Password = "thePassword",
                    EmailAddress = "theEmailAddress",
                };
                var result = controller.Register(request, null);

                ResultAssert.IsView(result);
                Assert.False(controller.ModelState.IsValid);
                Assert.Equal("aMessage", controller.ModelState[String.Empty].Errors[0].ErrorMessage);
            }

            [Fact]
            public void WillRedirectToTheReturnUrl()
            {
                var controller = GetController<AuthenticationController>();
                var user = new User("theUsername") { UnconfirmedEmailAddress = "unconfirmed@example.com" };
                GetMock<IUserService>()
                    .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(user);

                var result = controller.Register(new RegisterRequest
                    {
                        EmailAddress = "unconfirmed@example.com",
                        Password = "thepassword",
                        Username = "theUsername",
                    }, "/theReturnUrl");

                ResultAssert.IsRedirectTo(result, "/theReturnUrl");
            }
        }
    }
}

