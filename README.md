# WildNest

WildNest is a zoo management system for the course Razvoj softvera II. Administrators use the Flutter desktop app. Visitors use the Flutter mobile app. Both talk to an ASP.NET Core API. A separate worker sends notification emails through RabbitMQ.

All money amounts in the apps and in Stripe are in euros (EUR).

## Prerequisites

- Docker Desktop, running
- .NET 9 SDK, only if you run the API outside Docker
- Flutter stable, with Windows desktop enabled for the admin app and an Android emulator or device for the visitor app
- A Stripe test account, only if you want to complete a card payment

## Run the system

From `eCommerce`:

```bash
copy .env.example .env
docker compose up --build
```

The first start applies database migrations and loads the demo data, including images and activity schedules for the next six weeks. Starting the stack again does not insert those rows a second time.

| Service | Address |
| --- | --- |
| API | http://localhost:5126 |
| OpenAPI (development) | http://localhost:5126/swagger |
| Subscriber (worker) | no host port; it reads RabbitMQ and SQL Server on the Docker network |
| SQL Server | `localhost,1435` (database name `230080`) |
| RabbitMQ | `localhost:5672` |
| RabbitMQ management | http://localhost:15672 |
| Mailpit (mailbox) | http://localhost:8025 (SMTP `localhost:1025`) |

RabbitMQ management and the broker use `guest` / `guest` from `.env`. Mailpit needs no sign-in.

## Configuration

Secrets and host settings live in `eCommerce/.env`. That file is not committed. `eCommerce/.env.example` lists every variable the programs read.

Copy the example, then replace the placeholders. Do not put a live Stripe secret in git.

Required:

| Variable | Purpose |
| --- | --- |
| `MSSQL_SA_PASSWORD` | SQL Server `sa` password. The example value is for the local Docker database only. |
| `DB_NAME` | Database name. Use the student index without the `IB` prefix (`230080`). |
| `ConnectionStrings__DefaultConnection` | Used by `dotnet run` on the host. Docker replaces the server with `sql`. |
| `JwtToken__SecretKey` | JWT signing key, at least 32 characters. |
| `JwtToken__Issuer` | JWT issuer. |
| `JwtToken__Audience` | JWT audience. |
| `JwtToken__DurationInMinutes` | Access token lifetime. |
| `RABBITMQ_HOSTNAME` | `localhost` on the host, `rabbitmq` inside Docker. |
| `RABBITMQ_PORT` | `5672`. |
| `RABBITMQ_USERNAME` | RabbitMQ user. |
| `RABBITMQ_PASSWORD` | RabbitMQ password. |
| `RABBITMQ_QUEUENAME` | `wildnest.notifications`. |
| `STRIPE_SECRET_KEY` | Stripe test secret (`sk_test_...`). |
| `STRIPE_WEBHOOK_SECRET` | Signing secret from `stripe listen` (`whsec_...`). |
| `STRIPE_CURRENCY` | `eur`. |
| `STRIPE_SUCCESS_URL` | Mobile return link. Keep the `{CHECKOUT_SESSION_ID}` placeholder. |
| `STRIPE_CANCEL_URL` | Mobile cancel link. |
| `CORS_ALLOWED_ORIGINS` | Browser origins. The Flutter apps are not subject to CORS. |
| `ZOO_TIME_ZONE` | IANA id used for "today". Example: `Europe/Sarajevo`. |

Optional, with the default used when the variable is omitted:

| Variable | Default |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` in the example file |
| `SMTP_HOST` | empty (the subscriber logs mail instead of sending it). Docker sets `mailpit`. |
| `SMTP_PORT` | `587` in the example. Docker sets `1025`. |
| `SMTP_USERNAME` | empty |
| `SMTP_PASSWORD` | empty |
| `SMTP_FROM` | empty. Docker sets `noreply@wildnest.local`. |
| `SMTP_ENABLE_SSL` | `true` in the example. Docker sets `false` for Mailpit. |
| `PASSWORD_RESET_CODE_LIFETIME_MINUTES` | `15` |
| `PASSWORD_RESET_COOLDOWN_SECONDS` | `60` |
| `PASSWORD_RESET_MAX_ATTEMPTS` | `5` |
| `AUTH_RATE_LIMIT_WINDOW_SECONDS` | `60` |
| `AUTH_RATE_LIMIT_LOGIN_PER_IP` | `30` |
| `AUTH_RATE_LIMIT_LOGIN_PER_ACCOUNT` | `10` |
| `AUTH_RATE_LIMIT_REFRESH_PER_IP` | `60` |
| `AUTH_RATE_LIMIT_REFRESH_PER_ACCOUNT` | `20` |
| `AUTH_RATE_LIMIT_FORGOT_PASSWORD_PER_IP` | `10` |
| `AUTH_RATE_LIMIT_FORGOT_PASSWORD_PER_ACCOUNT` | `5` |
| `AUTH_RATE_LIMIT_RESET_PASSWORD_PER_IP` | `20` |
| `AUTH_RATE_LIMIT_RESET_PASSWORD_PER_ACCOUNT` | `10` |
| `REMINDER_HOURS_BEFORE` | `24` |
| `REMINDER_CHECK_INTERVAL_MINUTES` | `15` |
| `SENTIMENT_MODEL_PATH` | `ml-models/review-sentiment.zip` next to the API |
| `RECOMMENDATION_RETRAIN_INTERVAL_MINUTES` | `360` |
| `RECOMMENDATION_RETRAIN_AFTER_NEW_RESERVATIONS` | `10` |
| `RECOMMENDATION_TRAINING_CHECK_MINUTES` | `5` |

## Sign-in

The desktop app accepts the Admin role. The mobile visitor app accepts the Customer role. An Admin signed in on the phone gets the ticket-check screen.

The course password `test` is shorter than the registration rule (at least 6 characters). These accounts are inserted by the seeder. Registering a new account with `test` is rejected. The other seeded accounts use `Test123`, which does satisfy the rule.

| Context | Username | Password | Role |
| --- | --- | --- | --- |
| Desktop version | `desktop` | `test` | Admin |
| Mobile version | `mobile` | `test` | Customer |
| Role account | `Admin` | `test` | Admin |
| Role account | `Customer` | `test` | Customer |
| Extra administrator | `admin1` | `Test123` | Admin |
| Extra administrator | `admin2` | `Test123` | Admin |
| Extra administrator | `admin3` | `Test123` | Admin |
| Extra visitor | `customer1` | `Test123` | Customer |
| Extra visitor | `customer2` | `Test123` | Customer |

`Customer` has no reservation history, so the home screen shows popular activities. `mobile` and `customer1` already have reservations, so they can show a personalised list after the model has trained.

## Desktop app

From `eCommerce/UI/ecommerce_desktop`, with the API already running:

```bash
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5126
```

Release build:

```bash
flutter build windows --release --dart-define=API_BASE_URL=http://localhost:5126
```

The program is `build/windows/x64/runner/Release/`.

## Mobile app

`API_BASE_URL` is required. The app does not have a default host.

Android emulator (the emulator address for this PC):

```bash
cd eCommerce/UI/ecommerce_mobile
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5126
```

Release APK for the emulator:

```bash
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5126
```

The package is `build/app/outputs/flutter-apk/app-release.apk`.

A physical phone must use the PC's LAN address, not `localhost` and not `10.0.2.2`:

```bash
flutter run --dart-define=API_BASE_URL=http://192.168.1.20:5126
```

Replace `192.168.1.20` with the IPv4 address from `ipconfig`. The phone and the PC must be on the same network. Windows Firewall must allow inbound TCP on port 5126 for the Docker API, otherwise the phone cannot connect.

## Stripe test payments

The API charges in euros. Use test keys only.

In a second terminal, while the API is on port 5126:

```bash
stripe listen --forward-to http://localhost:5126/api/Payment/webhook
```

Copy the `whsec_...` value into `STRIPE_WEBHOOK_SECRET` in `eCommerce/.env`, then recreate the API container (`docker compose up --build -d` from `eCommerce`) so it picks up the new secret.

Test cards (any future expiry, any CVC, any postal code):

| Number | Result |
| --- | --- |
| `4242 4242 4242 4242` | Payment succeeds |
| `4000 0000 0000 0002` | Card is declined |
| `4000 0025 0000 3156` | Authentication is required |

## Recommendation and sentiment notes

See `recommender-dokumentacija.md`.

## Troubleshooting

**Port 5126 is already in use.** The Docker `api` container is already publishing that port. Stop it before `dotnet run`, or use the container that is already running. Check with `docker ps`.

**Windows long path limit.** Flutter Windows builds fail when the project path is too long. Keep the checkout at a short path such as `C:\dev\wildnest`.

**Docker is out of disk space.** `docker compose up --build` fails while extracting images or copying build output. Free space in Docker Desktop. Do not delete the WildNest database volume if you still need that data.

**The schedule calendar looks empty.** It opens on the current week. A database created before the relative schedule seeder existed only has older fixed dates. Restart the API so the seeder can add the coming weeks, or create a new database.

**Mail did not arrive.** Open http://localhost:8025. Password reset and reminders are delivered there when the stack was started with Docker.
