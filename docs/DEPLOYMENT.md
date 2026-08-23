# GPAHub - Production Deployment (Docker Compose)

## Prerequisites

- Docker Engine 24+ with the Compose plugin
- A Stripe account: publishable/secret key pair from *Developers → API keys* and a webhook endpoint secret created in *Developers → Webhooks* pointing at `https://<your-domain>/api/payments/webhook/stripe` with event `checkout.session.completed`
- A reverse proxy (nginx/Caddy/Traefik) in front of the API for TLS termination - the API container itself listens on plain HTTP :8080

## 1. Configure secrets

Create a `.env` file next to `docker-compose.yml` (**never commit it**):

```dotenv
SQL_SA_PASSWORD=<strong-password, min 12 chars incl. upper/lower/digit/symbol>
JWT_SECRET=<random string, at least 32 characters>
STRIPE_SECRET_KEY=sk_test_...            # sk_live_... in production
STRIPE_WEBHOOK_SECRET=whsec_...
```

Generate a JWT secret quickly:

```bash
openssl rand -base64 48
```

## 2. Start

```bash
docker compose up -d --build
```

On startup the API:

1. waits for SQL Server to become healthy,
2. applies all EF Core migrations,
3. seeds reference data (system default grade scale, Free/Premium plans).

Verify:

```bash
curl http://localhost:8080/health        # → {"status":"Healthy","database":{...}}
```

## 3. Stripe test-mode walkthrough

1. `POST /api/payments/checkout` with a bearer token and body
   `{ "amount": 9.99, "currency": "USD", "externalReference": "manual-ref", "durationDays": 30 }`
   → response contains a Stripe Checkout URL.
2. Complete the payment with a Stripe test card (`4242 4242 4242 4242`).
3. Stripe calls the webhook; the subscription activates automatically.
   Replay protection: unique `ExternalReference` index + idempotent handler.

## 4. Operations notes

| Concern | Handling |
|---------|----------|
| Migrations | Applied automatically on every start (`Database.MigrateAsync`) |
| HTTPS | Terminate TLS at the reverse proxy; keep `ASPNETCORE_ENVIRONMENT=Production` so HSTS + HTTPS redirection stay active |
| Logs | Structured console logs (`docker compose logs -f api`) |
| Backups | Back up the `sqldata` volume; test restores |
| Key rotation | Rotate `JWT_SECRET` to invalidate all sessions; rotate Stripe keys from the dashboard |

## 5. Local production rehearsal

```bash
cp .env.example .env   # fill in values
docker compose up -d --build
curl http://localhost:8080/health
```

Swagger is disabled in Production by design; use the OpenAPI JSON from a Development run or an external client.
