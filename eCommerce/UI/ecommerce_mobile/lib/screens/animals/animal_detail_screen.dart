import 'package:ecommerce_mobile/models/animal.dart';
import 'package:ecommerce_mobile/providers/activity_provider.dart';
import 'package:ecommerce_mobile/providers/animal_provider.dart';
import 'package:ecommerce_mobile/screens/activities/activities_list_screen.dart';
import 'package:ecommerce_mobile/screens/activities/activity_detail_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/animal_visuals.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class AnimalDetailScreen extends StatefulWidget {
  const AnimalDetailScreen({super.key, required this.animalId});

  final int animalId;

  @override
  State<AnimalDetailScreen> createState() => _AnimalDetailScreenState();
}

class _AnimalDetailScreenState extends State<AnimalDetailScreen> {
  Animal? _animal;
  bool _loading = true;
  bool _favorite = false;
  bool _booking = false;
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
      final animal = await context.read<AnimalProvider>().getById(widget.animalId);
      if (!mounted) return;
      setState(() {
        _animal = animal;
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

  void _comingSoon(String action) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('$action is coming soon.')),
    );
  }

  Future<void> _bookActivity() async {
    final animal = _animal;
    if (animal == null || _booking) return;
    setState(() => _booking = true);
    try {
      final linked = await context.read<ActivityProvider>().findActiveForAnimal(
        animal.id,
      );
      if (!mounted) return;
      if (linked != null) {
        await Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => ActivityDetailScreen(activityId: linked.id),
          ),
        );
      } else {
        await Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const ActivitiesListScreen()),
        );
      }
    } catch (e) {
      if (mounted) showAppError(context, e);
    } finally {
      if (mounted) setState(() => _booking = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: _loading
            ? const Center(
                child: CircularProgressIndicator(color: AppColors.sage),
              )
            : _error != null && _animal == null
            ? _errorView()
            : _content(_animal!),
      ),
    );
  }

  Widget _errorView() {
    return Column(
      children: [
        WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
        Expanded(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(_error ?? 'Could not load this animal.'),
                  const SizedBox(height: 12),
                  ElevatedButton(onPressed: _load, child: const Text('Try again')),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _content(Animal animal) {
    return Column(
      children: [
        WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.only(bottom: 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(28),
                    child: Container(
                      height: 240,
                      width: double.infinity,
                      color: AppColors.mint,
                      child: animal.imageUrl != null && animal.imageUrl!.isNotEmpty
                          ? Image.network(
                              animal.imageUrl!,
                              fit: BoxFit.cover,
                              errorBuilder: (_, _, _) => Center(
                                child: Text(
                                  animalEmoji(animal),
                                  style: const TextStyle(fontSize: 96),
                                ),
                              ),
                            )
                          : Center(
                              child: Text(
                                animalEmoji(animal),
                                style: const TextStyle(fontSize: 96),
                              ),
                            ),
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(22, 22, 22, 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  animal.name,
                                  style: GoogleFonts.playfairDisplay(
                                    fontSize: 32,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  animal.species,
                                  style: const TextStyle(
                                    color: AppColors.textMuted,
                                    fontSize: 15,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          IconButton(
                            onPressed: () => setState(() => _favorite = !_favorite),
                            icon: Icon(
                              _favorite
                                  ? Icons.favorite
                                  : Icons.favorite_border,
                              color: AppColors.heart,
                              size: 28,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          if (animal.habitat != null && animal.habitat!.isNotEmpty)
                            _InfoChip(
                              icon: Icons.location_on_outlined,
                              label: animal.habitat!,
                            ),
                          if (animal.diet != null && animal.diet!.isNotEmpty)
                            _InfoChip(label: animal.diet!),
                        ],
                      ),
                      if (animal.description != null &&
                          animal.description!.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        Text(
                          animal.description!,
                          style: const TextStyle(
                            height: 1.45,
                            fontSize: 15,
                            color: AppColors.text,
                          ),
                        ),
                      ],
                      const SizedBox(height: 18),
                      Row(
                        children: [
                          Expanded(
                            child: _StatCard(
                              label: 'Weight',
                              value: formatWeight(animal.weight),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: _StatCard(
                              label: 'Lifespan',
                              value: animal.lifespan?.isNotEmpty == true
                                  ? animal.lifespan!
                                  : '—',
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: _StatCard(
                              label: 'Diet',
                              value: animal.diet?.isNotEmpty == true
                                  ? animal.diet!
                                  : '—',
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 22),
                      Row(
                        children: [
                          Expanded(
                            child: ElevatedButton(
                              onPressed: () => _comingSoon('Virtual adoption'),
                              child: const FittedBox(
                                child: Text('Adopt Virtually'),
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: OutlinedButton(
                              onPressed: _booking ? null : _bookActivity,
                              child: _booking
                                  ? const SizedBox(
                                      width: 22,
                                      height: 22,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                        color: AppColors.sage,
                                      ),
                                    )
                                  : const FittedBox(
                                      child: Text('Book Activity'),
                                    ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _InfoChip extends StatelessWidget {
  const _InfoChip({required this.label, this.icon});

  final String label;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
      decoration: BoxDecoration(
        color: AppColors.chip,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 14, color: AppColors.sage),
            const SizedBox(width: 4),
          ],
          Text(
            label,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppColors.sageDark,
            ),
          ),
        ],
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 14),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          Text(
            label,
            style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
          ),
          const SizedBox(height: 6),
          Text(
            value,
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}
