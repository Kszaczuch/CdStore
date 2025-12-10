DOKUMENTACJA projektu CdStore

Ogólny opis

CdStore to prosty sklep internetowy z muzyk¹ (p³yty/albumy) zbudowany w ASP.NET Core Razor Pages / MVC (razor views) na platformie .NET 8. Projekt wykorzystuje Identity do obs³ugi u¿ytkowników, Entity Framework Core do warstwy dostêpu do danych oraz migracje EF do zarz¹dzania schematem bazy danych. Zawiera mechanikê koszyka, zamówieñ, ulubionych, panel konta oraz proste zarz¹dzanie u¿ytkownikami.

Wymagania

- .NET 8 SDK
- SQL Server (lub inny provider skonfigurowany w appsettings.json)
- Dotnet-ef (opcjonalnie, do stosowania migracji lokalnie)

Struktura projektu (najwa¿niejsze pliki i katalogi)

- Program.cs — konfiguracja aplikacji i us³ug.
- appsettings.json — ustawienia aplikacji, w tym connection string.
- CdStore/Services/ApplicationDbContext.cs — DbContext aplikacji i konfiguracja encji.
- CdStore/Services/SeedService.cs — serwis do seedowania danych pocz¹tkowych.
- CdStore/Models — modele domenowe, najwa¿niejsze pliki:
  - Users.cs — rozszerzenie IdentityUser (FullName, IsBlocked, DeliveryAddress).
  - Album.cs, Kategoria.cs — encje produktów i kategorii.
  - CartItem.cs, Order.cs, OrderItem.cs, Receipt.cs, Favorite.cs — encje koszyka/zamówieñ/paragonów/ulubionych.
  - OrderStatus.cs — statusy zamówieñ.
- CdStore/Controllers — kontrolery MVC:
  - AccountController.cs — logowanie, rejestracja, profil, zmiana has³a, lista u¿ytkowników itp.
  - HomeController.cs — strony g³ówne, przegl¹d, szczegó³y, koszyk.
  - OrderController.cs — obs³uga procesu zamówienia i widoków zamówieñ.
- CdStore/Views — widoki Razor dla akcji kontrolerów (layout, partials, widoki akcji).
- CdStore/Services/CartService.cs — logika koszyka przechowywana po stronie serwisu.
- CdStore/Migrations — wygenerowane migracje EF.

G³ówne funkcjonalnoœci

- Rejestracja i logowanie u¿ytkowników (ASP.NET Core Identity). Model Users rozszerza IdentityUser o pola FullName, IsBlocked (blokowanie u¿ytkownika) oraz DeliveryAddress.
- Przegl¹danie katalogu albumów i kategorii.
- Dodawanie produktów do koszyka oraz zarz¹dzanie koszykiem (CartService).
- Tworzenie zamówieñ, przegl¹danie historii zamówieñ i generowanie paragonów/receipt.
- Ulubione (Favorite) dla u¿ytkowników.
- Seedowanie przyk³adowych danych (SeedService) i migracje EF do tworzenia schematu bazy.

Przyk³ady kodu — jak to dzia³a (fragmenty)

1) Program.cs — rejestracja DbContext i Identity

```csharp
// ...typowy fragment z Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<Users, IdentityRole>(options =>
{
    // opcje has³a, lockout itp.
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// rejestracja serwisów aplikacyjnych
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<SeedService>();
```

2) ApplicationDbContext (wa¿ne mapowania i DbSety)

```csharp
public class ApplicationDbContext : IdentityDbContext<Users>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Album> Albums { get; set; }
    public DbSet<Kategoria> Kategoriass { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<Receipt> Receipts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // dodatkowe konfiguracje encji np. relacje, indeksy
    }
}
```

3) Model Users (rozszerzony IdentityUser)

```csharp
public class Users : IdentityUser
{
    [MaxLength(200)]
    public string FullName { get; set; }

    public bool IsBlocked { get; set; } = false;

    [MaxLength(300)]
    public string DeliveryAddress { get; set; }
}
```

4) Przyk³ad u¿ycia CartService w kontrolerze/home

```csharp
public class HomeController : Controller
{
    private readonly CartService _cartService;

    public HomeController(CartService cartService)
    {
        _cartService = cartService;
    }

    public IActionResult AddToCart(int albumId)
    {
        _cartService.AddToCart(albumId);
        return RedirectToAction("Index");
    }
}
```

5) Tworzenie zamówienia (schematycznie)

```csharp
// OrderController
var cart = _cartService.GetCartForUser(userId);
var order = new Order { UserId = userId, OrderDate = DateTime.UtcNow, Total = cart.Total };
// mapowanie pozycji
_context.Orders.Add(order);
await _context.SaveChangesAsync();
```

Konfiguracja bazy danych — przyk³adowy connection string (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CdStoreDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Jak po³¹czyæ projekt w Visual Studio z Microsoft SQL Server Express

1. Upewnij siê, ¿e SQL Server Express jest zainstalowany i dzia³a. Domyœlny instance czêsto to .\SQLEXPRESS lub (localdb)\MSSQLLocalDB.
2. W Visual Studio otwórz menu View -> SQL Server Object Explorer.
3. Kliknij prawym przyciskiem "SQL Server" -> Add SQL Server. Zaloguj siê u¿ywaj¹c Windows Authentication lub SQL Server Authentication.
4. Po dodaniu instancji rozwiñ serwer, kliknij prawym na "Databases" -> Add New Database i wpisz np. CdStoreDb.
5. W pliku appsettings.json ustaw connection string (jak wy¿ej), u¿ywaj¹c Server=.\SQLEXPRESS dla Express lub (localdb)\\MSSQLLocalDB dla LocalDB.
6. W konsoli Package Manager Console (Visual Studio) lub w terminalu wykonaj migracje:
   - dotnet ef migrations add InitialCreate
   - dotnet ef database update

Uwaga: Jeœli u¿ywasz Windows Authentication (Trusted_Connection=True) upewnij siê, ¿e konto uruchamiaj¹ce aplikacjê ma uprawnienia do bazy.

Szybkie kroki — synchronizacja projektu z SQL Server w praktyce

- Skonfiguruj connection string w appsettings.json.
- Uruchom Visual Studio -> SQL Server Object Explorer aby zobaczyæ bazê.
- Wykonaj migracje (Package Manager Console lub dotnet CLI).
- Uruchom aplikacjê i sprawdŸ, czy tabele siê pojawi³y.

Migracje i przydatne komendy

- Dodaj migracjê: dotnet ef migrations add NazwaMigracji
- Zastosuj migracje: dotnet ef database update
- Usuñ ostatni¹ migracjê (jeœli nie zosta³a zastosowana): dotnet ef migrations remove

Seedowanie danych

Projekt zawiera SeedService, który podczas uruchomienia (jeœli skonfigurowany w Program.cs) tworzy konta, przyk³adowe kategorie i albumy. Jeœli chcesz dodaæ w³asne dane, zmodyfikuj SeedService lub uruchom narzêdzie Seed rêcznie z poziomu aplikacji.

Bezpieczeñstwo i uwagi

- Pole IsBlocked w modelu Users pozwala blokowaæ dostêp u¿ytkownikom — sprawdŸ kontrolery logiki autoryzacji, ¿eby respektowa³y tê flagê.
- Nie przechowuj w repozytorium prawdziwych credentiali. W œrodowisku produkcyjnym u¿yj Secret Manager lub zmiennych œrodowiskowych.

Dalsze kierunki rozwoju

- Panel administracyjny do zarz¹dzania produktami, kategoriami i zamówieniami.
- Integracja p³atnoœci (np. Stripe, PayU).
- Obs³uga zdjêæ produktów (przechowywanie w chmurze lub folderze wwwroot).
- Testy jednostkowe i integracyjne dla serwisów i kontrolerów.

Kontakt i rozwój

Projekt znajduje siê w repozytorium: https://github.com/Kszaczuch/CdStore (remote origin). Wszelkie zmiany zg³aszaæ przez pull requesty; do dyskusji u¿ywaæ issues w repozytorium.

Lista najwa¿niejszych plików do szybkiego przegl¹du

- Program.cs
- Services/ApplicationDbContext.cs
- Services/SeedService.cs
- Services/CartService.cs
- Controllers/AccountController.cs
- Controllers/HomeController.cs
- Controllers/OrderController.cs
- Models/Users.cs, Album.cs, Order.cs, OrderItem.cs
- Views/* (szczególnie Views/Account i Views/Order)

Koniec dokumentacji
