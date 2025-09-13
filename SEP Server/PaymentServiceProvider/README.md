# Payment Service Provider (PSP) System

## Pregled

PSP sistem omogućava web shop vlasnicima da se registruju i biraju načine plaćanja koje žele da ponude svojim kupcima. Sistem je dizajniran da se lako proširuje novim načinima plaćanja.

## Funkcionalnosti

### 1. Admin Panel
- **Dashboard**: Pregled statistika sistema
- **Merchant Management**: Upravljanje web shop vlasnicima
- **Payment Methods Management**: Upravljanje načinima plaćanja
- **Transaction History**: Pregled svih transakcija

### 2. Merchant Registration
- Registracija novih web shop vlasnika
- Generisanje API ključeva i webhook secret-a
- Upravljanje statusom merchant-a (Active/Inactive/Suspended)

### 3. Payment Methods Management
- Dodavanje novih načina plaćanja (Payoneer, kriptovalute, itd.)
- Konfiguracija načina plaćanja
- Omogućavanje/onemogućavanje načina plaćanja

### 4. Payment Selection Flow
- Kupac bira način plaćanja na PSP-u
- Validacija dostupnih načina plaćanja za merchant-a
- Preusmeravanje na odgovarajući payment servis

## API Endpoints

### Admin Endpoints (`/api/admin`)

#### Merchants
- `GET /merchants` - Lista svih merchant-a
- `POST /merchants` - Kreiranje novog merchant-a
- `GET /merchants/{id}` - Detalji merchant-a
- `PUT /merchants/{id}` - Ažuriranje merchant-a
- `DELETE /merchants/{id}` - Brisanje merchant-a

#### Payment Methods
- `GET /payment-methods` - Lista svih načina plaćanja
- `POST /payment-methods` - Kreiranje novog načina plaćanja
- `GET /payment-methods/{id}` - Detalji načina plaćanja
- `PUT /payment-methods/{id}` - Ažuriranje načina plaćanja
- `DELETE /payment-methods/{id}` - Brisanje načina plaćanja

#### Merchant Payment Methods
- `GET /merchants/{merchantId}/payment-methods` - Načini plaćanja za merchant-a
- `POST /merchants/{merchantId}/payment-methods` - Dodavanje načina plaćanja merchant-u
- `DELETE /merchants/{merchantId}/payment-methods/{paymentTypeId}` - Uklanjanje načina plaćanja

#### Statistics
- `GET /statistics` - Statistike sistema

### Payment Selection Endpoints (`/api/payment-selection`)

- `GET /{pspTransactionId}` - Stranica za izbor načina plaćanja
- `POST /{pspTransactionId}/select` - Procesiranje izabranog načina plaćanja
- `GET /merchant/{merchantId}/payment-methods` - Dostupni načini plaćanja za merchant-a
- `GET /payment-methods/{paymentType}` - Detalji načina plaćanja

### PSP Endpoints (`/api/psp`)

- `POST /payment/create` - Kreiranje payment request-a
- `POST /payment/{pspTransactionId}/process` - Procesiranje plaćanja
- `GET /payment/{pspTransactionId}/status` - Status plaćanja
- `GET /payment-methods` - Dostupni načini plaćanja
- `POST /callback` - Webhook callback
- `POST /payment/{pspTransactionId}/refund` - Refund plaćanja

## Korišćenje

### 1. Pokretanje Admin Panel-a

```
http://localhost:5000/
```

### 2. Kreiranje Merchant-a

```bash
curl -X POST http://localhost:5000/api/admin/merchants \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Telekom Srbija",
    "description": "Telecom services provider",
    "merchantId": "telekom_001",
    "merchantPassword": "secure_password_123",
    "baseUrl": "https://telekom.rs"
  }'
```

### 3. Dodavanje Načina Plaćanja

```bash
curl -X POST http://localhost:5000/api/admin/payment-methods \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Payoneer",
    "type": "payoneer",
    "description": "Payoneer payment method",
    "isEnabled": true,
    "configuration": "{\"apiKey\": \"your-api-key\", \"endpoint\": \"https://api.payoneer.com\"}"
  }'
```

### 4. Povezivanje Načina Plaćanja sa Merchant-om

```bash
curl -X POST http://localhost:5000/api/admin/merchants/1/payment-methods \
  -H "Content-Type: application/json" \
  -d '{
    "paymentTypeId": 1
  }'
```

### 5. Kreiranje Payment Request-a

```bash
curl -X POST http://localhost:5000/api/psp/payment/create \
  -H "Content-Type: application/json" \
  -d '{
    "merchantId": "telekom_001",
    "merchantPassword": "secure_password_123",
    "amount": 49.99,
    "currency": "RSD",
    "merchantOrderID": "12345",
    "description": "Telekom Premium Package",
    "returnURL": "https://telekom.rs/payment/success",
    "cancelURL": "https://telekom.rs/payment/cancel",
    "callbackURL": "https://telekom.rs/webhook/payment"
  }'
```

### 6. Izbor Načina Plaćanja

```bash
curl -X GET http://localhost:5000/api/payment-selection/{pspTransactionId}
```

### 7. Procesiranje Plaćanja

```bash
curl -X POST http://localhost:5000/api/payment-selection/{pspTransactionId}/select \
  -H "Content-Type: application/json" \
  -d '{
    "paymentType": "card"
  }'
```

## Dodavanje Novih Načina Plaćanja

### 1. Kreiranje Payment Plugin-a

```csharp
public class PayoneerPaymentPlugin : IPaymentPlugin
{
    public string Name => "Payoneer";
    public string Type => "payoneer";
    public bool IsEnabled => true;

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, Transaction transaction)
    {
        // Implementacija Payoneer integracije
        return new PaymentResponse
        {
            Success = true,
            PaymentUrl = "https://payoneer.com/pay/...",
            PSPTransactionId = transaction.PSPTransactionId
        };
    }
}
```

### 2. Registracija Plugin-a

```csharp
// U Program.cs
builder.Services.AddScoped<IPaymentPlugin, PayoneerPaymentPlugin>();
```

### 3. Dodavanje u Admin Panel

Koristite admin panel da dodate novi način plaćanja sa tipom "payoneer".

## Flow Plaćanja

1. **Web Shop** → Kreira payment request na PSP-u
2. **PSP** → Generiše PSP transaction ID
3. **Kupac** → Preusmeren na PSP payment selection stranicu
4. **Kupac** → Biram način plaćanja
5. **PSP** → Procesira plaćanje kroz odgovarajući plugin
6. **Payment Service** → Procesira plaćanje
7. **PSP** → Callback sa rezultatom
8. **Web Shop** → Webhook sa rezultatom

## Konfiguracija

### Database Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PSPDb;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

## Sigurnost

- API ključevi se generišu automatski
- Webhook secret se generiše automatski
- Merchant password se hash-uje
- HTTPS je obavezan za produkciju

## Monitoring

- Sve transakcije se loguju
- Statistike su dostupne u admin panel-u
- Webhook callback-ovi se prate
- Error handling je implementiran

## Proširivanje

Sistem je dizajniran da se lako proširuje:

1. **Novi Payment Plugin**: Implementirajte `IPaymentPlugin` interface
2. **Nova Validacija**: Dodajte u `PaymentPluginManager`
3. **Nova Konfiguracija**: Dodajte u `PaymentType.Configuration`
4. **Novi Endpoint**: Dodajte u odgovarajući kontroler

## Troubleshooting

### Česti Problemi

1. **Plugin se ne registruje**: Proverite da li je dodan u `Program.cs`
2. **Merchant ne može da se autentifikuje**: Proverite `MerchantId` i `MerchantPassword`
3. **Payment method nije dostupan**: Proverite da li je omogućen za merchant-a
4. **Callback ne radi**: Proverite webhook URL i secret

### Logovi

```bash
# Console logovi
dotnet run --environment Development

# Database logovi
# Proverite Transaction tabelu za detalje
```
