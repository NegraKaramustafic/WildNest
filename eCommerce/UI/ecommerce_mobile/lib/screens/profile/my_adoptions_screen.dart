import 'package:ecommerce_mobile/models/adoption.dart';
import 'package:ecommerce_mobile/models/animal.dart';
import 'package:ecommerce_mobile/providers/adoption_provider.dart';
import 'package:ecommerce_mobile/screens/adopt/adoption_payment_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/async_body.dart';
import 'package:ecommerce_mobile/widgets/status_chip.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class MyAdoptionsScreen extends StatefulWidget {
  const MyAdoptionsScreen({super.key});

  @override
  State<MyAdoptionsScreen> createState() => _MyAdoptionsScreenState();
}

class _MyAdoptionsScreenState extends State<MyAdoptionsScreen> {
  List<Adoption> _items = [];
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
      final result = await context.read<AdoptionProvider>().get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      setState(() {
        _items = result.items ?? [];
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

  Future<void> _extend(Adoption adoption) async {
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => AdoptionPaymentScreen(
          animal: Animal(
            id: adoption.animalId,
            name: adoption.animalName,
            species: '',
            categoryId: 0,
            categoryName: '',
            adoptionPrice: adoption.monthlyAmount,
          ),
          existingAdoption: adoption,
        ),
      ),
    );
    if (mounted) _load();
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
                'My Adoptions',
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
                emptyMessage: 'You have not adopted any animals yet.',
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
                  itemCount: _items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (context, index) {
                    final adoption = _items[index];
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
                                  adoption.animalName,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 16,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  '${formatMoney(adoption.monthlyAmount)}/month · ${formatReviewDate(adoption.startDate)}',
                                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                                ),
                                if (adoption.validUntil != null) ...[
                                  const SizedBox(height: 4),
                                  Text(
                                    'Until ${formatReviewDate(adoption.validUntil!)}',
                                    style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                                  ),
                                ],
                                if (adoption.status == AdoptionStatus.active ||
                                    adoption.status == AdoptionStatus.expired)
                                  Align(
                                    alignment: Alignment.centerLeft,
                                    child: TextButton(
                                      onPressed: () => _extend(adoption),
                                      style: TextButton.styleFrom(
                                        padding: const EdgeInsets.symmetric(horizontal: 0),
                                        minimumSize: Size.zero,
                                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                      ),
                                      child: const Text('Extend'),
                                    ),
                                  ),
                              ],
                            ),
                          ),
                          StatusChip(
                            label: adoption.statusLabel,
                            tone: switch (adoption.status) {
                              AdoptionStatus.active => StatusTone.success,
                              AdoptionStatus.cancelled => StatusTone.danger,
                              AdoptionStatus.pendingPayment => StatusTone.warning,
                              AdoptionStatus.expired => StatusTone.neutral,
                            },
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
