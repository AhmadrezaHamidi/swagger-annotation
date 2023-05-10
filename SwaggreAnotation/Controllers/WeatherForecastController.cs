using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SwaggreAnotation.Dtos;
using SwaggreAnotation.Entittes;
using Swashbuckle.AspNetCore.Annotations;

namespace SwaggreAnotation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IMapper _mapper;
        public WeatherForecastController(ILogger<WeatherForecastController> logger, IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
        }

        //[HttpGet(Name = "GetWeatherForecast")]
        //[SwaggerOperation(
        //Summary = "GetWeatherForecast",
        //Description = "GetWeatherForecast",
        //OperationId = "d4ea45d8-8e78-4d4c-b4b1-f7ee72679ce9",
        //    Tags = new[] { "WeatherForecastController" })]
        //public IEnumerable<WeatherForecast> Get()
        //{
        //    return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //    {
        //        Date = DateTime.Now.AddDays(index),
        //        TemperatureC = Random.Shared.Next(-20, 55),
        //        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        //    })
        //    .ToArray();
        //}


        [HttpGet(Name = "GetHuman")]
        [SwaggerOperation(
Summary = "GetHuman",
Description = "GetHuman",
OperationId = "d5ea45d8-8e78-4d4c-b4b1-f7ee72679ce9",
    Tags = new[] { "WeatherForecastController" })]
        public HUmanDto GetHuman()
        {
            var instanceHuman = new Human("vestaabner", "mazadak nzaemi is a good man ");
            var res = _mapper.Map<HUmanDto>(instanceHuman);
            return res;
        }
    }
}