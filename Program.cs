using HipHopPizzaBackend.Models;
using HipHopPizzaBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using System.Numerics;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.Features;

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
    var user = db.Users.Where(u => u.UID == uid).ToList();
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
        PaymentTypeId = order.PaymentTypeId,
        Name = order.Name,
        Phone = order.Phone,
        Email = order.Email,
        OrderType = order.OrderType
    };
    User user = db.Users.FirstOrDefault(o => o.UID === order.userId)
    orderEntity.User = db.Users.FirstOrDefault(u => u.Id == order.UserId);
    orderEntity.PaymentType = db.PaymentTypes.FirstOrDefault(pt => pt.Id == order.PaymentTypeId);
    orderEntity.isOpen = true;
    orderEntity.TotalPrice = 0;

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
app.MapDelete("/orders/{id}", (HipHopPizzaDbContext db, int id) =>
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

//Update order price
app.MapPut("/orders/price/{id}", (HipHopPizzaDbContext db, int id, UpdateOrderDTO order) =>
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

    decimal totalPrice = 0;
    foreach (Item item in orderToUpdate.Items)
    {
        totalPrice += item.Price;
    }
    orderToUpdate.TotalPrice = totalPrice;

    db.Update(orderToUpdate);
    db.SaveChanges();
    return Results.Ok(orderToUpdate);
});

//Get items
app.MapGet("/items", (HipHopPizzaDbContext db) =>
{
    List<Item> items = db.Items.ToList();
    if (!items.Any())
    {
        return Results.NotFound("There are no items");
    }
    return Results.Ok(items);
});

//Get single item
app.MapGet("/items/{id}", (HipHopPizzaDbContext db, int id) =>
{
    Item item = db.Items
    .Include(i => i.Orders)
    .FirstOrDefault(i => i.Id == id);
    if (item == null)
    {
        return Results.NotFound("The item was not found");
    }
    return Results.Ok(item);
});

//Create item
app.MapPost("/items", (HipHopPizzaDbContext db, CreateItemDTO item) =>
{
    Item itemEntity = new()
    {
        Name = item.Name,
        ImageUrl = item.ImageUrl,
        Price = item.Price
    };

    try
    {
        db.Items.Add(itemEntity);
        db.SaveChanges();
        return Results.Created($"/items/{itemEntity.Id}", itemEntity);
    }
    catch (DbUpdateException)
    {
        return Results.NoContent();
    }
});

//Update item
app.MapPut("/items/{id}", (HipHopPizzaDbContext db, int id, UpdateItemDTO item) =>
{
    Item itemToUpdate = db.Items.FirstOrDefault(i => i.Id == id);
    itemToUpdate.Name = item.Name;
    itemToUpdate.ImageUrl = item.ImageUrl;
    itemToUpdate.Price = item.Price;

    db.Update(itemToUpdate);
    db.SaveChanges();
    return Results.Ok();

});

//Delete item from menu
app.MapDelete("/items/{id}", (HipHopPizzaDbContext db, int id) =>
{
    Item item = db.Items.FirstOrDefault(i => i.Id == id);
    if (item == null)
    {
        return Results.NotFound("Item was not found");
    }
    db.Remove(item);
    db.SaveChanges();
    return Results.NoContent();
});

//Add item to order
app.MapPut("/orders/items/{orderId}/{itemId}", (HipHopPizzaDbContext db, int orderId, int itemId, CreateOrderDTO order) =>
{
    Order orderToUpdate = db.Orders
    .Include(o => o.Items)
    .FirstOrDefault(o => o.Id == orderId);

    Item itemToAdd = db.Items.FirstOrDefault(i => i.Id == itemId);

    orderToUpdate.UserId = order.UserId;
    orderToUpdate.PaymentTypeId = order.PaymentTypeId;
    orderToUpdate.Name = order.Name;
    orderToUpdate.Phone = order.Phone;
    orderToUpdate.Email = order.Email;
    orderToUpdate.OrderType = order.OrderType;
    orderToUpdate.Items.Add(itemToAdd);

    db.Update(orderToUpdate);
    db.SaveChanges();
    return Results.Ok(orderToUpdate);
});

//Delete item from order
app.MapDelete("/orders/items/{orderId}/{itemId}", (HipHopPizzaDbContext db, int orderId, int itemId) =>
{
    Order order = db.Orders
    .Include(o => o.Items.Where(i => i.Id == itemId))
    .FirstOrDefault(o => o.Id == orderId);

    if (order != null)
    {
        Item itemToRemove = order.Items.FirstOrDefault(i => i.Id == itemId);
        order.Items.Remove(itemToRemove);
    }
    db.SaveChanges();
    return Results.NoContent();
});

//Get all payment types
app.MapGet("/paymentTypes", (HipHopPizzaDbContext db) =>
{
    List<PaymentType> paymentTypes = db.PaymentTypes.ToList();
    if (!paymentTypes.Any())
    {
        return Results.NotFound("No payment types are available");
    }
    return Results.Ok(paymentTypes);
});

//Get single payment type
app.MapGet("/paymentTypes/{id}", (HipHopPizzaDbContext db, int id) =>
{
    PaymentType paymentType = db.PaymentTypes.FirstOrDefault(pt => pt.Id == id);
    if (paymentType == null)
    {
        return Results.NotFound("Payment type was not found");
    }
    return Results.Ok(paymentType);
});

//Create revenue node
app.MapPost("/revenueNodes", (HipHopPizzaDbContext db, Order order) =>
{
    PaymentType paymentType = db.PaymentTypes.FirstOrDefault(pt => pt.Id == order.PaymentTypeId);
    Revenue revenueNode = new()
    {
        OrderId = order.Id,
        PaymentTypeId = order.PaymentTypeId,
        OrderTotal = order.TotalPrice,
        Tip = order.Tip,
        DateClosed = DateTime.Now,
        PaymentType = paymentType
    };

    db.Revenues.Add(revenueNode);
    db.SaveChanges();
    return Results.Created($"/revenueNodes/{revenueNode.Id}", revenueNode);
});

//Get total revenue
app.MapGet("/revenue", (HipHopPizzaDbContext db) =>
{
    List<Revenue> revenueNodes = db.Revenues.ToList();
    decimal? totalRevenue = 0;
    foreach (Revenue revenueNode in revenueNodes)
    {
        totalRevenue += revenueNode.OrderTotal;
    }
});
//If there are issues it may be due to nullable ? on the revenue not being in the database - redo migrations if so
app.Run();

