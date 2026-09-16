import 'package:ecommerce_mobile/models/achievement_badge.dart';
import 'package:ecommerce_mobile/models/user_badge.dart';
import 'package:ecommerce_mobile/providers/badge_provider.dart';
import 'package:ecommerce_mobile/providers/user_badge_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/async_body.dart';
import 'package:ecommerce_mobile/widgets/badge_visuals.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class MyBadgesScreen extends StatefulWidget {
  const MyBadgesScreen({super.key});

  @override
  State<MyBadgesScreen> createState() => _MyBadgesScreenState();
}

class _MyBadgesScreenState extends State<MyBadgesScreen> {
  List<AchievementBadge> _all = [];
  Set<int> _earnedIds = {};
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
      final badgeProvider = context.read<BadgeProvider>();
      final userBadgeProvider = context.read<UserBadgeProvider>();
      final all = await badgeProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      final mine = await userBadgeProvider.get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      setState(() {
        _all = all.items ?? [];
        _earnedIds = {
          for (final badge in mine.items ?? <UserBadge>[]) badge.badgeId,
        };
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
    final earned = _all.where((badge) => _earnedIds.contains(badge.id)).toList();
    final locked = _all.where((badge) => !_earnedIds.contains(badge.id)).toList();

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
                'My Badges',
                style: GoogleFonts.playfairDisplay(
                  fontSize: 32,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 6, 20, 12),
              child: Text(
                '${_earnedIds.length} of ${_all.length} earned',
                style: const TextStyle(color: AppColors.textMuted),
              ),
            ),
            Expanded(
              child: AsyncBody(
                loading: _loading,
                error: _error,
                onRetry: _load,
                isEmpty: _all.isEmpty,
                emptyMessage: 'No badges are configured yet.',
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
                  children: [
                    if (earned.isNotEmpty) ...[
                      const _GroupLabel(icon: Icons.lock_open_rounded, label: 'Earned'),
                      const SizedBox(height: 10),
                      _BadgeGrid(badges: earned, earned: true),
                      const SizedBox(height: 22),
                    ],
                    if (locked.isNotEmpty) ...[
                      const _GroupLabel(icon: Icons.lock_outline, label: 'Locked'),
                      const SizedBox(height: 10),
                      _BadgeGrid(badges: locked, earned: false),
                    ],
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

class _GroupLabel extends StatelessWidget {
  const _GroupLabel({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 18, color: AppColors.textMuted),
        const SizedBox(width: 6),
        Text(label, style: const TextStyle(fontWeight: FontWeight.w700)),
      ],
    );
  }
}

class _BadgeGrid extends StatelessWidget {
  const _BadgeGrid({
    required this.badges,
    required this.earned,
  });

  final List<AchievementBadge> badges;
  final bool earned;

  @override
  Widget build(BuildContext context) {
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: badges.length,
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        childAspectRatio: 0.92,
      ),
      itemBuilder: (context, index) {
        final badge = badges[index];
        final color = earned
            ? badgeAccentFor(criteria: badge.criteria, name: badge.name)
            : AppColors.sageMuted;
        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(24),
          ),
          child: Column(
            children: [
              Icon(
                badgeIconFor(criteria: badge.criteria, name: badge.name),
                size: 36,
                color: color,
              ),
              const SizedBox(height: 12),
              Text(
                badge.name,
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontWeight: FontWeight.w700,
                  color: earned ? AppColors.text : AppColors.textMuted,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                badge.description ?? '',
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
              ),
              const Spacer(),
              if (earned)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppColors.mint,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: const Text(
                    'Earned',
                    style: TextStyle(
                      color: AppColors.sageDark,
                      fontWeight: FontWeight.w700,
                      fontSize: 12,
                    ),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}
