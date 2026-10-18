import 'package:ecommerce_mobile/models/adoption.dart';
import 'package:ecommerce_mobile/models/animal.dart';
import 'package:ecommerce_mobile/providers/adoption_provider.dart';
import 'package:ecommerce_mobile/providers/animal_provider.dart';
import 'package:ecommerce_mobile/screens/adopt/adoption_payment_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/animal_visuals.dart';
import 'package:ecommerce_mobile/widgets/async_body.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class AdoptScreen extends StatefulWidget {
  const AdoptScreen({super.key});

  @override
  State<AdoptScreen> createState() => _AdoptScreenState();
}

class _AdoptScreenState extends State<AdoptScreen> {
  List<Animal> _animals = [];
  final Map<int, Adoption> _mine = {};
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
      final animalProvider = context.read<AnimalProvider>();
      final adoptionProvider = context.read<AdoptionProvider>();
      final animalsResult = await animalProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      final adoptionsResult = await adoptionProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      final mine = <int, Adoption>{};
      for (final adoption in adoptionsResult.items ?? <Adoption>[]) {
        final current = mine[adoption.animalId];
        if (current == null ||
            adoption.status == AdoptionStatus.active ||
            (adoption.status == AdoptionStatus.pendingPayment &&
                current.status != AdoptionStatus.active)) {
          mine[adoption.animalId] = adoption;
        }
      }
      setState(() {
        _animals = (animalsResult.items ?? [])
            .where((animal) => animal.isActive && animal.adoptionPrice != null)
            .toList();
        _mine
          ..clear()
          ..addAll(mine);
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

  Future<void> _adopt(Animal animal) async {
    final existing = _mine[animal.id];
    if (existing?.status == AdoptionStatus.active || existing?.isPaid == true) {
      return;
    }
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => AdoptionPaymentScreen(
          animal: animal,
          existingAdoption: existing?.status == AdoptionStatus.pendingPayment
              ? existing
              : null,
        ),
      ),
    );
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const WildNestHeader(),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: Text(
              'Virtual Adoption',
              style: GoogleFonts.playfairDisplay(
                fontSize: 32,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          const Padding(
            padding: EdgeInsets.fromLTRB(20, 6, 20, 16),
            child: Text(
              'Give an animal a virtual home and support their care',
              style: TextStyle(color: AppColors.textMuted, fontSize: 15),
            ),
          ),
          Expanded(
            child: AsyncBody(
              loading: _loading,
              error: _error,
              onRetry: _load,
              isEmpty: _animals.isEmpty,
              emptyMessage: 'No animals are available for adoption right now.',
              child: ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
                itemCount: _animals.length,
                separatorBuilder: (_, _) => const SizedBox(height: 12),
                itemBuilder: (context, index) {
                  final animal = _animals[index];
                  return _AdoptCard(
                    animal: animal,
                    adoption: _mine[animal.id],
                    onAdopt: () => _adopt(animal),
                  );
                },
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _AdoptCard extends StatelessWidget {
  const _AdoptCard({
    required this.animal,
    required this.onAdopt,
    this.adoption,
  });

  final Animal animal;
  final Adoption? adoption;
  final VoidCallback onAdopt;

  @override
  Widget build(BuildContext context) {
    final alreadyAdopted =
        adoption?.status == AdoptionStatus.active || adoption?.isPaid == true;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(24),
      ),
      child: Row(
        children: [
          Container(
            width: 64,
            height: 64,
            decoration: BoxDecoration(
              color: AppColors.mint,
              borderRadius: BorderRadius.circular(18),
            ),
            child: Center(
              child: Text(animalEmoji(animal), style: const TextStyle(fontSize: 30)),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  animal.name,
                  style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                ),
                Text(
                  animal.species,
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
                const SizedBox(height: 4),
                Text(
                  '${formatMoney(animal.adoptionPrice ?? 0)}/month',
                  style: const TextStyle(
                    color: AppColors.sage,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
          ElevatedButton(
            onPressed: alreadyAdopted ? null : onAdopt,
            style: ElevatedButton.styleFrom(
              minimumSize: const Size(0, 44),
              padding: const EdgeInsets.symmetric(horizontal: 16),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  alreadyAdopted ? Icons.check_rounded : Icons.favorite,
                  size: 16,
                ),
                const SizedBox(width: 6),
                Text(alreadyAdopted ? 'Adopted' : 'Adopt'),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
