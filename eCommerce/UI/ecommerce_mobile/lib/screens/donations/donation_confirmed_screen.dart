import 'package:ecommerce_mobile/models/donation.dart';
import 'package:ecommerce_mobile/models/payment.dart';
import 'package:ecommerce_mobile/models/zoo_partner.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class DonationConfirmedScreen extends StatelessWidget {
  const DonationConfirmedScreen({
    super.key,
    required this.partner,
    required this.donation,
    this.paymentStatus,
  });

  final ZooPartner partner;
  final Donation donation;
  final PaymentStatus? paymentStatus;

  bool get _paid => paymentStatus == PaymentStatus.completed;

  @override
  Widget build(BuildContext context) {
    final amount = formatMoney(donation.amount);
    final title = _paid ? 'Thank you!' : 'Donation pending';
    final message = _paid
        ? 'Your donation of $amount to ${partner.name} was processed successfully.'
        : 'If you finished paying, confirmation will follow shortly.';

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
                        _paid ? Icons.check_rounded : Icons.hourglass_top_rounded,
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
                    ElevatedButton(
                      onPressed: () {
                        Navigator.of(context).popUntil((route) {
                          return route.isFirst ||
                              route.settings.name == 'donations-list';
                        });
                      },
                      child: const Text('Back to donations'),
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
