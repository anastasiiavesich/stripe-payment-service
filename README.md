# Stripe Checkout Payment Service

Minimal Stripe payment backend built with ASP.NET Core.

The service:
- Creates Stripe Checkout Sessions
- Stores internal payment state
- Processes Stripe webhooks
- Verifies webhook signatures
- Enforces idempotency at database level

Stripe is treated as an external processor.  
Application database is the source of truth.

---

## 1. Requirements

- .NET 10
- Stripe account (Test mode)
- Stripe CLI installed

Verify installations:

```bash
dotnet --version
stripe --version
```

---

## 2. Stripe Setup (Test Mode)

### 2.1 Login to Stripe via CLI

```bash
stripe login
```

A browser window will open. Authorize access to your Stripe account.

---

### 2.2 Start listening for webhooks

Run:

```bash
stripe listen --forward-to localhost:5200/api/webhook
```

You will see output similar to:

```
Ready! Your webhook signing secret is:

whsec_xxxxxxxxxxxxxxxxx
```

Copy this `whsec_...` value.

⚠️ Keep this terminal running.  
If you stop it and restart, a new webhook secret will be generated.

---

## 3. Set Environment Variables

In a new terminal (or same one before running the app):

```bash
export STRIPE_SECRET_KEY=sk_test_...
export STRIPE_WEBHOOK_SECRET=whsec_xxxxxxxxxxxxxxxxx
```

Replace values with your actual keys from Stripe (Test mode).

You can verify:

```bash
echo $STRIPE_SECRET_KEY
echo $STRIPE_WEBHOOK_SECRET
```

---

## 4. Run the Application

From project root:

```bash
dotnet run
```

Default URL:

```
http://localhost:5200
```

The application will automatically apply database migrations on startup.

---

## 5. Open Frontend

In browser:

```
http://localhost:5200/index.html
```

Click the payment button to create a Checkout Session.

---

## 6. Complete Test Payment

Use Stripe test card (3D Secure):

```
4000 0025 0000 3155
```

Use any future expiration date and any CVC.

After completing 3DS authentication:

- Stripe redirects to success page
- Webhook is triggered
- PaymentRecord is updated in database

You should see webhook logs in the Stripe CLI terminal.

---

## 7. Payment Flow

1. `POST /api/checkout/create-session`
2. Redirect to Stripe Checkout
3. Stripe sends webhook → `/api/webhook`
4. Webhook signature is verified
5. Event idempotency is checked
6. PaymentRecord updated in SQLite database

---

## 8. Running Integration Tests

The solution includes a full integration test suite.

Covered scenarios:

- Checkout session creation
- Pending → Succeeded transition
- Pending → Canceled transition
- Webhook idempotency (same event twice)
- Duplicate PaymentIntent with different event IDs
- Invalid signature handling
- Unknown event types
- Non-existing PaymentIntent safety
- CompletedAtUtc audit validation

Run tests:

```bash
dotnet test
```

Tests use:

- SQLite in-memory database
- Fake Stripe service (no real API calls)
- WebApplicationFactory for full pipeline execution

No real Stripe account is required to run tests.

---

## 9. Webhook Idempotency Strategy

To prevent duplicate processing:

- Each Stripe event ID is stored in `ProcessedEvents` table.
- Before processing, event ID is checked.
- If already processed → return `200 OK` without reprocessing.
- Event ID is stored only after successful business handling.

This guarantees safe retry behavior.

---

## 10. Status Transitions

Valid transitions:

- Pending → Succeeded
- Pending → Canceled

Invalid transitions are ignored.

`CompletedAtUtc` is set only when status becomes `Succeeded`.

---

## 11. Error Handling Strategy

| Scenario | Result |
|----------|--------|
| Invalid webhook signature | 400 BadRequest |
| Unknown event type | 200 OK |
| Duplicate event ID | 200 OK |
| Non-existing PaymentIntent | 200 OK |
| Business exception | 500 InternalServerError |

---

## 12. Security Notes

- Webhook signature verification is enforced.
- Stripe keys are loaded from environment variables.
- Secrets are never committed.
- Idempotency prevents replay attacks.
- Stripe API is abstracted via `IStripeService`.

---

## 13. Development Notes

- Database recreated automatically in integration tests.
- FakeStripeService simulates Stripe behavior deterministically.
- Tests can run in parallel safely.

