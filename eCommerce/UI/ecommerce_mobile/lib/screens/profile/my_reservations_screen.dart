import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/providers/ticket_provider.dart';
import 'package:ecommerce_mobile/screens/profile/reservation_detail_screen.dart';
import 'package:ecommerce_mobile/screens/tickets/ticket_detail_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/async_body.dart';
import 'package:ecommerce_mobile/widgets/status_chip.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class MyReservationsScreen extends StatefulWidget {
  const MyReservationsScreen({super.key});

  @override
  State<MyReservationsScreen> createState() => _MyReservationsScreenState();
}

class _PassItem {
  const _PassItem.reservation(this.reservation) : ticket = null;

  const _PassItem.ticket(this.ticket) : reservation = null;

  final Reservation? reservation;
  final Ticket? ticket;

  DateTime get when {
    if (reservation != null) {
      return reservation!.createdAt ?? reservation!.scheduleDate;
    }
    return ticket!.createdAt ?? ticket!.visitDate;
  }
}

class _MyReservationsScreenState extends State<MyReservationsScreen> {
  List<_PassItem> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final reservations = context.read<ReservationProvider>();
      final tickets = context.read<TicketProvider>();
      final reservationResult = await reservations.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      final ticketResult = await tickets.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      final items = <_PassItem>[
        for (final reservation in reservationResult.items ?? <Reservation>[])
          _PassItem.reservation(reservation),
        for (final ticket in ticketResult.items ?? <Ticket>[]) _PassItem.ticket(ticket),
      ]..sort((a, b) => b.when.compareTo(a.when));
      setState(() {
        _items = items;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = e.toString();
      });
      showAppError(context, e);
    }
  }

  Future<void> _openReservation(int id) async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => ReservationDetailScreen(reservationId: id)),
    );
    if (mounted) _load();
  }

  Future<void> _openTicket(int id) async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => TicketDetailScreen(ticketId: id)),
    );
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 16),
              child: Text(
                'My Reservations',
                style: GoogleFonts.playfairDisplay(
                  fontSize: 30,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            Expanded(
              child: AsyncBody(
                loading: _loading,
                error: _error,
                onRetry: _load,
                isEmpty: _items.isEmpty,
                emptyMessage: 'You do not have any reservations or tickets yet.',
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
                  itemCount: _items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (context, index) {
                    final item = _items[index];
                    final reservation = item.reservation;
                    if (reservation != null) {
                      return _PassCard(
                        label: 'Activity reservation',
                        title: reservation.activityName.isEmpty
                            ? 'Reservation'
                            : reservation.activityName,
                        subtitle:
                            '${formatReviewDate(reservation.scheduleDate)} · ${formatClock(reservation.scheduleStartTime)}',
                        detail: visitorLabel(reservation.guestCount),
                        code: reservation.code,
                        chip: reservation.statusLabel,
                        tone: switch (reservation.status) {
                          ReservationStatus.confirmed => StatusTone.success,
                          ReservationStatus.completed => StatusTone.neutral,
                          ReservationStatus.cancelled => StatusTone.danger,
                          ReservationStatus.pending => StatusTone.warning,
                        },
                        icon: Icons.calendar_today_rounded,
                        onTap: () => _openReservation(reservation.id),
                      );
                    }
                    final ticket = item.ticket!;
                    return _PassCard(
                      label: 'Ticket',
                      title: 'Zoo entry',
                      subtitle: formatReviewDate(ticket.visitDate),
                      detail: visitorLabel(ticket.visitorCount),
                      code: ticket.code,
                      chip: ticket.statusLabel,
                      tone: switch (ticket.status) {
                        TicketStatus.active => StatusTone.success,
                        TicketStatus.used => StatusTone.neutral,
                        TicketStatus.cancelled => StatusTone.danger,
                        TicketStatus.pendingPayment => StatusTone.warning,
                      },
                      icon: Icons.confirmation_number_outlined,
                      onTap: () => _openTicket(ticket.id),
                    );
                  },
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PassCard extends StatelessWidget {
  const _PassCard({
    required this.label,
    required this.title,
    required this.subtitle,
    required this.detail,
    required this.code,
    required this.chip,
    required this.tone,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final String title;
  final String subtitle;
  final String detail;
  final String code;
  final String chip;
  final StatusTone tone;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(22),
      child: Ink(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(22),
        ),
        child: Row(
          children: [
            Container(
              width: 46,
              height: 46,
              decoration: BoxDecoration(
                color: AppColors.mint,
                borderRadius: BorderRadius.circular(14),
              ),
              child: Icon(icon, color: AppColors.sage, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    label,
                    style: const TextStyle(
                      color: AppColors.sage,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    title,
                    style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    subtitle,
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                  ),
                  Text(
                    detail,
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                StatusChip(label: chip, tone: tone),
                const SizedBox(height: 8),
                Text(
                  code,
                  style: const TextStyle(
                    color: AppColors.textMuted,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
