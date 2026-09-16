import 'package:ecommerce_mobile/screens/adopt/adopt_screen.dart';
import 'package:ecommerce_mobile/screens/animals/animals_list_screen.dart';
import 'package:ecommerce_mobile/screens/home/home_screen.dart';
import 'package:ecommerce_mobile/screens/profile/profile_screen.dart';
import 'package:ecommerce_mobile/providers/notification_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class MainShell extends StatefulWidget {
  const MainShell({super.key});

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _index = 0;
  final _homeKey = GlobalKey<NavigatorState>();
  final _animalsKey = GlobalKey<NavigatorState>();
  final _adoptKey = GlobalKey<NavigatorState>();
  final _profileKey = GlobalKey<NavigatorState>();
  NotificationProvider? _notifications;

  List<GlobalKey<NavigatorState>> get _keys =>
      [_homeKey, _animalsKey, _adoptKey, _profileKey];

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _notifications ??= context.read<NotificationProvider>()..startPolling();
  }

  @override
  void dispose() {
    _notifications?.stopPolling();
    super.dispose();
  }

  void _openTab(int index) {
    if (index == _index) {
      _keys[index].currentState?.popUntil((route) => route.isFirst);
      return;
    }
    setState(() => _index = index);
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (didPop) return;
        final nav = _keys[_index].currentState;
        if (nav?.canPop() ?? false) {
          nav!.pop();
        }
      },
      child: Scaffold(
        body: IndexedStack(
          index: _index,
          children: [
            _TabNavigator(
              navigatorKey: _homeKey,
              child: HomeScreen(onOpenTab: _openTab),
            ),
            _TabNavigator(
              navigatorKey: _animalsKey,
              child: const AnimalsListScreen(),
            ),
            _TabNavigator(
              navigatorKey: _adoptKey,
              child: const AdoptScreen(),
            ),
            _TabNavigator(
              navigatorKey: _profileKey,
              child: const ProfileScreen(),
            ),
          ],
        ),
        bottomNavigationBar: NavigationBarTheme(
          data: NavigationBarThemeData(
            backgroundColor: AppColors.background,
            indicatorColor: Colors.transparent,
            overlayColor: const WidgetStatePropertyAll(Colors.transparent),
            labelTextStyle: WidgetStateProperty.resolveWith((states) {
              final selected = states.contains(WidgetState.selected);
              return TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: selected ? AppColors.sage : AppColors.navUnselected,
              );
            }),
          ),
          child: NavigationBar(
            selectedIndex: _index,
            onDestinationSelected: _openTab,
            backgroundColor: AppColors.background,
            indicatorColor: Colors.transparent,
            height: 72,
            labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.home_outlined, color: AppColors.navUnselected),
                selectedIcon: Icon(Icons.home_rounded, color: AppColors.sage),
                label: 'Home',
              ),
              NavigationDestination(
                icon: Icon(Icons.pets_outlined, color: AppColors.navUnselected),
                selectedIcon: Icon(Icons.pets, color: AppColors.sage),
                label: 'Animals',
              ),
              NavigationDestination(
                icon: Icon(Icons.favorite_border, color: AppColors.navUnselected),
                selectedIcon: Icon(Icons.favorite, color: AppColors.sage),
                label: 'Adopt',
              ),
              NavigationDestination(
                icon: Icon(Icons.person_outline, color: AppColors.navUnselected),
                selectedIcon: Icon(Icons.person, color: AppColors.sage),
                label: 'Profile',
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _TabNavigator extends StatelessWidget {
  const _TabNavigator({required this.navigatorKey, required this.child});

  final GlobalKey<NavigatorState> navigatorKey;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Navigator(
      key: navigatorKey,
      onGenerateRoute: (settings) {
        return MaterialPageRoute(
          settings: settings,
          builder: (_) => child,
        );
      },
    );
  }
}
