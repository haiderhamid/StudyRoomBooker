using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class AuthControllerTests
{
    private readonly Mock<UserManager<AuthUser>> _userManagerMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock = new();

    public AuthControllerTests()
    {
        // UserManager has no interface, so it is mocked with an empty user store.
        _userManagerMock = new Mock<UserManager<AuthUser>>(
            Mock.Of<IUserStore<AuthUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<AuthUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<AuthUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
    }

    // ValidationProblem() needs a ProblemDetailsFactory, which normally comes from the app's services.
    private class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null,
            string? type = null, string? detail = null, string? instance = null)
        {
            return new ProblemDetails { Status = statusCode ?? 500, Title = title };
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelStateDictionary,
            int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
        {
            return new ValidationProblemDetails(modelStateDictionary) { Status = statusCode ?? 400 };
        }
    }

    private AuthController CreateController()
    {
        return new AuthController(_userManagerMock.Object, new ConfigurationBuilder().Build(), _loggerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    [Fact]
    public async Task Register_NewUsernameAndEmail_ReturnsOk()
    {
        var controller = CreateController();

        var result = await controller.Register(new RegisterDto { Username = "kari", Email = "kari@oslomet.no", Password = "Kari123!" });

        Assert.IsType<OkObjectResult>(result);
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<AuthUser>(), "Kari123!"), Times.Once);
    }

    [Fact]
    public async Task Register_UsernameIsAnotherUsersEmail_ReturnsBadRequest()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("kari@oslomet.no")).ReturnsAsync(new AuthUser { UserName = "kari", Email = "kari@oslomet.no" });
        var controller = CreateController();

        var result = await controller.Register(new RegisterDto { Username = "kari@oslomet.no", Email = "ola@oslomet.no", Password = "Ola123!" });

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
        Assert.True(controller.ModelState.ContainsKey("Username"));
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<AuthUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_EmailIsAnotherUsersUsername_ReturnsBadRequest()
    {
        _userManagerMock.Setup(m => m.FindByNameAsync("ola@oslomet.no")).ReturnsAsync(new AuthUser { UserName = "ola@oslomet.no", Email = "ola@example.com" });
        var controller = CreateController();

        var result = await controller.Register(new RegisterDto { Username = "ola2", Email = "ola@oslomet.no", Password = "Ola123!" });

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
        Assert.True(controller.ModelState.ContainsKey("Email"));
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<AuthUser>(), It.IsAny<string>()), Times.Never);
    }
}
