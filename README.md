Introduction to Swagger and How to Use It in Your .NET Project
Swagger is a powerful tool for documenting and testing RESTful APIs. It simplifies the process of describing and interacting with your API endpoints. In this article, we will explore how to integrate Swagger into your .NET project and use it effectively.

Prerequisites
Before we dive into Swagger, make sure you have the following prerequisites in place:

.NET SDK
Visual Studio (or your preferred code editor)
Adding Swagger to Your .NET Project
Install Swagger Packages: To get started, you need to install the necessary NuGet packages. You can do this by running the following command in your terminal:  
dotnet add package Swashbuckle.AspNetCore




Configure Swagger: In your project's Startup.cs (or Program.cs for .NET 6 minimal APIs), configure Swagger by adding the following code   
public void ConfigureServices(IServiceCollection services)
{
    // ...

    // Add SwaggerGen and configure it
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "swaggerAnotation",
            Version = "v1",
            Description = "Hello World"
        });
        c.EnableAnnotations();
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...

    // Configure Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "swaggerAnotation v1");
        c.RoutePrefix = "swagger";
    });
} 


Annotate Your API: Now, let's add Swagger annotations to your API methods to provide detailed documentation:   
[HttpGet(Name = "GetHuman")]
[SwaggerOperation(
    Summary = "GetHumanSummery",   
    Description = "GetHumanDescrption",
    OperationId = "d5ea45d8-8e78-4d4c-b4b1-f7ee72679ce9",
    Tags = new[] { "WeatherForecastController" })]
public IActionResult Get()
{
    // Your API logic here
}
Viewing Swagger Documentation
Build and Run Your Application: Build and run your .NET application.

Access Swagger UI: Open your web browser and navigate to https://localhost:5001/swagger to access the Swagger UI.

Explore and Test: Swagger UI allows you to explore and test your API endpoints interactively. It provides detailed information about your API, making it easier for developers to understand and use.
Conclusion
Congratulations! You have successfully integrated Swagger into your .NET project and documented your API using annotations. This will greatly improve the developer experience and help others understand and use your API effectively.

For more advanced Swagger configurations and options, refer to the official Swagger documentation
معرفی Swagger و نحوه استفاده از آن در پروژه‌های .NET

Swagger ابزاری قدرتمند برای مستندسازی و تست API‌های RESTful می‌باشد که فرآیند توضیح دادن و تعامل با اندپوینت‌های API را ساده‌تر می‌کند. در این مقاله، ما به بررسی نحوه یکپارچه‌سازی Swagger در پروژه‌های .NET و استفاده مؤثر از آن می‌پردازیم.

  یش‌نیازها
قبل از ورود به موضوع Swagger، مطمئن شوید که شرایط پیش‌نیاز زیر فراهم باشد:

.NET SDK
ویژوال استودیو (یا ویرایشگر کد مورد نظر خود)
dotnet add package Swashbuckle.AspNetCore
پیکربندی Swagger: در فایل Startup.cs پروژه‌تان (یا Program.cs برای پروژه‌های .NET 6 با ساختار Minimal API)، Swagger را با افزودن کد زیر پیکربندی کنید:     using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

// ...

public void ConfigureServices(IServiceCollection services)
{
    // ...

    // افزودن SwaggerGen و پیکربندی آن
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "swaggerAnotation",
            Version = "v1",
            Description = "Hello World"
        });
        c.EnableAnnotations();
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...

    // پیکربندی Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "swaggerAnotation v1");
        c.RoutePrefix = "swagger";
    });
}
توصیف API با Annotations: حالا وقت آن است که به متدهای API‌تان Annotations Swagger اضافه کنید تا مستندات دقیقی ارائه دهید:
[HttpGet(Name = "GetHuman")]
[SwaggerOperation(
    Summary = "GetHumanSummery",   
    Description = "GetHumanDescrption",
    OperationId = "d5ea45d8-8e78-4d4c-b4b1-f7ee72679ce9",
    Tags = new[] { "WeatherForecastController" })]
public IActionResult Get()
{
    // منطق API‌تان را در اینجا قرار دهید
}
مشاهده مستندات Swagger

ایجاد و اجرا پروژه: پروژه .NET خود را ایجاد و اجرا کنید.

دسترسی به Swagger UI: مرورگر وب خود را باز کرده و به آدرس https://localhost:5001/swagger بروید تا به Swagger UI دسترسی داشته باشید.

بررسی و تست: Swagger UI به شما امکان مشاهده و تست تعاملی با اندپوینت‌های API را می‌دهد. اطلاعات دقیقی در مورد API شما ارائه می‌دهد که به توسعه‌دهندگان کمک می‌کند تا API را به آسانی درک و استفاده کنند.

