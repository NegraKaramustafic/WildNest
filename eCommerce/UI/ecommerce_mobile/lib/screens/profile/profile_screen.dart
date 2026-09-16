import 'dart:convert';
import 'dart:typed_data';

import 'package:ecommerce_mobile/models/adoption.dart';
import 'package:ecommerce_mobile/models/donation.dart';
import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/models/user.dart';
import 'package:ecommerce_mobile/models/user_badge.dart';
import 'package:ecommerce_mobile/providers/adoption_provider.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/donation_provider.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/providers/ticket_provider.dart';
import 'package:ecommerce_mobile/providers/user_badge_provider.dart';
import 'package:ecommerce_mobile/providers/user_provider.dart';
import 'package:ecommerce_mobile/screens/profile/donation_history_screen.dart';
import 'package:ecommerce_mobile/screens/profile/my_adoptions_screen.dart';
import 'package:ecommerce_mobile/screens/profile/my_badges_screen.dart';
import 'package:ecommerce_mobile/screens/profile/my_reservations_screen.dart';
import 'package:ecommerce_mobile/screens/profile/settings_screen.dart';
import 'package:ecommerce_mobile/screens/tickets/my_tickets_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/status_chip.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  User? _user;
  List<Reservation> _reservations = [];
  List<Ticket> _tickets = [];
  List<Adoption> _adoptions = [];
  List<Donation> _donations = [];
  List<UserBadge> _badges = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final userId = context.read<AuthProvider>().currentUserId;
    final userProvider = context.read<UserProvider>();
    final reservationProvider = context.read<ReservationProvider>();
    final ticketProvider = context.read<TicketProvider>();
    final adoptionProvider = context.read<AdoptionProvider>();
    final donationProvider = context.read<DonationProvider>();
    final badgeProvider = context.read<UserBadgeProvider>();
    try {
      User? user;
      if (userId != null) {
        try {
          user = await userProvider.getById(userId);
        } catch (_) {}
      }
      final reservationsResult = await reservationProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      final ticketsResult = await ticketProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      final adoptionsResult = await adoptionProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      final donationsResult = await donationProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      final badgesResult = await badgeProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      setState(() {
        _user = user;
        _reservations = reservationsResult.items ?? [];
        _tickets = ticketsResult.items ?? [];
        _adoptions = adoptionsResult.items ?? [];
        _donations = donationsResult.items ?? [];
        _badges = badgesResult.items ?? [];
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _loading = false);
      showAppError(context, e);
    }
  }

  int get _activeAdoptions =>
      _adoptions.where((a) => a.status == AdoptionStatus.active).length;

  double get _donatedTotal =>
      _donations.fold<double>(0, (sum, item) => sum + item.amount);

  List<_ActivityItem> get _recent {
    final items = <_ActivityItem>[
      for (final reservation in _reservations)
        _ActivityItem(
          title: reservation.activityName.isEmpty
              ? 'Reservation'
              : reservation.activityName,
          date: reservation.createdAt ?? reservation.scheduleDate,
          chip: reservation.statusLabel,
          tone: _reservationTone(reservation.status),
        ),
      for (final ticket in _tickets)
        _ActivityItem(
          title: 'Entry ticket',
          date: ticket.createdAt ?? ticket.visitDate,
          chip: ticket.statusLabel,
          tone: switch (ticket.status) {
            TicketStatus.active => StatusTone.success,
            TicketStatus.used => StatusTone.neutral,
            TicketStatus.cancelled => StatusTone.danger,
            TicketStatus.pendingPayment => StatusTone.warning,
          },
        ),
      for (final adoption in _adoptions)
        _ActivityItem(
          title: 'Adopted ${adoption.animalName}',
          date: adoption.startDate,
          chip: adoption.statusLabel,
          tone: adoption.status == AdoptionStatus.active
              ? StatusTone.success
              : adoption.status == AdoptionStatus.cancelled
              ? StatusTone.danger
              : StatusTone.warning,
        ),
      for (final donation in _donations)
        _ActivityItem(
          title: '${formatMoney(donation.amount)} to ${donation.zooPartnerName}',
          date: donation.createdAt,
          chip: 'Completed',
          tone: StatusTone.neutral,
        ),
    ]..sort((a, b) => b.date.compareTo(a.date));
    return items.take(4).toList();
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        children: [
          const WildNestHeader(),
          Expanded(
            child: _loading
                ? const Center(
                    child: CircularProgressIndicator(color: AppColors.sage),
                  )
                : RefreshIndicator(
                    color: AppColors.sage,
                    onRefresh: _load,
                    child: ListView(
                      padding: const EdgeInsets.fromLTRB(20, 8, 20, 88),
                      children: [
                        _ProfileHeader(
                          user: _user,
                          badgeCount: _badges.length,
                          adoptionCount: _activeAdoptions,
                        ),
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            _StatBox(value: '${_reservations.length}', label: 'Reservations'),
                            const SizedBox(width: 10),
                            _StatBox(value: '$_activeAdoptions', label: 'Adoptions'),
                            const SizedBox(width: 10),
                            _StatBox(value: formatMoney(_donatedTotal), label: 'Donated'),
                          ],
                        ),
                        const SizedBox(height: 16),
                        Material(
                          color: AppColors.surface,
                          borderRadius: BorderRadius.circular(24),
                          clipBehavior: Clip.antiAlias,
                          child: Column(
                            children: [
                              _LinkTile(
                                icon: Icons.confirmation_number_outlined,
                                label: 'My Tickets',
                                count: _tickets.length,
                                onTap: () async {
                                  await Navigator.of(context).push(
                                    MaterialPageRoute(
                                      builder: (_) => const MyTicketsScreen(),
                                    ),
                                  );
                                  if (mounted) _load();
                                },
                              ),
                              _LinkTile(
                                icon: Icons.calendar_today_outlined,
                                label: 'My Reservations',
                                count: _reservations.length,
                                onTap: () async {
                                  await Navigator.of(context).push(
                                    MaterialPageRoute(
                                      builder: (_) => const MyReservationsScreen(),
                                    ),
                                  );
                                  if (mounted) _load();
                                },
                              ),
                              _LinkTile(
                                icon: Icons.favorite_border,
                                label: 'My Adoptions',
                                count: _activeAdoptions,
                                onTap: () async {
                                  await Navigator.of(context).push(
                                    MaterialPageRoute(
                                      builder: (_) => const MyAdoptionsScreen(),
                                    ),
                                  );
                                  if (mounted) _load();
                                },
                              ),
                              _LinkTile(
                                icon: Icons.account_balance_wallet_outlined,
                                label: 'Donation History',
                                onTap: () async {
                                  await Navigator.of(context).push(
                                    MaterialPageRoute(
                                      builder: (_) => const DonationHistoryScreen(),
                                    ),
                                  );
                                  if (mounted) _load();
                                },
                              ),
                              _LinkTile(
                                icon: Icons.workspace_premium_outlined,
                                label: 'My Badges',
                                count: _badges.length,
                                onTap: () {
                                  Navigator.of(context).push(
                                    MaterialPageRoute(
                                      builder: (_) => const MyBadgesScreen(),
                                    ),
                                  );
                                },
                              ),
                              _LinkTile(
                                icon: Icons.settings_outlined,
                                label: 'Settings',
                                onTap: () {
                                  Navigator.of(context).push(
                                    MaterialPageRoute(
                                      builder: (_) => const SettingsScreen(),
                                    ),
                                  );
                                },
                                showDivider: false,
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 22),
                        Text(
                          'Recent Activity',
                          style: GoogleFonts.playfairDisplay(
                            fontSize: 22,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(height: 12),
                        if (_recent.isEmpty)
                          const Text(
                            'No recent activity yet.',
                            style: TextStyle(color: AppColors.textMuted),
                          )
                        else
                          for (final item in _recent) ...[
                            _RecentTile(item: item),
                            const SizedBox(height: 10),
                          ],
                      ],
                    ),
                  ),
          ),
        ],
      ),
    );
  }
}

StatusTone _reservationTone(ReservationStatus status) {
  switch (status) {
    case ReservationStatus.confirmed:
      return StatusTone.success;
    case ReservationStatus.completed:
      return StatusTone.neutral;
    case ReservationStatus.cancelled:
      return StatusTone.danger;
    case ReservationStatus.pending:
      return StatusTone.warning;
  }
}

class _ActivityItem {
  const _ActivityItem({
    required this.title,
    required this.date,
    required this.chip,
    required this.tone,
  });

  final String title;
  final DateTime date;
  final String chip;
  final StatusTone tone;
}

class _ProfileHeader extends StatelessWidget {
  const _ProfileHeader({
    required this.user,
    required this.badgeCount,
    required this.adoptionCount,
  });

  final User? user;
  final int badgeCount;
  final int adoptionCount;

  @override
  Widget build(BuildContext context) {
    final name = user?.fullName ?? 'Guest';
    final image = _decodeImage(user?.profileImageBase64);
    return Row(
      children: [
        CircleAvatar(
          radius: 34,
          backgroundColor: AppColors.mint,
          backgroundImage: image == null ? null : MemoryImage(image),
          child: image == null
              ? const Icon(Icons.person, color: AppColors.sage, size: 32)
              : null,
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                name,
                style: GoogleFonts.playfairDisplay(
                  fontSize: 24,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const Text(
                'Wildlife Enthusiast',
                style: TextStyle(color: AppColors.textMuted),
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                children: [
                  _MiniPill(label: '$badgeCount Badges'),
                  _MiniPill(label: '$adoptionCount Adoptions'),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}

Uint8List? _decodeImage(String? raw) {
  if (raw == null || raw.isEmpty) return null;
  try {
    final cleaned = raw.contains(',') ? raw.split(',').last : raw;
    return base64Decode(cleaned);
  } catch (_) {
    return null;
  }
}

class _MiniPill extends StatelessWidget {
  const _MiniPill({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: AppColors.cream,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
      ),
    );
  }
}

class _StatBox extends StatelessWidget {
  const _StatBox({required this.value, required this.label});

  final String value;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 16),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(20),
        ),
        child: Column(
          children: [
            Text(
              value,
              style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w700,
                color: AppColors.sage,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              label,
              style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
            ),
          ],
        ),
      ),
    );
  }
}

class _LinkTile extends StatelessWidget {
  const _LinkTile({
    required this.icon,
    required this.label,
    required this.onTap,
    this.count,
    this.showDivider = true,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final int? count;
  final bool showDivider;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        ListTile(
          onTap: onTap,
          leading: Icon(icon, color: AppColors.sage),
          title: Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (count != null)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: AppColors.cream,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text('$count', style: const TextStyle(fontWeight: FontWeight.w700)),
                ),
              const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
            ],
          ),
        ),
        if (showDivider)
          const Divider(height: 1, indent: 16, endIndent: 16, color: AppColors.border),
      ],
    );
  }
}

class _RecentTile extends StatelessWidget {
  const _RecentTile({required this.item});

  final _ActivityItem item;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(item.title, style: const TextStyle(fontWeight: FontWeight.w700)),
                const SizedBox(height: 4),
                Text(
                  formatReviewDate(item.date),
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
              ],
            ),
          ),
          StatusChip(label: item.chip, tone: item.tone),
        ],
      ),
    );
  }
}
