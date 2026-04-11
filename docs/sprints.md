### Przemysław Borczak, Bartosz Cichomski, Marek Makochon, Łukasz Przybylski

**Wybrane technologie:** C#, .NET, PostgreSQL, Entity Framework, React, Azure, Leaflet

### Sprint 1: Podział zadań w Clean Architecture

#### DevOps, Szkielet i Baza Danych (Infrastructure & API)

**Cel:** Przygotowanie fundamentów projektu, aby reszta zespołu miała na czym pracować.
* **Szkielet Solucji:** Utworzenie projektów w .NET zachowując podział na warstwy: `Core` (Domain), `Application`, `Infrastructure`, `API` (Presentation).
* **Baza Danych (Infrastructure):** Skonfigurowanie połączenia z bazą PostgreSQL.
* **Entity Framework (Infrastructure):** Instalacja paczek EF Core, skonfigurowanie `DbContext` i przygotowanie pierwszej migracji dla bazy danych.
* **Bezpieczeństwo (API):** Skonfigurowanie wymuszania protokołu HTTPS dla całej aplikacji.
* **Dokumentacja (API):** Konfiguracja Swaggera do testowania endpointów.

#### Serce Systemu (Domain & Application)

* **Cel:** Oprogramowanie czystej logiki biznesowej, bez dotykania bazy danych czy protokołu HTTP.
* **Modele Domenowe (Domain):** Utworzenie klasy `User` z polami: `id`, `name`, `surname`, `email`, `passwordHash`, `pfpUrl`, `dateOfBirth`, `bio`.
* **Role (Domain):** Stworzenie enumeratora `UserRole` (`REGULAR_USER`, `ADMIN`) i przypisanie go do użytkownika.
* **Kontrakty (Application):** Stworzenie interfejsu `IUserRepository` definiującego metody: `save()`, `findById()`, `findByEmail(email: string)`.
* **Logika (Application):** Utworzenie interfejsu i klasy `AuthService`, która będzie obsługiwać logikę biznesową metod `register(UserDTO)`, `login(email, password)` oraz `resetPassword(email)`.
* **Zewnętrzne Kontrakty (Application):** Stworzenie interfejsów do hashowania haseł (np. `IPasswordHasher`) oraz generowania tokenów (np. `IJwtProvider`).

#### Implementacja i Wystawienie API (Infrastructure & API)

* **Cel:** Spięcie logiki z bazą danych i wystawienie danych na zewnątrz.

* **Implementacja Repozytorium (Infrastructure):** Napisanie klasy `UserRepository` implementującej `IUserRepository`, która wykorzystuje EF Core do fizycznego zapisu/odczytu użytkowników w PostgreSQL.
* **Narzędzia (Infrastructure):** Zaimplementowanie mechanizmu hashowania haseł oraz klasy generującej i walidującej tokeny JWT (implementacja `IJwtProvider`).
* **Kontrolery (API):** Stworzenie `AuthController` i endpointu `POST api/auth/login` dla logowania standardowego.
* **Logowanie Zewnętrzne (API):** Utworzenie endpointu `POST api/auth/google` przyjmującego parametr `id_token`. 
* **Formatowanie Odpowiedzi (API):** Zmapowanie wyników na Auth Response DTO zawierające `token`, `expiresIn` oraz obiekt `user` (z polami `id`, `firstName`, `role`).

#### Aplikacja Kliencka (Frontend - React)

* **Cel:** Zbudowanie interfejsu użytkownika i spięcie go z gotowym backendem.
* **Inicjalizacja (Frontend):** Utworzenie projektu w React (np. Vite/Create React App) i podstawowa konfiguracja routingu.
* **Widoki (Frontend):** Zbudowanie prostego formularza logowania (email, hasło) oraz rejestracji.
* **Integracja (Frontend):** Podpięcie żądań HTTP (np. za pomocą biblioteki Axios lub Fetch) do endpointu `POST api/auth/login`.
* **Zarządzanie Stanem (Frontend):** Obsługa poprawnego logowania (kod 200) polegająca na zapisaniu odebranego tokenu JWT  (np. w LocalStorage lub odpowiednim Context API).
* **Obsługa Błędów (Frontend):** Obsługa błędu autoryzacji (kod 401)  i wyświetlenie stosownego komunikatu dla użytkownika (np. "Błędny email lub hasło").

### Sprint 2: Core Biznesowy – Oferty Przejazdów

**Cel:** Umożliwienie kierowcom dodawania tras, a pasażerom ich przeglądania.

* **Domain (Core):**
* Wdrożenie głównej encji `Trip` agregującej dane o trasie (`route`), terminie (`date`), identyfikatorze kierowcy (`driverId`) i kosztach (`price`).
* Definicja stanów oferty (Aktywna, Odwołana).

* **Application:**
* Zdefiniowanie interfejsów `ITripRepository` oraz `IPassengerOperations`.
* Implementacja `TripService` pozwalającego na tworzenie (`createTrip`) i wyszukiwanie ofert (`searchTrips` przyjmujące obiekt `Criteria`).

* **Infrastructure:**
* Implementacja `TripRepository` (EF Core) do zapisu i filtrowania wycieczek w bazie PostgreSQL.

* **Presentation (API):**
* Uruchomienie endpointów `POST api/trips` do publikacji oraz `GET api/trips` do wyszukiwania z filtrami.

### Sprint 3: Rezerwacje i Zarządzanie Przejazdem

**Cel:** Połączenie kierowcy z pasażerem i obsługa logiki zajmowania miejsc.

* **Domain (Core):**
* Implementacja logiki domenowej w encji `Trip` zapobiegającej przekroczeniu limitu miejsc (enkapsulacja - metoda np. `TryAddPassenger`).
* Zarządzanie stanami "Pending" i "Pełna".

* **Application:**
* Rozbudowa `TripService` o logikę `addPassenger(tripId, userId)`.
* Obsługa wyjątków domenowych (np. `SeatUnavailableException`), które później zamienią się w błąd 409 Conflict.

* **Infrastructure:**
* Obsługa współbieżności w EF Core (Concurrency Tokens), aby uniknąć *race conditions* przy rezerwowaniu ostatniego miejsca.

* **Presentation (API):**
* Uruchomienie endpointów `/api/trips/{id}/request` (rezerwacja) oraz `/api/requests/{id}/accept` (akceptacja).

### Sprint 4: Komunikacja w Systemie

**Cel:** Umożliwienie użytkownikom bezpośredniego kontaktu w celu ustalenia szczegółów.

* **Domain (Core):**
* Zbudowanie encji `Message` przechowującej `senderId`, `receiverId`, `content` oraz `timestamp`.

* **Application:**
* Zdefiniowanie `IMessageRepository`.
* Wdrożenie `MessagingService` pozwalającego wysyłać wiadomości i pobierać konwersacje (`sendMessage`, `getConversation`).

* **Infrastructure:**
* Implementacja zapisu wiadomości w bazie danych.

* **Presentation (API):**
* Konfiguracja endpointów `POST api/messages` (wysyłanie) oraz `GET api/messages/{userId}` (pobieranie historii).

### Sprint 5: Zaufanie i Bezpieczeństwo (Oceny i Moderacja)

**Cel:** Wdrożenie systemu recenzji oraz panelu dla administratora.

* **Domain (Core):**
* Utworzenie encji `Review` (`authorId`, `subjectId`, `rating`, `content`).
* Utworzenie encji `Report` dla zgłoszeń naruszeń.

* **Application:**
* Definicja `IReviewRepository` oraz `IReportRepository`.
* Implementacja `ReviewService` , `ReportingService` (`submitReport`) oraz `AdminModerationService` (`banUser`, `resolveReport`).

* **Infrastructure:**
* Dodanie nowych repozytoriów do wstrzykiwania zależności (Dependency Injection).

* **Presentation (API):**
* Podłączenie endpointu `POST api/reports` dla zgłoszeń naruszeń  i endpointów dla dodawania ocen.

### Sprint 6: Integracje Zewnętrzne, Historia i Szlify

**Cel:** Dopracowanie aplikacji, dodanie map, płatności i telemetrii.

* **Domain (Core):**
* Obsługa stanów końcowych obiektów: "Wykonana" oraz "Zarchiwizowana".

* **Application:**
* Zdefiniowanie interfejsów dla serwisów zewnętrznych: `IGeoLocationService`, `IPaymentGateway`.
* Wdrożenie logiki zwracającej historię ukończonych podróży w oparciu o datę przeszłą.

* **Infrastructure:**
* Implementacja `GoogleMapsGeoLocationService` w oparciu o zewnętrzne API Google Maps.
* Implementacja zewnętrznego systemu bankowego do obsługi płatności.

* **Presentation (API / Frontend):**
* Wystawienie danych na zewnątrz i zintegrowanie ich z mapami (Leaflet) na frontendzie w środowisku React.