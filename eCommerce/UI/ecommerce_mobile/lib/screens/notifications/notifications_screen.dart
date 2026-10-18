import 'package:ecommerce_mobile/models/app_notification.dart';
import 'package:ecommerce_mobile/providers/notification_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/widgets/async_body.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            WildNestHeader(
              showBack: true,
              showNotifications: false,
              onBack: () => Navigator.pop(context),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 16),
              child: Text(
                'Notifications',
                style: GoogleFonts.playfairDisplay(
                  fontSize: 32,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            Expanded(
              child: Consumer<NotificationProvider>(
                builder: (context, provider, _) {
                  return AsyncBody(
                    loading: provider.loading && provider.items.isEmpty,
                    error: provider.items.isEmpty ? provider.error : null,
                    onRetry: provider.refresh,
                    isEmpty: provider.items.isEmpty,
                    emptyMessage: 'No notifications yet.',
                    child: ListView.separated(
                      padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
                      itemCount: provider.items.length,
                      separatorBuilder: (_, _) => const SizedBox(height: 10),
                      itemBuilder: (context, index) {
                        final item = provider.items[index];
                        return _NotificationCard(
                          notification: item,
                          onTap: () {
                            if (!item.isRead) {
                              provider.markRead(item.id);
                            }
                          },
                        );
                      },
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _NotificationCard extends StatelessWidget {
  const _NotificationCard({
    required this.notification,
    required this.onTap,
  });

  final AppNotification notification;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final unread = !notification.isRead;
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(22),
      child: Ink(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: unread ? AppColors.mint.withValues(alpha: 0.55) : AppColors.surface,
          borderRadius: BorderRadius.circular(22),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 42,
              height: 42,
              decoration: BoxDecoration(
                color: AppColors.mint,
                borderRadius: BorderRadius.circular(14),
              ),
              child: Icon(_iconFor(notification.type), color: AppColors.sage, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    notification.title,
                    style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    notification.message,
                    style: const TextStyle(color: AppColors.textMuted, height: 1.35),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    formatRelativeTime(notification.createdAt),
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                  ),
                ],
              ),
            ),
            if (unread)
              Container(
                width: 8,
                height: 8,
                margin: const EdgeInsets.only(top: 6, left: 8),
                decoration: const BoxDecoration(
                  color: AppColors.sage,
                  shape: BoxShape.circle,
                ),
              ),
          ],
        ),
      ),
    );
  }
}

IconData _iconFor(NotificationType type) {
  switch (type) {
    case NotificationType.ticketPurchased:
    case NotificationType.ticketVerified:
      return Icons.confirmation_number_outlined;
    case NotificationType.reservationCreated:
    case NotificationType.reservationConfirmed:
      return Icons.check_circle_outline;
    case NotificationType.reservationCancelled:
      return Icons.event_busy_outlined;
    case NotificationType.adoptionCreated:
      return Icons.favorite_border;
    case NotificationType.donationReceived:
    case NotificationType.paymentCompleted:
      return Icons.payments_outlined;
    case NotificationType.badgeEarned:
      return Icons.workspace_premium_outlined;
    case NotificationType.passwordReset:
      return Icons.lock_reset;
    case NotificationType.upcomingReminder:
      return Icons.event_outlined;
  }
}
