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
        Name = order.Name,
        Phone = order.Phone,
        Email = order.Email,
        OrderType = order.OrderType
    };
    User user = db.Users.FirstOrDefault(u => u.UID == order.UID);
    orderEntity.UserId = user.Id;
    orderEntity.User = user;
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

    orderToUpdate.Name = order.Name;
    orderToUpdate.Phone = order.Phone;
    orderToUpdate.Email = order.Email;
    orderToUpdate.OrderType = order.OrderType;

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
app.MapPut("/orders/{id}/close", (HipHopPizzaDbContext db, int id, CloseOrderDTO order) =>
{
    Order orderToUpdate = db.Orders.FirstOrDefault(o => o.Id == id);
    if (orderToUpdate == null)
    {
        return Results.NotFound("This order does not exist");
    }

    orderToUpdate.PaymentTypeId = order.PaymentTypeId;
    orderToUpdate.Tip = order.Tip;
    orderToUpdate.Comments = order.Comments;
    orderToUpdate.DateClosed = DateTime.Now;
    orderToUpdate.TotalPrice += order.Tip;
    orderToUpdate.isOpen = false;

    db.Update(orderToUpdate);
    db.SaveChanges();
    return Results.Ok(orderToUpdate);
});

//Update order price
app.MapPut("/orders/price/{id}", (HipHopPizzaDbContext db, int id) =>
{
    Order orderToUpdate = db.Orders.FirstOrDefault(o => o.Id == id);
    if (orderToUpdate == null)
    {
        return Results.NotFound("This order does not exist");
    }

    List<OrderItem> orderItems = db.OrderItems
    .Include(oi => oi.Item)
    .Where(oi => oi.OrderId == id).ToList();

    decimal totalPrice = 0;
    foreach (OrderItem orderItem in orderItems)
    {
        totalPrice += orderItem.Item.Price;
    }
    orderToUpdate.TotalPrice = totalPrice;

    db.Orders.Update(orderToUpdate);
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
    return Results.Ok(itemToUpdate);
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
app.MapPost("/orders/items/{orderId}/{itemId}", (HipHopPizzaDbContext db, int orderId, int itemId) =>
{
    OrderItem orderItem = new OrderItem()
    {
        OrderId = orderId,
        ItemId = itemId,
        //Quantity = itemCount
    };

    db.OrderItems.Add(orderItem);
    db.SaveChanges();
    return Results.Ok(orderItem);
    //Possibilty of adding multiple items at a time
    //One endpoint for add item, one endpoint for update item
    //Create quantity property on OrderItem, update that when same item added twice
});

//Delete item from order
app.MapDelete("/orders/items/{orderId}/{itemId}", (HipHopPizzaDbContext db, int orderId, int itemId) =>
{
    OrderItem orderItem = db.OrderItems.FirstOrDefault(oi => oi.OrderId == orderId && oi.ItemId == itemId);
    if (orderItem == null)
    {
        return Results.NoContent();
    }
    db.OrderItems.Remove(orderItem);
    db.SaveChanges();
    return Results.NoContent();
});

//Count same item in an order
app.MapGet("/orders/items/{orderId}/{itemId}", (HipHopPizzaDbContext db, int orderId, int itemId) =>
{
    List<OrderItem> orderItems = db.OrderItems.Where(oi => oi.OrderId == orderId && oi.ItemId == itemId).ToList();
    int count = orderItems.Count();
    return Results.Ok(count);
});

//Get all OrderItems for an order
app.MapGet("/orders/items/{orderId}", (HipHopPizzaDbContext db, int orderId) =>
{
    List<OrderItem> orderItems = db.OrderItems
        .Include(oi => oi.Item)
        .Where(oi => oi.OrderId == orderId).ToList();

    if (orderItems.Count == 0)
    {
        return Results.NotFound("There are no items in this order");
    }
    return Results.Ok(orderItems);
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
        OrderType = order.OrderType,
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
    return Results.Ok(totalRevenue);
});

//Delete Revenue node
app.MapDelete("revenue/{orderId}", (HipHopPizzaDbContext db, int orderId) =>
{
    Revenue revenueNode = db.Revenues.FirstOrDefault(r => r.OrderId == orderId);
    if (revenueNode == null)
    {
        return Results.NoContent();
    }
    db.Revenues.Remove(revenueNode);
    db.SaveChanges();
    return Results.NoContent();
});

app.Run();

