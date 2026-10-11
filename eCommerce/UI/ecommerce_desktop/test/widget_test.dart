import 'package:ecommerce_desktop/layouts/admin_shell.dart';
import 'package:ecommerce_desktop/main.dart';
import 'package:ecommerce_desktop/models/ticket.dart';
import 'package:ecommerce_desktop/providers/animal_category_provider.dart';
import 'package:ecommerce_desktop/providers/animal_provider.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/ticket_provider.dart';
import 'package:ecommerce_desktop/screens/login_screen.dart';
import 'package:ecommerce_desktop/screens/tickets/ticket_verification_screen.dart';
import 'package:ecommerce_desktop/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    FlutterSecureStorage.setMockInitialValues({});
    AuthProvider.debugReset();
  });

  test('only the Admin role is accepted', () {
    expect(AuthProvider.isAdminRole({'Role': 'Admin'}), isTrue);
    expect(AuthProvider.isAdminRole({'role': 'Admin'}), isTrue);
    expect(AuthProvider.isAdminRole({'Role': 'User'}), isFalse);
    expect(AuthProvider.isAdminRole(null), isFalse);
  });

  testWidgets('login requires a username and password', (tester) async {
    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (_) => AuthProvider(),
        child: MaterialApp(theme: AppTheme.light, home: const LoginScreen()),
      ),
    );

    await tester.tap(find.text('Prijava'));
    await tester.pump();

    expect(find.text('Unesite korisničko ime i lozinku.'), findsOneWidget);
  });

  testWidgets('shell opens placeholders and ticket verification', (tester) async {
    tester.view.physicalSize = const Size(1440, 900);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider(create: (_) => AuthProvider()),
          ChangeNotifierProvider(create: (_) => AnimalProvider()),
          ChangeNotifierProvider(create: (_) => AnimalCategoryProvider()),
          ChangeNotifierProvider(create: (_) => TicketProvider()),
        ],
        child: MaterialApp(theme: AppTheme.light, home: const AdminShell()),
      ),
    );

    expect(find.text('Ovaj ekran dolazi u narednoj sesiji.'), findsOneWidget);

    await tester.tap(find.text('Verifikacija ulaznica'));
    await tester.pump();

    expect(find.text('Unesite kod ulaznice za provjeru valjanosti'), findsOneWidget);
    expect(find.textContaining('Demo kodovi'), findsNothing);
    expect(find.text('npr. WN-A1B2C3'), findsOneWidget);

    await tester.tap(find.text('Verificiraj'));
    await tester.pump();
    expect(find.text('Unesite kod ulaznice.'), findsOneWidget);

    await tester.tap(find.text('Donacije'));
    await tester.pump();
    expect(find.text('Donacije'), findsWidgets);
    expect(find.text('Ovaj ekran dolazi u narednoj sesiji.'), findsOneWidget);

    await tester.tap(find.text('Životinje'));
    await tester.pump();
    await tester.pump();
    expect(find.textContaining('API_BASE_URL'), findsOneWidget);
    expect(find.text('Upravljanje životinjama'), findsOneWidget);
  });

  testWidgets('ticket result shows details and the today warning', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: TicketVerifyResultView(
            ticket: Ticket(
              id: 4,
              userId: 2,
              userFullName: 'Ana Horvat',
              visitDate: DateTime(2026, 4, 2),
              visitorCount: 2,
              totalPrice: 20,
              code: 'WN-2026-004',
              status: TicketStatus.used,
              warning: 'Ticket is not valid for today',
            ),
          ),
        ),
      ),
    );

    expect(find.text('Ulaznica je uspješno verificirana'), findsOneWidget);
    expect(find.text('Ticket is not valid for today'), findsOneWidget);
    expect(find.text('WN-2026-004'), findsOneWidget);
    expect(find.text('Ana Horvat'), findsOneWidget);
  });

  testWidgets('ticket result shows the API error message', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: TicketVerifyResultView(error: 'Ticket already used'),
        ),
      ),
    );

    expect(find.text('Ticket already used'), findsOneWidget);
    expect(find.text('Ulaznica nije verificirana'), findsOneWidget);
  });

  testWidgets('app starts on the login screen', (tester) async {
    await tester.pumpWidget(const WildNestApp());
    await tester.pump();
    await tester.pump();

    expect(find.text('Prijava'), findsOneWidget);
    expect(find.text('WildNest'), findsOneWidget);
  });
}
