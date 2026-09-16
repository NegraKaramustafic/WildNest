import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/status_chip.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import 'package:qr_flutter/qr_flutter.dart';

class ReservationDetailScreen extends StatefulWidget {
  const ReservationDetailScreen({super.key, required this.reservationId});

  final int reservationId;

  @override
  State<ReservationDetailScreen> createState() => _ReservationDetailScreenState();
}

class _ReservationDetailScreenState extends State<ReservationDetailScreen> {
  Reservation? _reservation;
  bool _loading = true;
  bool _cancelling = false;
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
      final reservation = await context.read<ReservationProvider>().getById(
        widget.reservationId,
      );
      if (!mounted) return;
      setState(() {
        _reservation = reservation;
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

  Future<void> _cancel() async {
    final reservation = _reservation;
    if (reservation == null || _cancelling) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cancel reservation'),
        content: const Text('Are you sure you want to cancel this reservation?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Keep'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Cancel reservation', style: TextStyle(color: AppColors.heart)),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    setState(() => _cancelling = true);
    try {
      final updated = await context.read<ReservationProvider>().cancel(reservation.id);
      if (!mounted) return;
      setState(() {
        _reservation = updated;
        _cancelling = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _cancelling = false);
      showAppError(context, e);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: _loading
            ? const Center(child: CircularProgressIndicator(color: AppColors.sage))
            : _error != null && _reservation == null
            ? Column(
                children: [
                  WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
                  Expanded(
                    child: Center(
                      child: ElevatedButton(onPressed: _load, child: const Text('Try again')),
                    ),
                  ),
                ],
              )
            : _content(_reservation!),
      ),
    );
  }

  Widget _content(Reservation reservation) {
    return Column(
      children: [
        WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
            children: [
              Text(
                'Reservation details',
                style: GoogleFonts.playfairDisplay(
                  fontSize: 30,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 20),
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(28),
                ),
                child: Column(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: AppColors.cream,
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: QrImageView(
                        data: reservation.code.isEmpty
                            ? 'WN-${reservation.id}'
                            : reservation.code,
                        size: 180,
                        backgroundColor: AppColors.cream,
                        eyeStyle: const QrEyeStyle(
                          eyeShape: QrEyeShape.square,
                          color: AppColors.sageDark,
                        ),
                        dataModuleStyle: const QrDataModuleStyle(
                          dataModuleShape: QrDataModuleShape.square,
                          color: AppColors.sageDark,
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      reservation.code,
                      style: const TextStyle(
                        letterSpacing: 2,
                        fontWeight: FontWeight.w800,
                        color: AppColors.sage,
                        fontSize: 18,
                      ),
                    ),
                    const SizedBox(height: 6),
                    const Text(
                      'Show this code at the entrance',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                    const SizedBox(height: 14),
                    StatusChip(
                      label: reservation.statusLabel,
                      tone: switch (reservation.status) {
                        ReservationStatus.confirmed => StatusTone.success,
                        ReservationStatus.completed => StatusTone.neutral,
                        ReservationStatus.cancelled => StatusTone.danger,
                        ReservationStatus.pending => StatusTone.warning,
                      },
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(24),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      reservation.activityName.isEmpty
                          ? 'Reservation'
                          : reservation.activityName,
                      style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                    ),
                    const SizedBox(height: 12),
                    _InfoRow(
                      icon: Icons.calendar_today_outlined,
                      label: 'Date',
                      value: formatReviewDate(reservation.scheduleDate),
                    ),
                    _InfoRow(
                      icon: Icons.schedule,
                      label: 'Time',
                      value: formatClock(reservation.scheduleStartTime),
                    ),
                    _InfoRow(
                      icon: Icons.group_outlined,
                      label: 'Visitors',
                      value: visitorLabel(reservation.guestCount),
                    ),
                  ],
                ),
              ),
              if (reservation.canCancel) ...[
                const SizedBox(height: 20),
                OutlinedButton(
                  onPressed: _cancelling ? null : _cancel,
                  style: OutlinedButton.styleFrom(
                    foregroundColor: AppColors.heart,
                    side: const BorderSide(color: Color(0xFFF3C4C4)),
                  ),
                  child: _cancelling
                      ? const SizedBox(
                          width: 22,
                          height: 22,
                          child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.heart),
                        )
                      : const Text('Cancel reservation'),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Icon(icon, size: 18, color: AppColors.sage),
          const SizedBox(width: 10),
          Text(label, style: const TextStyle(color: AppColors.textMuted)),
          const Spacer(),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w700)),
        ],
      ),
    );
  }
}
