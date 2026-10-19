# WildNest desktop

Administrator app for WildNest. Sign in with an Admin account.

Start the API first. The steps, ports, and sign-in table are in the repository root `README.md`.

From this folder:

```bash
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5126
```

`API_BASE_URL` is required. A trailing slash is optional.
