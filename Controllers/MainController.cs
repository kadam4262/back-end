using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

namespace back_end.Controllers;

[ApiController]
[Route("api/")]
public class LoginController : ControllerBase
{
    private readonly ILogger<LoginController> _logger;

    public LoginController(ILogger<LoginController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] User user)
    {
        if (user.Password is null && user.Email is null) 
        {
            var response = new 
            {
                error = "Add email or password"
            };
            
            return NotFound(JsonSerializer.Serialize(response));
        }
        else
        {
            (string,string)? signInUser = await new DBController().Login(user.Email!,user.Password!);
            if (signInUser is null)
            {
                var response = new 
                {
                    error = "Wrong email or password!"
                };
                
                return NotFound(JsonSerializer.Serialize(response));
            }
            else
            {
                await HttpContext.SignInAsync(new ClaimsPrincipal(new[]
                {
                    new ClaimsIdentity(new List<Claim>()
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, signInUser.Value.Item2)
                    },
                    CookieAuthenticationDefaults.AuthenticationScheme)
                }));
                
                var response = new 
                {
                    name = signInUser.Value.Item1,
                    role = signInUser.Value.Item2
                };
                return Ok(JsonSerializer.Serialize(response));
            }
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost("registration")]
    public async Task<ActionResult> Registration([FromBody] Registration user)
    {
        string resultValue = "Unsuccesful registration";
        
        bool result = await new DBController().Registration(user.Name!,user.Email!,user.Password!,user.Role!);
        if (result)
        {
            resultValue = "Registration completed.";
            var obj = new
            {
                Value = resultValue
            };

            return Ok(JsonSerializer.Serialize(resultValue));
        }
        else 
        {
            var obj = new
            {
                Value = resultValue
            };
            
            return BadRequest(JsonSerializer.Serialize(obj));
        }
    }

    [HttpDelete("delete")]
    public async Task<ActionResult> Delete([FromBody] User user)
    {
        var result = await new DBController().Delete(user.Email!,user.Password!) switch {
            DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully deleted"})),
            DBController.Result.NoRecordAffected => BadRequest(JsonSerializer.Serialize(new {Message =  "Wrong email or password"})),
            DBController.Result.DbException => StatusCode( 500,JsonSerializer.Serialize(new {Message =  "Internal error"})),
            _ => null
        };
        
        if (result is null)
        {
            return StatusCode(500, JsonSerializer.Serialize(new { Message = "Internal Error." }));
        }
        else 
        {
            return result;
        }
    }

    [AllowAnonymous]
    [HttpGet("logout")]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(JsonSerializer.Serialize(new {Message = "Succesfuly loged out"}));
    }

    [Authorize(Roles = "raktarvezeto")]
    [HttpPost("set-price")]
    public async Task<ActionResult> SetPrice([FromBody] Price price)
    {
        if (price.ID == 0 || price.Value == 0)
        {
            Console.Error.WriteLine("result");
            return BadRequest(JsonSerializer.Serialize(new {Message = "Missing data"}));
        }
        else 
        {
            var result = await new DBController().ChangePrice(price.ID,price.Value) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully changed the price"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Bad request"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Bad request"}))
            };
            return result;
        }
    }

    [Authorize(Roles = "raktarvezeto")]
    [HttpPost("add-component")]
    public async Task<ActionResult> AddComponent([FromBody] Component component)
    {
        if (component.Name is null || component.Price == 0 || component.MaxQuantity == 0)
        {
            return BadRequest(JsonSerializer.Serialize(new {Message = "Missing data"}));
        }
        else 
        {
            var result = await new DBController().AddComponent(component.Name, component.Price!, component.MaxQuantity!) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully added new component"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Bad request"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Bad request"}))
            };

            return result;
        }
    }

    [Authorize(Roles = "raktarvezeto")]
    [HttpGet("list-components")]
    public async Task<ActionResult> ListComponent()
    {
      List<Component> component  = await new DBController().ListComponents();
      if (component.Count == 0)
      {
        return BadRequest(JsonSerializer.Serialize(new {Message = "There is no component"}));
      }
      else
      {
        return Ok(JsonSerializer.Serialize(component));
      }
    }

    [Authorize(Roles = "raktarvezeto")]
    [HttpGet("list-stack")]
    public async Task<ActionResult> ListStack()
    {
        List<StackItem> stackItems =  await new DBController().ListStack();

        if (stackItems.Count == 0)
        {
            return BadRequest(JsonSerializer.Serialize(new {Message = "There is no stack item"}));
        }
        else 
        {
            return Ok(JsonSerializer.Serialize(stackItems));
        }

    }

    [HttpPost("update-component")]
    [Authorize(Roles = "raktarvezeto")]
    public async Task<ActionResult> UpdateComponent([FromBody] Component comp)
    {
        var result = await new DBController().UpdateComponent(comp.ID, comp.quantity) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully updated new component"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Bad request"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Bad request"}))
            };
        return result;
    }
}