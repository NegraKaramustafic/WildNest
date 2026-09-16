import 'package:ecommerce_mobile/models/zoo_partner.dart';
import 'package:ecommerce_mobile/providers/zoo_partner_provider.dart';
import 'package:ecommerce_mobile/screens/donations/donation_detail_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/async_body.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:ecommerce_mobile/widgets/zoo_partner_visuals.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class DonationsListScreen extends StatefulWidget {
  const DonationsListScreen({super.key});

  @override
  State<DonationsListScreen> createState() => _DonationsListScreenState();
}

class _DonationsListScreenState extends State<DonationsListScreen> {
  List<ZooPartner> _partners = [];
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
      final partners = await context.read<ZooPartnerProvider>().getActive();
      if (!mounted) return;
      setState(() {
        _partners = partners;
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
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
              child: Text(
                'Support small zoos',
                style: GoogleFonts.playfairDisplay(
                  fontSize: 32,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            const Padding(
              padding: EdgeInsets.fromLTRB(20, 6, 20, 16),
              child: Text(
                'Help local sanctuaries care for rescued wildlife',
                style: TextStyle(color: AppColors.textMuted, fontSize: 15),
              ),
            ),
            Expanded(
              child: AsyncBody(
                loading: _loading,
                error: _error,
                onRetry: _load,
                isEmpty: _partners.isEmpty,
                emptyMessage: 'No partner sanctuaries are available right now.',
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
                  itemCount: _partners.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (context, index) {
                    final partner = _partners[index];
                    return _PartnerCard(
                      partner: partner,
                      onTap: () async {
                        await Navigator.of(context).push(
                          MaterialPageRoute(
                            builder: (_) => DonationDetailScreen(partnerId: partner.id),
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

class _PartnerCard extends StatelessWidget {
  const _PartnerCard({required this.partner, required this.onTap});

  final ZooPartner partner;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(24),
      child: Ink(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(24),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 48,
                  height: 48,
                  decoration: BoxDecoration(
                    color: AppColors.mint,
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Center(
                    child: Text(
                      zooPartnerEmoji(partner),
                      style: const TextStyle(fontSize: 24),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        partner.name,
                        style: const TextStyle(
                          fontWeight: FontWeight.w700,
                          fontSize: 16,
                        ),
                      ),
                      Row(
                        children: [
                          const Icon(Icons.location_on_outlined, size: 14, color: AppColors.textMuted),
                          const SizedBox(width: 4),
                          Text(
                            partner.location,
                            style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted),
              ],
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Text(
                  formatMoney(partner.currentAmount),
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                const Spacer(),
                Text(
                  formatMoney(partner.goalAmount),
                  style: const TextStyle(color: AppColors.textMuted),
                ),
              ],
            ),
            const SizedBox(height: 8),
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: LinearProgressIndicator(
                value: partner.progress,
                minHeight: 8,
                backgroundColor: AppColors.cream,
                color: AppColors.sage,
              ),
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                Text(
                  '${partner.progressPercent}% of goal',
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
                const Spacer(),
                const Row(
                  children: [
                    Icon(Icons.favorite, size: 14, color: AppColors.sage),
                    SizedBox(width: 4),
                    Text(
                      'Donate',
                      style: TextStyle(
                        color: AppColors.sage,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
