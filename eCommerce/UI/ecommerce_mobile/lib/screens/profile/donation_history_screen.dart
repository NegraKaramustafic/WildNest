import 'package:ecommerce_mobile/models/donation.dart';
import 'package:ecommerce_mobile/providers/donation_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/async_body.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class DonationHistoryScreen extends StatefulWidget {
  const DonationHistoryScreen({super.key});

  @override
  State<DonationHistoryScreen> createState() => _DonationHistoryScreenState();
}

class _DonationHistoryScreenState extends State<DonationHistoryScreen> {
  List<Donation> _items = [];
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
      final result = await context.read<DonationProvider>().get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      final items = <Donation>[...?result.items]
        ..sort((a, b) => b.createdAt.compareTo(a.createdAt));
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
                'Donation History',
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
                emptyMessage: 'You have not made any donations yet.',
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
                  itemCount: _items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (context, index) {
                    final donation = _items[index];
                    return Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: AppColors.surface,
                        borderRadius: BorderRadius.circular(22),
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  donation.zooPartnerName,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 16,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  formatReviewDate(donation.createdAt),
                                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                                ),
                              ],
                            ),
                          ),
                          Text(
                            formatMoney(donation.amount),
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              color: AppColors.sage,
                              fontSize: 16,
                            ),
                          ),
                        ],
                      ),
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
