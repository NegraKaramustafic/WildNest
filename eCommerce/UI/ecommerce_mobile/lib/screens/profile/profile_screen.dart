import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/widgets/coming_soon.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        children: [
          const WildNestHeader(),
          const Expanded(
            child: ComingSoonView(
              title: 'Your profile',
              message:
                  'Account settings, badges, and adoption history will appear here later.',
              emoji: '👤',
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(24, 0, 24, 24),
            child: OutlinedButton(
              onPressed: () async {
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
                        child: const Text(
                          'Log out',
                          style: TextStyle(color: AppColors.heart),
                        ),
                      ),
                    ],
                  ),
                );
                if (leave == true && context.mounted) {
                  await context.read<AuthProvider>().logout();
                }
              },
              child: const Text('Log out'),
            ),
          ),
        ],
      ),
    );
  }
}
