using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyApp.Controllers;
using MyApp.Models;

namespace MyApp.Tests;

public class AccountControllerTests
{
    private readonly Mock<SignInManager<IdentityUser>> _signInManager;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        var userStore = new Mock<IUserStore<IdentityUser>>();
        var userManager = new Mock<UserManager<IdentityUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManager = new Mock<SignInManager<IdentityUser>>(
            userManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<IdentityUser>>().Object,
            null!, null!, null!, null!);

        _controller = new AccountController(_signInManager.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void Login_Get_WhenNotSignedIn_ReturnsView()
    {
        _signInManager
            .Setup(m => m.IsSignedIn(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(false);

        var result = _controller.Login();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Login_Get_WhenAlreadySignedIn_RedirectsToHome()
    {
        _signInManager
            .Setup(m => m.IsSignedIn(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(true);

        var result = _controller.Login();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Login_Post_InvalidModel_ReturnsViewWithModel()
    {
        _controller.ModelState.AddModelError("Email", "Required");

        var result = await _controller.Login(new LoginViewModel());

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<LoginViewModel>(view.Model);
    }

    [Fact]
    public async Task Login_Post_ValidCredentials_RedirectsToHome()
    {
        var model = new LoginViewModel { Email = "admin@myapp.com", Password = "Admin@123456" };

        _signInManager
            .Setup(m => m.PasswordSignInAsync(model.Email, model.Password, false, true))
            .ReturnsAsync(SignInResult.Success);

        var result = await _controller.Login(model);

        Assert.IsType<LocalRedirectResult>(result);
    }

    [Fact]
    public async Task Login_Post_InvalidCredentials_ReturnsViewWithError()
    {
        var model = new LoginViewModel { Email = "bad@test.com", Password = "wrong" };

        _signInManager
            .Setup(m => m.PasswordSignInAsync(model.Email, model.Password, false, true))
            .ReturnsAsync(SignInResult.Failed);

        var result = await _controller.Login(model);

        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_Post_LockedOut_ReturnsViewWithLockoutMessage()
    {
        var model = new LoginViewModel { Email = "locked@test.com", Password = "pass" };

        _signInManager
            .Setup(m => m.PasswordSignInAsync(model.Email, model.Password, false, true))
            .ReturnsAsync(SignInResult.LockedOut);

        var result = await _controller.Login(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(_controller.ModelState.ErrorCount > 0);
    }
}
