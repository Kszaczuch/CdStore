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

<h2>Przykłady kodu — rzeczywiste wycinki z projektu</h2>

<h3>1) Program.cs — rejestracja DbContext, Identity i serwisów</h3>
<p>Wyciąg z pliku CdStore/Program.cs — pokazuje jak skonfigurowane są usługi aplikacji (DbContext, Identity, Cookie, rejestracja serwisów i seedowanie bazy):</p>

```csharp
using CdStore.Models;
using CdStore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});
builder.Services.AddScoped<CdStore.Services.CartService>();
builder.Services.AddIdentity<Users, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

await SeedService.SeedDatabase(app.Services);
```

<p>Gdzie: Program.cs; uruchamia się przy starcie aplikacji — rejestruje wymagane serwisy i inicjuje seed danych.</p>

<h3>2) ApplicationDbContext — rzeczywisty DbContext z projektu</h3>
<p>Wyciąg z CdStore/Services/ApplicationDbContext.cs — definicje DbSetów używanych w aplikacji:</p>

```csharp
using CdStore.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<Users>
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Album> Albumy { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<Kategoria> Kategorie { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
}
```

<p>Gdzie: ApplicationDbContext jest wstrzykiwany do kontrolerów i serwisów (np. OrderController, CartService) i odpowiada za dostęp do tabel w bazie.</p>

<h3>3) Model Users — faktyczny model z projektu</h3>
<p>Wyciąg z CdStore/Models/Users.cs — dodatkowe pola powiązane z użytkownikiem:</p>

```csharp
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class Users : IdentityUser
{
    [MaxLength(200)]
    public string FullName { get; set; }

    public bool IsBlocked { get; set; } = false;

    [MaxLength(300)]
    public string DeliveryAddress { get; set; }
}
```

<p>Gdzie: Model używany przez Identity i wszędzie tam, gdzie aplikacja potrzebuje dodatkowych informacji o użytkowniku (np. OrderController, AccountController).</p>

<h3>4) CartService — fragmenty implementacji z projektu</h3>
<p>Wyciąg z CdStore/Services/CartService.cs — pokazany jest rzeczywisty kod metody dodającej pozycję do koszyka:</p>

```csharp
public bool Add(string cartId, int albumId, int quantity = 1)
{
    if (string.IsNullOrEmpty(cartId)) return false;
    if (quantity <= 0) return false;

    var album = _db.Albumy.Find(albumId);
    if (album == null) return false;

    var maxAllowed = Math.Min(5, album.IloscNaStanie);
    if (maxAllowed <= 0) return false;

    var existing = _db.CartItems.FirstOrDefault(ci => ci.CartId == cartId && ci.AlbumId == albumId);
    if (existing == null)
    {
        var qty = Math.Min(quantity, maxAllowed);
        _db.CartItems.Add(new CartItem { CartId = cartId, AlbumId = albumId, Quantity = qty });
    }
    else
    {
        var newQty = existing.Quantity + quantity;
        existing.Quantity = Math.Min(newQty, maxAllowed);
        _db.CartItems.Update(existing);
    }

    _db.SaveChanges();
    return true;
}
```

<p>Gdzie: CartService jest wstrzykiwany do kontrolerów (np. HomeController, AccountController, OrderController) i obsługuje logikę koszyka.</p>

<h3>5) Checkout i zapis zamówienia — rzeczywista logika z OrderController</h3>
<p>Wyciąg z CdStore/Controllers/OrderController.cs — fragment POST Checkout, który tworzy Order i OrderItemy oraz aktualizuje stany magazynowe:</p>

```csharp
var order = new Order
{
    UserId = userId,
    FirstName = model.FirstName,
    LastName = model.LastName,
    Address = model.Address,
    Phone = model.Phone,
    Email = model.Email,
    CreatedAt = DateTime.UtcNow,
    IsPaid = false,
    Total = cartItems.Sum(a => a.Cena * (ids2Detailed.First(ci => ci.AlbumId == a.Id).Quantity))
};

foreach (var album in cartItems)
{
    var qty = ids2Detailed.First(ci => ci.AlbumId == album.Id).Quantity;
    order.Items.Add(new OrderItem
    {
        AlbumId = album.Id,
        Quantity = qty,
        UnitPrice = album.Cena
    });

    album.IloscNaStanie -= qty;
}

_context.Orders.Add(order);
_context.SaveChanges();

_cartService.Clear(cartId2);
```

<p>Gdzie: Fragment znajduje się w OrderController i to on zapisuje zamówienie w bazie po przejściu przez stronę checkout.</p>

<h3>6) Opłacenie zamówienia i generowanie paragonu (PDF)</h3>
<p>Wyciąg z OrderController — metoda Pay oznacza zamówienie jako opłacone, tworzy Receipt, a DownloadReceipt generuje PDF przy użyciu QuestPDF:</p>

```csharp
// ustawienie płatności
order.IsPaid = true;

var receipt = new Receipt
{
    OrderId = order.Id,
    Number = $"R-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
    PaymentMethod = Enum.Parse<PaymentMethod>(method)
};

_context.Receipts.Add(receipt);
_context.SaveChanges();
```

```csharp
// generowanie PDF (wycinek)
var pdfBytes = doc.GeneratePdf();
var fileName = $"Paragon_{receipt.Number}.pdf";
return File(pdfBytes, "application/pdf", fileName);
```

<p>Gdzie: Endpoint Pay i DownloadReceipt w OrderController obsługują płatność i pobranie paragonu.</p>

<h3>7) Connection string — aktualny w projekcie (appsettings.json)</h3>

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=.\\SQLEXPRESS;Initial Catalog=cdstoredb;Integrated Security=True;TrustServerCertificate=True"
}
```

<p>Gdzie: appsettings.json — connection string jest odczytywany w Program.cs i przekazywany do DbContext.</p>

<h2>Operacje użytkownika i administratora — krok po kroku (bez kodu)</h2>

<p>W tej sekcji znajdują się opisy kroków, jakie wykonuje użytkownik lub administrator w aplikacji. Nie ma tu fragmentów kodu — tylko instrukcje operacyjne i miejsca, gdzie warto dodać zrzuty ekranu.</p>

<h3>Rejestracja konta</h3>
<p>Co robi: Tworzy nowe konto użytkownika. Wypełnij formularz rejestracji (Email, Hasło, Imię i nazwisko) i zatwierdź. Po rejestracji konto jest utworzone i użytkownik może się zalogować.</p>
<p><img src="/img/screenshots/register.png" alt="Formularz rejestracji" /></p>

<h3>Logowanie</h3>
<p>Co robi: Uwierzytelnia użytkownika. Wypełnij formularz logowania (Email, Hasło) i zatwierdź, aby uzyskać dostęp do konta i funkcji chronionych.</p>
<p><img src="/img/screenshots/login.png" alt="Formularz logowania" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/login.png</p>

<h3>Zmiana danych osobowych</h3>
<p>Co robi: Pozwala użytkownikowi zaktualizować swoje dane (FullName, DeliveryAddress, Phone). Przejdź do profilu i edytuj pola, a następnie zapisz zmiany.</p>
<p><img src="/img/screenshots/profile.png" alt="Formularz profilu" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/profile.png</p>

<h3>Przeglądanie katalogu</h3>
<p>Co robi: Przeglądaj listę albumów, filtruj i sortuj według kategorii lub ceny. Kliknij album, aby zobaczyć szczegóły i okładkę.</p>
<p><img src="/img/screenshots/album-details.png" alt="Szczegóły albumu" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/album-details.png</p>

<h3>Dodanie albumu do koszyka</h3>
<p>Co robi: Dodaje wybrany album do koszyka. Użyj przycisku "Do koszyka" na karcie albumu lub na stronie szczegółów.</p>
<p><img src="/img/screenshots/add-to-cart.png" alt="Dodaj do koszyka" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/add-to-cart.png</p>

<h3>Zarządzanie koszykiem</h3>
<p>Co robi: Pokazuje zawartość koszyka, pozwala zmienić ilości i usuwać pozycje. Przejdź do widoku Koszyk, aby zaktualizować zawartość przed checkout.</p>
<p><img src="/img/screenshots/cart.png" alt="Widok koszyka" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/cart.png</p>

<h3>Checkout / Złożenie zamówienia</h3>
<p>Co robi: Wprowadź dane dostawy i zatwierdź zamówienie. System tworzy zamówienie (Order) i pozycje (OrderItem), rezerwuje stany magazynowe i czyści koszyk.</p>
<p><img src="/img/screenshots/checkout.png" alt="Podsumowanie zamówienia" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/checkout.png</p>

<h3>Opłacenie zamówienia</h3>
<p>Co robi: Po potwierdzeniu płatności zamówienie jest oznaczane jako opłacone, a system generuje paragon (Receipt). Integracja z bramką płatności wymaga konfiguracji zewnętrznej.</p>
<p><img src="/img/screenshots/payment.png" alt="Proces płatności" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/payment.png</p>

<h3>Pobranie paragonu</h3>
<p>Co robi: Umożliwia pobranie paragonu w formacie PDF z widoku szczegółów zamówienia. Kliknij "Pobierz paragon" i otrzymasz plik PDF generowany przez QuestPDF.</p>
<p><img src="/img/screenshots/receipt.png" alt="Pobierz paragon" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/receipt.png</p>

<h3>Dodawanie / edycja albumu i gatunku (Admin)</h3>
<p>Co robi: W panelu administracyjnym możesz dodać nowy album lub gatunek (kategorie) oraz edytować istniejące wpisy. Formularze admina umożliwiają ustawienie tytułu, artysty, ceny, ilości na stanie i linku do okładki.</p>
<p><img src="/img/screenshots/admin-albums.png" alt="Panel admin - albumy" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/admin-albums.png</p>

<h3>Blokowanie / odblokowywanie użytkownika (Admin)</h3>
<p>Co robi: Na liście użytkowników możesz zablokować konto, ustawiając flagę IsBlocked; zablokowany użytkownik nie może składać zamówień ani wykonywać płatności. Możesz również przywrócić konto ustawiając IsBlocked = false.</p>
<p><img src="/img/screenshots/admin-users.png" alt="Panel admin - użytkownicy" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/admin-users.png</p>

<h3>Zarządzanie zamówieniami (Admin)</h3>
<p>Co robi: Panel administracyjny pokazuje wszystkie zamówienia; możesz filtrować po statusie i zmieniać statusy (np. oczekujące, wysłane, dostarczone). Możesz też pobierać paragon klienta lub generować notatki wysyłkowe.</p>
<p><img src="/img/screenshots/admin-orders.png" alt="Panel admin - zamówienia" /></p>
<p>Ścieżka do obrazu: wwwroot/img/screenshots/admin-orders.png</p>

<p>Instrukcja dodawania obrazków w dokumentacji:</p>
<!-- Przykład znacznika: <a href="/Home/Detale/1"><img src="/img/covers/okladka.jpg" alt="Okładka albumu" /></a>
     Ścieżka do folderu obrazków w projekcie: wwwroot/img/ (np. wwwroot/img/screenshots/register.png lub wwwroot/img/covers/okladka.jpg) -->

<p>Uwaga: Pliki obrazów umieszczaj w katalogu wwwroot/img/ i odwołuj się do nich ścieżką względną zaczynając od /img/ (np. /img/screenshots/register.png).</p>

<h2>Uwagi końcowe</h2>

- Dopasuj zrzuty ekranu do ścieżek podanych w komentarzach.
- Integracja płatności i bardziej zaawansowane operacje (PDF, zewnętrzne API) wymagają dodatkowych zależności.

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
