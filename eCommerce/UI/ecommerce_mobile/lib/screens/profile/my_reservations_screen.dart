import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/screens/profile/reservation_detail_screen.dart';
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

class _MyReservationsScreenState extends State<MyReservationsScreen> {
  List<Reservation> _items = [];
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
      final result = await context.read<ReservationProvider>().get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      final items = <Reservation>[...?result.items]
        ..sort((a, b) {
          final left = a.createdAt ?? a.scheduleDate;
          final right = b.createdAt ?? b.scheduleDate;
          return right.compareTo(left);
        });
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
                emptyMessage: 'You do not have any reservations yet.',
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
                  itemCount: _items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (context, index) {
                    final reservation = _items[index];
                    return _ReservationCard(
                      reservation: reservation,
                      onTap: () async {
                        await Navigator.of(context).push(
                          MaterialPageRoute(
                            builder: (_) => ReservationDetailScreen(
                              reservationId: reservation.id,
                            ),
                          ),
                        );
                        if (mounted) _load();
                      },
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

class _ReservationCard extends StatelessWidget {
  const _ReservationCard({required this.reservation, required this.onTap});

  final Reservation reservation;
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
              child: const Icon(Icons.calendar_today_rounded, color: AppColors.sage, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    reservation.activityName.isEmpty
                        ? 'Reservation'
                        : reservation.activityName,
                    style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${formatReviewDate(reservation.scheduleDate)} · ${formatClock(reservation.scheduleStartTime)}',
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                  ),
                  Text(
                    visitorLabel(reservation.guestCount),
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                StatusChip(
                  label: reservation.statusLabel,
                  tone: switch (reservation.status) {
                    ReservationStatus.confirmed => StatusTone.success,
                    ReservationStatus.completed => StatusTone.neutral,
                    ReservationStatus.cancelled => StatusTone.danger,
                    ReservationStatus.pending => StatusTone.warning,
                  },
                ),
                const SizedBox(height: 8),
                Text(
                  reservation.code,
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
