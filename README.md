<h1 align="Center">CdStore</h1>

<h3>CdStore to prosta aplikacja asp.net która służy do kupna płyt CD. <br> 
Poniżej zamieszczony jest krótki poradnik jak uruchomić aplikacje oraz przedstawienie jej funkcji </h3>


<h2>Uruchomienie aplikacji:</h2>

- Na początku sklonuj repozytorium w konsoli za pomocą gita:
git clone https://github.com/Kszaczuch/CdStore.git
cd CdStore

- Otwórz plik z aplikacją np. w Visual Studio, używając pliku CdStore.sln

- Upewnij się czy masz zainstalowany wymagany framework do działania tj. asp.net core

- Utwórz połączenie z bazą danych microsoft sql server express

- Uruchom aplikacje klikając przycisk "Startu" w Visual Studio bądź używając skrótu klawiaturowego CTRL + F5

<h2>Wykonanie migracji:</h2>

- Aplikacja korzysta z Entity Framework Core, dlatego migracje wykonuje się w "Package Manager Console"

- Aby utworzyć migracje musimy użyć następującej komendy:
Add-Migration NazwaMigracji

- Żeby migracje zastosować bądź ją zaktualizować używamy polecenia:
Update-Database

<h2>Przedstawienie funkcji</h2>
<h3>Przykładowe konta:</h3>

- Są przygotowane dwa przykładowe konta użytkowników

- Pierwsze to konto admina z następującymi danymi:
Email: admin@example.com 
Hasło: Haslo123

- Drugie konto jest z uprawnieniami zwykłego użytkownika:
Email: user@example.com 
Hasło: Haslo123

<h3>Przykładowy scenariusz:</h3>

- Uruchom aplikację.

- Zaloguj się jako admin.

- Przejdź do panelu zarządzania, najpierw do zakładki "Gatunki" żeby dodać gatunki muzyczne, a następnie do zakładki "Albumy" w której będziesz mógł dodać klika płyt.

- Wyloguj się.

- Zaloguj się jako zwykły użytkownik.

- Przejdź do sekcji profil i zmień swoje dane np. hasło

- Przejrzyj dostępne płyty.

- Użyj opcji filtrowania by znaleźć interesujące pozycje 

- Dodaj jakąś płytę do ulubionych klikając w ikonę serca

- Wyświetl swoje ulubione pozycje klikając na swoje imię i przechodząc do zakładki "Ulubione"

- Dodaj kilka produktów do koszyka.

- Złóż zamówienie.

- Wyloguj się.

- Zaloguj się ponownie jako admin i sprawdź listę zamówień

- W liście zamówień spróbuj zmienić status zamówienia

- Przejdź do zakładki "Użytkownicy" i zablokuj konto użytkownika

- Wyloguj się

- Zaloguj się ponownie jako użytkownik i spróbuj dodać produkt do koszyka

- Wyskoczy Ci alert z informacją o tym, że twoje konto zostało zablokowane i nie możesz dokonać transakcji

- Zamknij aplikację poprzez zamknięcie okna i użycie skrótu klawiaturowego w konsoli CTRL + C



