import 'package:ecommerce_mobile/providers/notification_provider.dart';
import 'package:ecommerce_mobile/screens/notifications/notifications_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class WildNestHeader extends StatelessWidget {
  const WildNestHeader({
    super.key,
    this.showBack = false,
    this.onBack,
    this.showNotifications = true,
  });

  final bool showBack;
  final VoidCallback? onBack;
  final bool showNotifications;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
      child: Row(
        children: [
          if (showBack) ...[
            _RoundIconButton(
              icon: Icons.arrow_back_ios_new_rounded,
              onTap: onBack ?? () => Navigator.of(context).maybePop(),
            ),
            const SizedBox(width: 8),
          ],
          Text(
            'WildNest',
            style: GoogleFonts.playfairDisplay(
              fontSize: 28,
              fontWeight: FontWeight.w600,
              fontStyle: FontStyle.italic,
              color: AppColors.sage,
            ),
          ),
          const Spacer(),
          if (showNotifications)
            Consumer<NotificationProvider>(
              builder: (context, notifications, _) {
                return InkWell(
                  onTap: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const NotificationsScreen(),
                      ),
                    );
                  },
                  borderRadius: BorderRadius.circular(21),
                  child: Stack(
                    clipBehavior: Clip.none,
                    children: [
                      Container(
                        width: 42,
                        height: 42,
                        decoration: const BoxDecoration(
                          color: AppColors.surface,
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(
                          Icons.notifications_none_rounded,
                          color: AppColors.sage,
                        ),
                      ),
                      if (notifications.hasUnread)
                        Positioned(
                          right: 6,
                          top: 6,
                          child: Container(
                            width: 9,
                            height: 9,
                            decoration: const BoxDecoration(
                              color: AppColors.orange,
                              shape: BoxShape.circle,
                            ),
                          ),
                        ),
                    ],
                  ),
                );
              },
            ),
        ],
      ),
    );
  }
}

class _RoundIconButton extends StatelessWidget {
  const _RoundIconButton({required this.icon, required this.onTap});

  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: Container(
        width: 40,
        height: 40,
        decoration: const BoxDecoration(
          color: AppColors.surface,
          shape: BoxShape.circle,
        ),
        child: Icon(icon, size: 18, color: AppColors.text),
      ),
    );
  }
}
