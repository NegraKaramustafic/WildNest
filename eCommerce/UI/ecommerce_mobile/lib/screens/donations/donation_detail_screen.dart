import 'package:ecommerce_mobile/models/zoo_partner.dart';
import 'package:ecommerce_mobile/providers/zoo_partner_provider.dart';
import 'package:ecommerce_mobile/screens/donations/donation_payment_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:ecommerce_mobile/widgets/zoo_partner_visuals.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class DonationDetailScreen extends StatefulWidget {
  const DonationDetailScreen({super.key, required this.partnerId});

  final int partnerId;

  @override
  State<DonationDetailScreen> createState() => _DonationDetailScreenState();
}

class _DonationDetailScreenState extends State<DonationDetailScreen> {
  static const _amounts = [5.0, 10.0, 25.0, 50.0, 100.0];

  ZooPartner? _partner;
  bool _loading = true;
  String? _error;
  double _amount = 25;

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
      final partner = await context.read<ZooPartnerProvider>().getById(widget.partnerId);
      if (!mounted) return;
      setState(() {
        _partner = partner;
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
        child: _loading
            ? const Center(child: CircularProgressIndicator(color: AppColors.sage))
            : _error != null && _partner == null
            ? _errorView()
            : _content(_partner!),
      ),
    );
  }

  Widget _errorView() {
    return Column(
      children: [
        WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
        Expanded(
          child: Center(
            child: ElevatedButton(onPressed: _load, child: const Text('Try again')),
          ),
        ),
      ],
    );
  }

  Widget _content(ZooPartner partner) {
    return Column(
      children: [
        WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.only(bottom: 24),
            children: [
              Container(
                height: 180,
                margin: const EdgeInsets.symmetric(horizontal: 16),
                decoration: BoxDecoration(
                  color: AppColors.mint,
                  borderRadius: BorderRadius.circular(28),
                ),
                child: Center(
                  child: Text(
                    zooPartnerEmoji(partner),
                    style: const TextStyle(fontSize: 72),
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
                child: Text(
                  partner.name,
                  style: GoogleFonts.playfairDisplay(
                    fontSize: 30,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 6, 20, 0),
                child: Row(
                  children: [
                    const Icon(Icons.location_on_outlined, size: 16, color: AppColors.textMuted),
                    const SizedBox(width: 4),
                    Text(
                      partner.location,
                      style: const TextStyle(color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 14, 20, 0),
                child: Text(
                  partner.displayDescription,
                  style: const TextStyle(height: 1.45, fontSize: 15),
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
                child: Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Column(
                    children: [
                      Row(
                        children: [
                          Text(
                            formatMoney(partner.currentAmount),
                            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
                          ),
                          const Spacer(),
                          Text(
                            'of ${formatMoney(partner.goalAmount)}',
                            style: const TextStyle(color: AppColors.textMuted),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: LinearProgressIndicator(
                          value: partner.progress,
                          minHeight: 8,
                          backgroundColor: AppColors.cream,
                          color: AppColors.sage,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Align(
                        alignment: Alignment.centerLeft,
                        child: Text(
                          '${partner.progressPercent}% of goal reached',
                          style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const Padding(
                padding: EdgeInsets.fromLTRB(20, 22, 20, 12),
                child: Text(
                  'Choose an amount',
                  style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                ),
              ),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 20),
                child: Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children: [
                    for (final amount in _amounts)
                      ChoiceChip(
                        label: Text(formatMoney(amount)),
                        selected: _amount == amount,
                        onSelected: (_) => setState(() => _amount = amount),
                        selectedColor: AppColors.sage,
                        labelStyle: TextStyle(
                          color: _amount == amount ? Colors.white : AppColors.text,
                          fontWeight: FontWeight.w700,
                        ),
                        backgroundColor: AppColors.surface,
                        side: const BorderSide(color: AppColors.border),
                        showCheckmark: false,
                      ),
                  ],
                ),
              ),
            ],
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 20),
          child: ElevatedButton.icon(
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => DonationPaymentScreen(
                    partner: partner,
                    amount: _amount,
                  ),
                ),
              );
            },
            icon: const Icon(Icons.favorite, size: 18),
            label: Text('Donate ${formatMoney(_amount)}'),
          ),
        ),
      ],
    );
  }
}
