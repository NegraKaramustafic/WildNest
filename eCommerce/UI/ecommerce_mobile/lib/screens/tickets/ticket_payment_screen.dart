import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/providers/payment_provider.dart';
import 'package:ecommerce_mobile/providers/ticket_provider.dart';
import 'package:ecommerce_mobile/screens/tickets/ticket_detail_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/stripe_checkout.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class TicketPaymentScreen extends StatefulWidget {
  const TicketPaymentScreen({super.key, required this.ticket});

  final Ticket ticket;

  @override
  State<TicketPaymentScreen> createState() => _TicketPaymentScreenState();
}

class _TicketPaymentScreenState extends State<TicketPaymentScreen> {
  bool _busy = false;

  Future<void> _pay() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final ticketProvider = context.read<TicketProvider>();
      final paymentProvider = context.read<PaymentProvider>();
      final checkout = await paymentProvider.checkoutTicket(widget.ticket.id);
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
      var ticket = widget.ticket;
      try {
        ticket = await ticketProvider.getById(ticket.id);
      } catch (_) {}
      await fetchPaymentStatus(paymentProvider, checkout.paymentId);
      if (!mounted) return;
      Navigator.of(context).pushAndRemoveUntil(
        MaterialPageRoute(
          builder: (_) => TicketDetailScreen(ticketId: ticket.id),
        ),
        (route) => route.isFirst,
      );
    } catch (e) {
      if (!mounted) return;
      setState(() => _busy = false);
      showAppError(context, e);
    }
  }

  @override
  Widget build(BuildContext context) {
    final ticket = widget.ticket;
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
                      fontSize: 32,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Pay securely with Stripe Checkout',
                    style: TextStyle(color: AppColors.textMuted),
                  ),
                  const SizedBox(height: 18),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
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
                            'Card details are entered on Stripe',
                            style: TextStyle(fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 18),
                  Container(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(24),
                    ),
                    child: Column(
                      children: [
                        _Row(
                          label: 'Visit date',
                          value: formatReviewDate(ticket.visitDate),
                        ),
                        _Row(
                          label: 'Visitors',
                          value: visitorLabel(ticket.visitorCount),
                        ),
                        const Divider(color: AppColors.border),
                        _Row(
                          label: 'Total',
                          value: formatMoney(ticket.totalPrice),
                          emphasize: true,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 20),
              child: ElevatedButton(
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
                    : Text('Pay ${formatMoney(ticket.totalPrice)}'),
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
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          Text(
            label,
            style: TextStyle(
              fontWeight: emphasize ? FontWeight.w700 : FontWeight.w500,
              color: emphasize ? AppColors.text : AppColors.textMuted,
            ),
          ),
          const Spacer(),
          Text(
            value,
            style: TextStyle(
              fontWeight: FontWeight.w800,
              color: emphasize ? AppColors.sage : AppColors.text,
              fontSize: emphasize ? 18 : 15,
            ),
          ),
        ],
      ),
    );
  }
}
