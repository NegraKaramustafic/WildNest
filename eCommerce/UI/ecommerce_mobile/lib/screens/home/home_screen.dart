import 'package:ecommerce_mobile/models/animal.dart';
import 'package:ecommerce_mobile/models/recommendation.dart';
import 'package:ecommerce_mobile/providers/animal_provider.dart';
import 'package:ecommerce_mobile/providers/recommendation_provider.dart';
import 'package:ecommerce_mobile/screens/activities/activities_list_screen.dart';
import 'package:ecommerce_mobile/screens/activities/activity_detail_screen.dart';
import 'package:ecommerce_mobile/screens/animals/animal_detail_screen.dart';
import 'package:ecommerce_mobile/screens/donations/donations_list_screen.dart';
import 'package:ecommerce_mobile/screens/tickets/buy_ticket_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/animal_visuals.dart';
import 'package:ecommerce_mobile/widgets/section_header.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.onOpenTab});

  final ValueChanged<int> onOpenTab;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  List<Animal> _animals = [];
  List<RecommendationItem> _recommended = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final animalProvider = context.read<AnimalProvider>();
      final recommendationProvider = context.read<RecommendationProvider>();

      final animalsResult = await animalProvider.get(
        filter: {'page': 1, 'pageSize': 20, 'includeTotalCount': false},
      );
      var recommended = <RecommendationItem>[];
      try {
        recommended = await recommendationProvider.getRecommended(limit: 4);
      } catch (_) {}

      if (!mounted) return;
      setState(() {
        _animals = (animalsResult.items ?? []).where((a) => a.isActive).toList();
        _recommended = recommended;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _loading = false);
      showAppError(context, e);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        children: [
          const WildNestHeader(),
          Expanded(
            child: RefreshIndicator(
              color: AppColors.sage,
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.only(bottom: 88),
                children: [
                  const Padding(
                    padding: EdgeInsets.fromLTRB(20, 8, 20, 0),
                    child: _HeroBanner(),
                  ),
                  const SizedBox(height: 18),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 20),
                    child: Row(
                      children: [
                        _QuickAction(
                          emoji: '🦁',
                          label: 'Animals',
                          onTap: () => widget.onOpenTab(1),
                        ),
                        const SizedBox(width: 10),
                        _QuickAction(
                          emoji: '🎟️',
                          label: 'Tickets',
                          onTap: () {
                            Navigator.of(context).push(
                              MaterialPageRoute(
                                builder: (_) => const BuyTicketScreen(),
                              ),
                            );
                          },
                        ),
                        const SizedBox(width: 10),
                        _QuickAction(
                          emoji: '💚',
                          label: 'Adopt',
                          onTap: () => widget.onOpenTab(2),
                        ),
                        const SizedBox(width: 10),
                        _QuickAction(
                          emoji: '🌿',
                          label: 'Donate',
                          onTap: () {
                            Navigator.of(context).push(
                              MaterialPageRoute(
                                settings: const RouteSettings(name: 'donations-list'),
                                builder: (_) => const DonationsListScreen(),
                              ),
                            );
                          },
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 18),
                  SectionHeader(
                    title: 'Featured animals',
                    onAction: () => widget.onOpenTab(1),
                  ),
                  SizedBox(
                    height: 210,
                    child: _loading && _animals.isEmpty
                        ? const Center(
                            child: CircularProgressIndicator(color: AppColors.sage),
                          )
                        : ListView.separated(
                            padding: const EdgeInsets.symmetric(horizontal: 20),
                            scrollDirection: Axis.horizontal,
                            itemCount: _animals.length,
                            separatorBuilder: (_, _) => const SizedBox(width: 12),
                            itemBuilder: (context, index) {
                              final animal = _animals[index];
                              return _FeaturedAnimalCard(
                                animal: animal,
                                onTap: () {
                                  Navigator.of(context).push(
                                    MaterialPageRoute(
                                      builder: (_) =>
                                          AnimalDetailScreen(animalId: animal.id),
                                    ),
                                  );
                                },
                              );
                            },
                          ),
                  ),
                  const SizedBox(height: 12),
                  SectionHeader(
                    title: 'Recommended for you',
                    onAction: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => const ActivitiesListScreen(),
                        ),
                      );
                    },
                  ),
                  if (_loading && _recommended.isEmpty)
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 24),
                      child: Center(
                        child: CircularProgressIndicator(color: AppColors.sage),
                      ),
                    )
                  else if (_recommended.isEmpty)
                    const Padding(
                      padding: EdgeInsets.fromLTRB(20, 0, 20, 16),
                      child: Text(
                        'No recommendations right now.',
                        style: TextStyle(color: AppColors.textMuted),
                      ),
                    )
                  else
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 20),
                      child: Column(
                        children: [
                          for (final item in _recommended) ...[
                            _RecommendedActivityTile(
                              item: item,
                              onTap: () {
                                Navigator.of(context).push(
                                  MaterialPageRoute(
                                    builder: (_) => ActivityDetailScreen(
                                      activityId: item.activityId,
                                    ),
                                  ),
                                );
                              },
                            ),
                            const SizedBox(height: 10),
                          ],
                        ],
                      ),
                    ),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
                    child: _DonateBanner(
                      onTap: () {
                        Navigator.of(context).push(
                          MaterialPageRoute(
                            settings: const RouteSettings(name: 'donations-list'),
                            builder: (_) => const DonationsListScreen(),
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _RecommendedActivityTile extends StatelessWidget {
  const _RecommendedActivityTile({required this.item, required this.onTap});

  final RecommendationItem item;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final reasons = item.reasons
        .map((reason) => reason.text.trim())
        .where((text) => text.isNotEmpty)
        .take(2)
        .join(' · ');
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: Ink(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(20),
        ),
        child: Row(
          children: [
            Container(
              width: 46,
              height: 46,
              decoration: BoxDecoration(
                color: AppColors.mint,
                borderRadius: BorderRadius.circular(14),
              ),
              child: const Icon(Icons.auto_awesome, color: AppColors.sage, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    reasons.isEmpty ? item.activityTypeName : reasons,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Text(
              formatMoney(item.price),
              style: const TextStyle(
                fontWeight: FontWeight.w700,
                color: AppColors.sage,
                fontSize: 16,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HeroBanner extends StatelessWidget {
  const _HeroBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 176,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(28),
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF67A56D), Color(0xFF2F6840), Color(0xFF1F4D32)],
        ),
      ),
      child: Stack(
        children: [
          const Positioned(right: 18, top: 18, child: Text('🦜', style: TextStyle(fontSize: 28))),
          const Positioned(right: 70, top: 8, child: Text('🦒', style: TextStyle(fontSize: 42))),
          const Positioned(right: 16, bottom: 18, child: Text('🐘', style: TextStyle(fontSize: 48))),
          Padding(
            padding: const EdgeInsets.fromLTRB(22, 28, 110, 22),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Discover and protect',
                  style: GoogleFonts.playfairDisplay(
                    fontSize: 28,
                    color: Colors.white,
                    fontWeight: FontWeight.w600,
                    height: 1.1,
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Explore animals, adopt virtually, and support wildlife conservation.',
                  style: TextStyle(color: Colors.white70, height: 1.35, fontSize: 13),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _QuickAction extends StatelessWidget {
  const _QuickAction({
    required this.emoji,
    required this.label,
    required this.onTap,
  });

  final String emoji;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(20),
        child: Ink(
          padding: const EdgeInsets.symmetric(vertical: 16),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(20),
          ),
          child: Column(
            children: [
              Text(emoji, style: const TextStyle(fontSize: 22)),
              const SizedBox(height: 8),
              Text(
                label,
                style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _FeaturedAnimalCard extends StatelessWidget {
  const _FeaturedAnimalCard({required this.animal, required this.onTap});

  final Animal animal;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(24),
      child: Ink(
        width: 148,
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(24),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Container(
                width: double.infinity,
                decoration: const BoxDecoration(
                  color: AppColors.mint,
                  borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
                ),
                child: Center(
                  child: Text(animalEmoji(animal), style: const TextStyle(fontSize: 48)),
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(14, 10, 14, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    animal.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                  ),
                  Text(
                    animal.species,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                  ),
                  if (animal.tag != AnimalTag.none) ...[
                    const SizedBox(height: 8),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: animal.tag == AnimalTag.adoptable
                            ? AppColors.mint
                            : AppColors.cream,
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Text(
                        animal.tagLabel,
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                          color: animal.tag == AnimalTag.adoptable
                              ? AppColors.sageDark
                              : AppColors.textMuted,
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DonateBanner extends StatelessWidget {
  const _DonateBanner({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(24),
      child: Ink(
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 18),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(24),
          gradient: const LinearGradient(
            colors: [Color(0xFF3E7A4C), Color(0xFF2F6840)],
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.favorite, color: Colors.white, size: 18),
                SizedBox(width: 8),
                Text(
                  'Support small zoos',
                  style: TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.w700,
                    fontSize: 18,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            const Text(
              'Your donation helps local sanctuaries care for rescued animals.',
              style: TextStyle(color: Colors.white70, height: 1.4),
            ),
            const SizedBox(height: 14),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(22),
              ),
              child: const Text(
                'Donate now',
                style: TextStyle(
                  color: AppColors.sageDark,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
