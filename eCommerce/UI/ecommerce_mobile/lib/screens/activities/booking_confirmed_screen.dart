import 'package:ecommerce_mobile/models/payment.dart';
import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class BookingConfirmedScreen extends StatelessWidget {
  const BookingConfirmedScreen({
    super.key,
    required this.reservation,
    this.paymentStatus,
  });

  final Reservation reservation;
  final PaymentStatus? paymentStatus;

  bool get _paid => paymentStatus == PaymentStatus.completed;
  bool get _confirmed =>
      reservation.status == ReservationStatus.confirmed ||
      reservation.status == ReservationStatus.completed;

  @override
  Widget build(BuildContext context) {
    final time = formatClock(reservation.scheduleStartTime);
    final title = _paid || _confirmed
        ? 'Booking Confirmed!'
        : 'Reservation pending';
    final message = _paid || _confirmed
        ? 'Your reservation for $time has been confirmed.\nCheck your email for details.'
        : 'Your reservation for $time is saved as pending. If you finished paying, confirmation will follow shortly.';

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            const WildNestHeader(),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(28, 24, 28, 24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      width: 88,
                      height: 88,
                      decoration: const BoxDecoration(
                        color: AppColors.mint,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        _paid || _confirmed
                            ? Icons.check_rounded
                            : Icons.hourglass_top_rounded,
                        size: 44,
                        color: AppColors.sage,
                      ),
                    ),
                    const SizedBox(height: 22),
                    Text(
                      title,
                      textAlign: TextAlign.center,
                      style: GoogleFonts.playfairDisplay(
                        fontSize: 28,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      message,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: AppColors.textMuted,
                        height: 1.45,
                        fontSize: 15,
                      ),
                    ),
                    const SizedBox(height: 28),
                    Row(
                      children: [
                        Expanded(
                          child: ElevatedButton(
                            onPressed: () {
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text(
                                    'Reservations list is coming soon.',
                                  ),
                                ),
                              );
                            },
                            child: const FittedBox(
                              child: Text('View Reservations'),
                            ),
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: OutlinedButton(
                            onPressed: () {
                              Navigator.of(context).popUntil((route) => route.isFirst);
                            },
                            child: const Text('Home'),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
