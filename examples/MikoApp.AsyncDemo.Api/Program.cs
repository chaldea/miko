var builder = WebApplication.CreateBuilder(args);

// Add CORS to allow the Miko client to call the API from localhost:5000
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

// In-memory product data
var products = new List<Product>
{
    new(1, "Laptop", 1299.99m, "Electronics"),
    new(2, "Wireless Mouse", 29.99m, "Electronics"),
    new(3, "Mechanical Keyboard", 149.99m, "Electronics"),
    new(4, "USB-C Hub", 49.99m, "Electronics"),
    new(5, "Monitor 27\"", 399.99m, "Electronics"),
    new(6, "Desk Lamp", 39.99m, "Home"),
    new(7, "Office Chair", 299.99m, "Furniture"),
    new(8, "Standing Desk", 599.99m, "Furniture"),
    new(9, "Webcam HD", 89.99m, "Electronics"),
    new(10, "Headphones", 199.99m, "Electronics"),
    new(11, "Coffee Mug", 12.99m, "Home"),
    new(12, "Water Bottle", 19.99m, "Home"),
    new(13, "Notebook", 8.99m, "Stationery"),
    new(14, "Pen Set", 15.99m, "Stationery"),
    new(15, "Backpack", 79.99m, "Bags"),
    new(16, "Phone Stand", 24.99m, "Accessories"),
    new(17, "Cable Organizer", 14.99m, "Accessories"),
    new(18, "Desk Mat", 34.99m, "Home"),
    new(19, "Monitor Arm", 129.99m, "Furniture"),
    new(20, "Footrest", 44.99m, "Furniture"),
};

app.MapGet("/api/products", async (string? search) =>
{
    // Simulate network delay
    await Task.Delay(3000);

    if (string.IsNullOrWhiteSpace(search))
        return products;

    var searchLower = search.ToLower();
    return products.Where(p =>
        p.Name.ToLower().Contains(searchLower) ||
        p.Category.ToLower().Contains(searchLower)).ToList();
});

app.Run("http://localhost:5000");

record Product(int Id, string Name, decimal Price, string Category);
