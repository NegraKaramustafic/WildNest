import 'package:ecommerce_mobile/screens/adopt/adopt_screen.dart';
import 'package:ecommerce_mobile/screens/animals/animals_list_screen.dart';
import 'package:ecommerce_mobile/screens/home/home_screen.dart';
import 'package:ecommerce_mobile/screens/profile/profile_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:flutter/material.dart';

class MainShell extends StatefulWidget {
  const MainShell({super.key});

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _index = 0;
  final _animalsNavigatorKey = GlobalKey<NavigatorState>();

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (didPop) return;
        if (_index == 1 &&
            (_animalsNavigatorKey.currentState?.canPop() ?? false)) {
          _animalsNavigatorKey.currentState!.pop();
          return;
        }
      },
      child: Scaffold(
        body: IndexedStack(
          index: _index,
          children: [
            HomeScreen(onOpenTab: (i) => setState(() => _index = i)),
            Navigator(
              key: _animalsNavigatorKey,
              onGenerateRoute: (settings) {
                return MaterialPageRoute(
                  builder: (_) => const AnimalsListScreen(),
                );
              },
            ),
            const AdoptScreen(),
            const ProfileScreen(),
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
            onDestinationSelected: (i) => setState(() => _index = i),
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
