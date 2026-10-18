import 'package:ecommerce_mobile/models/donation.dart';
import 'package:ecommerce_mobile/models/zoo_partner.dart';
import 'package:ecommerce_mobile/providers/donation_provider.dart';
import 'package:ecommerce_mobile/providers/payment_provider.dart';
import 'package:ecommerce_mobile/screens/donations/donation_confirmed_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/stripe_checkout.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:ecommerce_mobile/widgets/zoo_partner_visuals.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class DonationPaymentScreen extends StatefulWidget {
  const DonationPaymentScreen({
    super.key,
    required this.partner,
    required this.amount,
  });

  final ZooPartner partner;
  final double amount;

  @override
  State<DonationPaymentScreen> createState() => _DonationPaymentScreenState();
}

class _DonationPaymentScreenState extends State<DonationPaymentScreen> {
  bool _busy = false;
  Donation? _donation;

  Future<void> _pay() async {
    if (_busy || _donation?.isPaid == true) return;
    setState(() => _busy = true);
    try {
      final donationProvider = context.read<DonationProvider>();
      final paymentProvider = context.read<PaymentProvider>();
      var donation = await donationProvider.createPending(
        zooPartnerId: widget.partner.id,
        amount: widget.amount,
      );
      _donation = donation;
      if (donation.isPaid) {
        if (mounted) setState(() => _busy = false);
        return;
      }
      final checkout = await paymentProvider.checkoutDonation(donation.id);
      final url = checkout.sessionUrl;
      if (url == null || url.isEmpty) {
        throw Exception('Stripe did not return a checkout URL.');
      }
      final outcome = await launchStripeCheckout(
        url,
        payments: paymentProvider,
        paymentId: checkout.paymentId,
        stripeCheckoutSessionId: checkout.stripeCheckoutSessionId,
      );
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
      try {
        donation = await donationProvider.getById(donation.id);
      } catch (_) {}
      final paymentStatus = await fetchPaymentStatus(
        paymentProvider,
        checkout.paymentId,
      );
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => DonationConfirmedScreen(
            partner: widget.partner,
            donation: donation,
            paymentStatus: paymentStatus,
          ),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      setState(() => _busy = false);
      showAppError(context, e);
    }
  }

  @override
  Widget build(BuildContext context) {
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
                    'Donation payment',
                    style: GoogleFonts.playfairDisplay(
                      fontSize: 30,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 18),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(24),
                    ),
                    child: Row(
                      children: [
                        Container(
                          width: 56,
                          height: 56,
                          decoration: BoxDecoration(
                            color: AppColors.mint,
                            borderRadius: BorderRadius.circular(16),
                          ),
                          child: Center(
                            child: Text(
                              zooPartnerEmoji(widget.partner),
                              style: const TextStyle(fontSize: 26),
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                widget.partner.name,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                  fontSize: 16,
                                ),
                              ),
                              Text(
                                'Donation: ${formatMoney(widget.amount)}',
                                style: const TextStyle(color: AppColors.textMuted),
                              ),
                            ],
                          ),
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
                  if (_donation?.isPaid == true)
                    const Text(
                      'Paid.',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontWeight: FontWeight.w700,
                        color: AppColors.sage,
                        fontSize: 16,
                      ),
                    )
                  else
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
                        : Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              const Icon(Icons.favorite, size: 18),
                              const SizedBox(width: 8),
                              Text('Donate ${formatMoney(widget.amount)}'),
                            ],
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
