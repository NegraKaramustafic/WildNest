import 'package:ecommerce_mobile/models/activity.dart';
import 'package:ecommerce_mobile/models/activity_schedule.dart';
import 'package:ecommerce_mobile/models/payment.dart';
import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/providers/payment_provider.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/screens/activities/booking_confirmed_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/stripe_checkout.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class PaymentScreen extends StatefulWidget {
  const PaymentScreen({
    super.key,
    required this.reservation,
    required this.activity,
    required this.schedule,
  });

  final Reservation reservation;
  final Activity activity;
  final ActivitySchedule schedule;

  @override
  State<PaymentScreen> createState() => _PaymentScreenState();
}

class _PaymentScreenState extends State<PaymentScreen> {
  bool _busy = false;

  String get _activityLabel {
    final name = widget.reservation.activityName;
    return name.isNotEmpty ? name : widget.activity.name;
  }

  Future<void> _pay() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final paymentProvider = context.read<PaymentProvider>();
      final checkout = await paymentProvider.checkoutReservation(
        widget.reservation.id,
      );
      final url = checkout.sessionUrl;
      if (url == null || url.isEmpty) {
        throw Exception('Stripe did not return a checkout URL.');
      }

      final outcome = await launchStripeCheckout(url);
      if (!mounted) return;
      if (outcome == StripeCheckoutOutcome.failedToLaunch) {
        alertBox(
          context,
          'Payment',
          'Could not open Stripe Checkout. Please try again.',
        );
        setState(() => _busy = false);
        return;
      }
      if (outcome != StripeCheckoutOutcome.success) {
        setState(() => _busy = false);
        return;
      }

      await _goToResult(checkout.paymentId);
    } catch (e) {
      if (!mounted) return;
      setState(() => _busy = false);
      showAppError(context, e);
    }
  }

  Future<void> _goToResult(int? paymentId) async {
    final reservationProvider = context.read<ReservationProvider>();
    final paymentProvider = context.read<PaymentProvider>();
    var reservation = widget.reservation;
    PaymentStatus? paymentStatus;
    try {
      reservation = await reservationProvider.getById(reservation.id);
    } catch (_) {}
    if (paymentId != null || reservation.paymentId != null) {
      paymentStatus = await fetchPaymentStatus(
        paymentProvider,
        paymentId ?? reservation.paymentId,
      );
    }
    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (_) => BookingConfirmedScreen(
          reservation: reservation,
          paymentStatus: paymentStatus,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final reservation = widget.reservation;
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                children: [
                  Text(
                    'Payment',
                    style: GoogleFonts.playfairDisplay(
                      fontSize: 30,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 18),
                  Container(
                    padding: const EdgeInsets.fromLTRB(18, 16, 18, 8),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(24),
                    ),
                    child: Column(
                      children: [
                        _Row(label: 'Activity', value: _activityLabel),
                        _Row(
                          label: 'Time',
                          value: formatClock(
                            reservation.scheduleStartTime == Duration.zero
                                ? widget.schedule.startTime
                                : reservation.scheduleStartTime,
                          ),
                        ),
                        _Row(
                          label: 'Guests',
                          value: guestLabel(reservation.guestCount),
                        ),
                        const Divider(color: AppColors.border),
                        _Row(
                          label: 'Total',
                          value: formatMoney(reservation.totalPrice, decimals: true),
                          emphasize: true,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 22),
                  const Text(
                    'Payment Method',
                    style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                  ),
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 16,
                    ),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: AppColors.sage.withValues(alpha: 0.45)),
                    ),
                    child: const Row(
                      children: [
                        Icon(Icons.lock_outline, color: AppColors.sage),
                        SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            'Pay securely with Stripe Checkout',
                            style: TextStyle(fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Card details are entered in a secure in-app Stripe sheet. You will return here automatically when payment finishes.',
                    style: TextStyle(color: AppColors.textMuted, fontSize: 13, height: 1.4),
                  ),
                  const SizedBox(height: 28),
                  ElevatedButton(
                    onPressed: _busy ? null : _pay,
                    child: _busy
                        ? const SizedBox(
                            width: 22,
                            height: 22,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : Text(
                            'Pay ${formatMoney(reservation.totalPrice, decimals: true)}',
                          ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({
    required this.label,
    required this.value,
    this.emphasize = false,
  });

  final String label;
  final String value;
  final bool emphasize;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Text(
            label,
            style: TextStyle(
              color: emphasize ? AppColors.text : AppColors.textMuted,
              fontWeight: emphasize ? FontWeight.w700 : FontWeight.w500,
            ),
          ),
          const Spacer(),
          Text(
            value,
            style: TextStyle(
              fontWeight: FontWeight.w700,
              color: emphasize ? AppColors.sage : AppColors.text,
              fontSize: emphasize ? 18 : 15,
            ),
          ),
        ],
      ),
    );
  }
}
