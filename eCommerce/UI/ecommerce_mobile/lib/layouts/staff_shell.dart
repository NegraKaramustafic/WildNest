import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/screens/staff/enter_code_screen.dart';
import 'package:ecommerce_mobile/screens/staff/scan_ticket_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class StaffShell extends StatefulWidget {
  const StaffShell({super.key});

  @override
  State<StaffShell> createState() => _StaffShellState();
}

class _StaffShellState extends State<StaffShell> {
  int _index = 0;

  Future<void> _logout() async {
    final leave = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Log out'),
        content: const Text('Are you sure you want to log out?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Log out', style: TextStyle(color: AppColors.heart)),
          ),
        ],
      ),
    );
    if (leave == true && mounted) {
      await context.read<AuthProvider>().logout();
    }
  }

  @override
  Widget build(BuildContext context) {
    final name = context.watch<AuthProvider>().displayName ?? 'Staff';
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            const WildNestHeader(showNotifications: false),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 0, 8, 8),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Entrance staff',
                          style: TextStyle(color: AppColors.textMuted, fontSize: 13),
                        ),
                        Text(
                          name,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
                  TextButton.icon(
                    onPressed: _logout,
                    icon: const Icon(Icons.logout_rounded, color: AppColors.heart, size: 18),
                    label: const Text(
                      'Log out',
                      style: TextStyle(color: AppColors.heart, fontWeight: FontWeight.w700),
                    ),
                  ),
                ],
              ),
            ),
            Expanded(
              child: IndexedStack(
                index: _index,
                children: [
                  ScanTicketScreen(
                    isActive: _index == 0,
                    onEnterCode: () => setState(() => _index = 1),
                  ),
                  const EnterCodeScreen(),
                ],
              ),
            ),
          ],
        ),
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
          onDestinationSelected: (index) => setState(() => _index = index),
          backgroundColor: AppColors.background,
          indicatorColor: Colors.transparent,
          height: 72,
          labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.qr_code_scanner, color: AppColors.navUnselected),
              selectedIcon: Icon(Icons.qr_code_scanner, color: AppColors.sage),
              label: 'Scan ticket',
            ),
            NavigationDestination(
              icon: Icon(Icons.keyboard_alt_outlined, color: AppColors.navUnselected),
              selectedIcon: Icon(Icons.keyboard_alt_outlined, color: AppColors.sage),
              label: 'Enter code',
            ),
          ],
        ),
      ),
    );
  }
}
