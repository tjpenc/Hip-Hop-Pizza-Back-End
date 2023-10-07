using HipHopPizzaBackend.Models;
using HipHopPizzaBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using System.Numerics;
using System.Xml.Linq;

var DefaultCors = "_DefaultCors";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: DefaultCors,

        policy =>
        {
            policy.WithOrigins("http://localhost:3000",
                                "http://localhost:7283")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowAnyOrigin();
        });
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// allows passing datetimes without time zone data 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// allows our api endpoints to access the database through Entity Framework Core
builder.Services.AddNpgsql<HipHopPizzaDbContext>(builder.Configuration["HipHopPizzaDbConnectionString"]);

// Set the JSON serializer options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
var app = builder.Build();

app.UseCors(DefaultCors);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//Check user
app.MapGet("/checkuser/{uid}", (HipHopPizzaDbContext db, string uid) =>
{
    var user = db.Users.Where(x => x.UID == uid).ToList();
    if (user == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(user);
});

//Register user
app.MapPost("/register", (HipHopPizzaDbContext db, User user) =>
{
    db.Users.Add(user);
    db.SaveChanges();
    return Results.Created($"/api/user/user.Id", user);
});

//Get all orders
app.MapGet("/orders", (HipHopPizzaDbContext db) =>
{
    List<Order> orders = db.Orders.ToList();
    if (!orders.Any())
    {
        return Results.NotFound("There are no orders in the system");
    }
    return Results.Ok(orders);
});

//Get single order and item details
app.MapGet("/orders/{id}", (HipHopPizzaDbContext db, int id) =>
{
    Order order = db.Orders
        .Include(o => o.Items)
        .FirstOrDefault(o => o.Id == id);
    if (order == null)
    {
        return Results.NotFound("This order does not exist");
    }
    return Results.Ok(order);
});

//Create Order
app.MapPost("/orders", (HipHopPizzaDbContext db, CreateOrderDTO order) =>
{
    Order orderEntity = new Order()
    {
        UserId = order.UserId,
        PaymentTypeId = order.PaymentTypeId,
        Name = order.Name,
        Phone = order.Phone,
        Email = order.Email,
        OrderType = order.OrderType
    };
    orderEntity.User = db.Users.FirstOrDefault(u => u.Id == order.UserId);
    orderEntity.PaymentType = db.PaymentTypes.FirstOrDefault(pt => pt.Id == order.PaymentTypeId);
    orderEntity.isOpen = true;

    try
    {
        db.Orders.Add(orderEntity);
        db.SaveChanges();
        return Results.Created($"/orders/{orderEntity.Id}", orderEntity);
    }
    catch (DbUpdateException)
    {
        return Results.NoContent();
    }
});

//Update Order
app.MapPut("/orders/{id}", (HipHopPizzaDbContext db, int id, UpdateOrderDTO order) =>
{
    Order orderToUpdate = db.Orders.FirstOrDefault(o => o.Id == id);
    if (orderToUpdate == null)
    {
        return Results.NotFound("This order does not exist");
    }

    orderToUpdate.UserId = order.UserId;
    orderToUpdate.PaymentTypeId = order.PaymentTypeId;
    orderToUpdate.Name = order.Name;
    orderToUpdate.Phone = order.Phone;
    orderToUpdate.Email = order.Email;
    orderToUpdate.OrderType = order.OrderType;
    orderToUpdate.TotalPrice = order.TotalPrice;
    orderToUpdate.Tip = order.Tip;
    orderToUpdate.Comments = order.Comments;

    db.Update(orderToUpdate);
    db.SaveChanges();
    return Results.Ok(orderToUpdate);
});

//Delete Order
app.MapDelete("/orders", (HipHopPizzaDbContext db, int id) =>
{
    Order order = db.Orders.FirstOrDefault(o => o.Id == id);
    if (order == null)
    {
        return Results.NotFound("Order does not exist");
    }
    db.Remove(order);
    db.SaveChanges();
    return Results.NoContent();
});

//Close order
app.MapPut("/orders/{id}/close", (HipHopPizzaDbContext db, int id, UpdateOrderDTO order) =>
{
    Order orderToUpdate = db.Orders.FirstOrDefault(o => o.Id == id);
    if (orderToUpdate == null)
    {
        return Results.NotFound("This order does not exist");
    }

    orderToUpdate.UserId = order.UserId;
    orderToUpdate.PaymentTypeId = order.PaymentTypeId;
    orderToUpdate.Name = order.Name;
    orderToUpdate.Phone = order.Phone;
    orderToUpdate.Email = order.Email;
    orderToUpdate.OrderType = order.OrderType;
    orderToUpdate.TotalPrice = order.TotalPrice;
    orderToUpdate.Tip = order.Tip;
    orderToUpdate.Comments = order.Comments;
    orderToUpdate.DateClosed = DateTime.Now;
    orderToUpdate.isOpen = false;

    db.Update(orderToUpdate);
    db.SaveChanges();
    return Results.Ok(orderToUpdate);
});

app.Run();

