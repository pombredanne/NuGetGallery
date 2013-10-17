﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Moq;
using Xunit;

namespace NuGetGallery.Controllers
{
    public class AppControllerFacts
    {
        public class TheGetCurrentUserMethod
        {
            [Fact]
            public void GivenNoActiveUserPrincipal_ItReturnsNull()
            {
                // Arrange
                var context = new Mock<IOwinContext>();
                var ctrl = new TestableAppController();
                ctrl.OwinContext = context.Object;

                // Act
                var user = ctrl.InvokeGetCurrentUser();

                // Assert
                Assert.Null(user);
            }
        }

        public class TestableAppController : AppController
        {
            // Nothing but a concrete class to test an abstract class :)

            public User InvokeGetCurrentUser()
            {
                return GetCurrentUser();
            }
        }
    }
}