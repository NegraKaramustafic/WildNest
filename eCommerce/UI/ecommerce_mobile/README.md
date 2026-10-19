# WildNest mobile

Visitor app for WildNest. Sign in with a Customer account. An Admin account opens ticket check.

Start the API first. The steps, ports, and sign-in table are in the repository root `README.md`.

From this folder, Android emulator:

```bash
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5126
```

A physical phone needs the PC's LAN address, for example `http://192.168.1.20:5126`, and Windows Firewall must allow inbound TCP on port 5126.

`API_BASE_URL` is required. A trailing slash is optional.
