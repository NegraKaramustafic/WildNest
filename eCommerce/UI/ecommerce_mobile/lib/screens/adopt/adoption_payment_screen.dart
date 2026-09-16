import 'package:ecommerce_mobile/models/adoption.dart';
import 'package:ecommerce_mobile/models/animal.dart';
import 'package:ecommerce_mobile/providers/adoption_provider.dart';
import 'package:ecommerce_mobile/providers/payment_provider.dart';
import 'package:ecommerce_mobile/screens/adopt/adoption_confirmed_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/stripe_checkout.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/animal_visuals.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class AdoptionPaymentScreen extends StatefulWidget {
  const AdoptionPaymentScreen({
    super.key,
    required this.animal,
    this.existingAdoption,
  });

  final Animal animal;
  final Adoption? existingAdoption;

  @override
  State<AdoptionPaymentScreen> createState() => _AdoptionPaymentScreenState();
}

class _AdoptionPaymentScreenState extends State<AdoptionPaymentScreen> {
  bool _busy = false;

  double get _amount =>
      widget.existingAdoption?.monthlyAmount ?? widget.animal.adoptionPrice ?? 0;

  Future<void> _pay() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final adoptionProvider = context.read<AdoptionProvider>();
      final paymentProvider = context.read<PaymentProvider>();
      var adoption = widget.existingAdoption;
      if (adoption == null || adoption.status != AdoptionStatus.pendingPayment) {
        adoption = await adoptionProvider.createPending(widget.animal.id);
      }
      final checkout = await paymentProvider.checkoutAdoption(adoption.id);
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

      try {
        adoption = await adoptionProvider.getById(adoption.id);
      } catch (_) {}
      final paymentStatus = await fetchPaymentStatus(
        paymentProvider,
        checkout.paymentId,
      );
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => AdoptionConfirmedScreen(
            animal: widget.animal,
            adoption: adoption!,
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
                    'Adoption payment',
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
                              animalEmoji(widget.animal),
                              style: const TextStyle(fontSize: 28),
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                widget.animal.name,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                  fontSize: 16,
                                ),
                              ),
                              Text(
                                widget.animal.species,
                                style: const TextStyle(color: AppColors.textMuted),
                              ),
                            ],
                          ),
                        ),
                        Text(
                          '${formatMoney(_amount, decimals: true)}/mo',
                          style: const TextStyle(
                            fontWeight: FontWeight.w700,
                            color: AppColors.sage,
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
                        : Text('Adopt for ${formatMoney(_amount, decimals: true)}/month'),
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
