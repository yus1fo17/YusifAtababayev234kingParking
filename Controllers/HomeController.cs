using Microsoft.AspNetCore.Mvc;

namespace ParkingApi.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
