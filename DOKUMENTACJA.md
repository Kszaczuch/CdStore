<h1 align="center">DOKUMENTACJA projektu CdStore</h1>

<h2>Ogólny opis</h2>

CdStore to prosty sklep internetowy z płytami CD zbudowany w ASP.NET Core Razor Pages / MVC (razor views) na platformie .NET 8. Projekt wykorzystuje Identity do obsługi użytkowników, Entity Framework Core do warstwy dostępu do danych oraz migracje EF do zarządzania schematem bazy danych. Zawiera mechanikę koszyka, zamówień, ulubionych, panel konta oraz proste zarządzanie użytkownikami.

<h3>Wymagania</h3>

- .NET 8 SDK
- SQL Server (lub inny provider skonfigurowany w appsettings.json)
- Dotnet-ef (opcjonalnie, do stosowania migracji lokalnie)

<h3>Struktura projektu (najważniejsze pliki i katalogi)</h3>

- Program.cs — konfiguracja aplikacji i usług.
- appsettings.json — ustawienia aplikacji, w tym connection string.
- CdStore/Services/ApplicationDbContext.cs — DbContext aplikacji i konfiguracja encji.
- CdStore/Services/SeedService.cs — serwis do seedowania danych początkowych.
- CdStore/Models — modele domenowe, najważniejsze pliki:
  - Users.cs — rozszerzenie IdentityUser (FullName, IsBlocked, DeliveryAddress).
  - Album.cs, Kategoria.cs — encje produktów i kategorii.
  - CartItem.cs, Order.cs, OrderItem.cs, Receipt.cs, Favorite.cs — encje koszyka/zamówień/paragonów/ulubionych.
  - OrderStatus.cs — statusy zamówień.
- CdStore/Controllers — kontrolery MVC:
  - AccountController.cs — logowanie, rejestracja, profil, zmiana hasła, lista użytkowników itp.
  - HomeController.cs — strony główne, przegląd, szczegóły, koszyk.
  - OrderController.cs — obsługa procesu zamówienia i widoków zamówień.
- CdStore/Views — widoki Razor dla akcji kontrolerów (layout, partials, widoki akcji).
- CdStore/Services/CartService.cs — logika koszyka przechowywana po stronie serwisu.
- CdStore/Migrations — wygenerowane migracje EF.

<h3>Główne funkcjonalności</h3>

- Rejestracja i logowanie użytkowników (ASP.NET Core Identity). Model Users rozszerza IdentityUser o pola FullName, IsBlocked (blokowanie użytkownika) oraz DeliveryAddress.
- Przeglądanie katalogu albumów i kategorii.
- Dodawanie produktów do koszyka oraz zarządzanie koszykiem (CartService).
- Tworzenie zamówień, przeglądanie historii zamówień i generowanie paragonów/receipt.
- Ulubione (Favorite) dla użytkowników.
- Seedowanie przykładowych danych (SeedService) i migracje EF do tworzenia schematu bazy.

<h2>Przykłady kodu — jak to działa (fragmenty)</h2>

<h3>1) Program.cs — rejestracja DbContext i Identity</h3>

<p>Krótkie wyjaśnienie: Ten fragment pokazuje rejestrację usług aplikacji w kontenerze DI. DbContext konfiguruje połączenie do bazy danych, a Identity rejestruje mechanizmy uwierzytelniania i przechowywanie użytkowników w EF. CartService i SeedService są dodawane jako serwisy aplikacyjne.</p>

```csharp
// ...typowy fragment z Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<Users, IdentityRole>(options =>
{
    // opcje hasła, lockout itp.
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// rejestracja serwisów aplikacyjnych
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<SeedService>();
```

<p>Gdzie jest używane: Kod znajduje się w Program.cs i wykonuje się podczas uruchamiania aplikacji. Dzięki temu kontrolery i strony otrzymują zainicjowane instancje serwisów przez DI.</p>

<h3>2) ApplicationDbContext (ważne mapowania i DbSety)</h3>

<p>Krótkie wyjaśnienie: ApplicationDbContext to klasa pochodna IdentityDbContext, która definiuje zestawy tabel (DbSet) dla encji aplikacji. Metoda OnModelCreating służy do dodatkowej konfiguracji modelu (relacje, indeksy, ograniczenia).</p>

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

<p>Gdzie jest używane: ApplicationDbContext jest wstrzykiwany do serwisów i kontrolerów tam, gdzie potrzebny jest dostęp do bazy danych (np. SeedService, kontrolery zamówień, serwis koszyka).</p>

<h3>3) Model Users (rozszerzony IdentityUser)</h3>

<p>Krótkie wyjaśnienie: Klasa Users rozszerza IdentityUser, dodając pola specyficzne dla aplikacji (FullName, IsBlocked, DeliveryAddress). Pole IsBlocked pozwala na implementację mechanizmu blokowania konta na poziomie logiki aplikacji.</p>

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

<p>Gdzie jest używane: Ten model jest używany przez Identity oraz wszędzie, gdzie potrzebne są dodatkowe informacje o użytkowniku (Profile, Orders, Checkout).</p>

<h3>4) Przykład użycia CartService w kontrolerze/home</h3>

<p>Krótkie wyjaśnienie: Przykład ilustruje prostą integrację serwisu koszyka w kontrolerze. Metoda AddToCart wywołuje metodę serwisu odpowiedzialną za dodanie pozycji do koszyka, a następnie przekierowuje użytkownika.</p>

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

<p>Gdzie jest używane: HomeController (lub inny kontroler) wstrzykuje CartService przez DI. CartService realizuje logikę dodawania, usuwania i pobierania zawartości koszyka.</p>

<h3>5) Tworzenie zamówienia (schematycznie)</h3>

<p>Krótkie wyjaśnienie: Fragment pokazuje podstawowy przepływ tworzenia zamówienia z zawartości koszyka: pobranie koszyka, utworzenie obiektu Order, zapis do kontekstu i zapis do bazy.</p>

```csharp
// OrderController
var cart = _cartService.GetCartForUser(userId);
var order = new Order { UserId = userId, OrderDate = DateTime.UtcNow, Total = cart.Total };
// mapowanie pozycji
_context.Orders.Add(order);
await _context.SaveChangesAsync();
```

<p>Gdzie jest używane: Logika tworzenia zamówienia znajduje się w OrderController lub w dedykowanym serwisie zamówień; powinna też tworzyć rekordy OrderItem na podstawie pozycji z koszyka.</p>

<h3>6)Konfiguracja bazy danych — przykładowy connection string (appsettings.json)</h3>

<p>Krótkie wyjaśnienie: Connection string określa serwer bazy danych, nazwę bazy oraz sposób uwierzytelniania. Poniższy przykład używa instancji SQL Server Express i uwierzytelnienia Windows (Trusted Connection).</p>

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CdStoreDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

<p>Gdzie jest używane: Connection string jest odczytywany w Program.cs i przekazywany do konfiguracji DbContext, aby aplikacja mogła łączyć się z bazą danych.</p>

<h2>Jak połączyć projekt w Visual Studio z Microsoft SQL Server Express</h2>

1. Upewnij się, że SQL Server Express jest zainstalowany i działa. Domyślny instance często to .\SQLEXPRESS lub (localdb)\MSSQLLocalDB.
2. W Visual Studio otwórz menu View -> SQL Server Object Explorer.
3. Kliknij prawym przyciskiem "SQL Server" -> Add SQL Server. Zaloguj się używając Windows Authentication lub SQL Server Authentication.
4. Po dodaniu instancji rozwiń serwer, kliknij prawym na "Databases" -> Add New Database i wpisz np. CdStoreDb.
5. W pliku appsettings.json ustaw connection string (jak wyżej), używając Server=.\SQLEXPRESS dla Express lub (localdb)\\MSSQLLocalDB dla LocalDB.
6. W konsoli Package Manager Console (Visual Studio) lub w terminalu wykonaj migracje:
   - dotnet ef migrations add InitialCreate
   - dotnet ef database update

<strong>Uwaga: Jeśli używasz Windows Authentication (Trusted_Connection=True) upewnij się, że konto uruchamiające aplikację ma uprawnienia do bazy.</strong>

<h3>Szybkie kroki — synchronizacja projektu z SQL Server w praktyce</h3>

- Skonfiguruj connection string w appsettings.json.
- Uruchom Visual Studio -> SQL Server Object Explorer aby zobaczyć bazę.
- Wykonaj migracje (Package Manager Console lub dotnet CLI).
- Uruchom aplikację i sprawdź, czy tabele się pojawiły.

<h3>Migracje i przydatne komendy</h3>

- Dodaj migrację: dotnet ef migrations add NazwaMigracji
- Zastosuj migracje: dotnet ef database update
- Usuń ostatnią migrację (jeśli nie została zastosowana): dotnet ef migrations remove

<h3>Seedowanie danych</h3>

Projekt zawiera SeedService, który podczas uruchomienia (jeśli skonfigurowany w Program.cs) tworzy konta, przykładowe kategorie i albumy. Jeśli chcesz dodać własne dane, zmodyfikuj SeedService lub uruchom narzędzie Seed ręcznie z poziomu aplikacji.

<h2>Bezpieczeństwo i uwagi</h2>

- Pole IsBlocked w modelu Users pozwala blokować dostęp użytkownikom — sprawdź kontrolery logiki autoryzacji, żeby respektowały tę flagę.
- Nie przechowuj w repozytorium prawdziwych credentiali. W środowisku produkcyjnym użyj Secret Manager lub zmiennych środowiskowych.

<h2>Kontakt i rozwój</h2>

Projekt znajduje się w repozytorium: https://github.com/Kszaczuch/CdStore (remote origin). Wszelkie zmiany zgłaszać przez pull requesty; do dyskusji używać issues w repozytorium.

<h2>Lista najważniejszych plików do szybkiego przeglądu</h2>

- Program.cs
- Services/ApplicationDbContext.cs
- Services/SeedService.cs
- Services/CartService.cs
- Controllers/AccountController.cs
- Controllers/HomeController.cs
- Controllers/OrderController.cs
- Models/Users.cs, Album.cs, Order.cs, OrderItem.cs
- Views/* (szczególnie Views/Account i Views/Order)
