import 'package:ecommerce_mobile/models/adoption.dart';
import 'package:ecommerce_mobile/models/animal.dart';
import 'package:ecommerce_mobile/models/payment.dart';
import 'package:ecommerce_mobile/models/user_badge.dart';
import 'package:ecommerce_mobile/providers/user_badge_provider.dart';
import 'package:ecommerce_mobile/screens/profile/my_badges_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/widgets/animal_visuals.dart';
import 'package:ecommerce_mobile/widgets/badge_visuals.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class AdoptionConfirmedScreen extends StatefulWidget {
  const AdoptionConfirmedScreen({
    super.key,
    required this.animal,
    required this.adoption,
    this.paymentStatus,
  });

  final Animal animal;
  final Adoption adoption;
  final PaymentStatus? paymentStatus;

  @override
  State<AdoptionConfirmedScreen> createState() => _AdoptionConfirmedScreenState();
}

class _AdoptionConfirmedScreenState extends State<AdoptionConfirmedScreen> {
  UserBadge? _badge;

  bool get _success =>
      widget.paymentStatus == PaymentStatus.completed ||
      widget.adoption.status == AdoptionStatus.active;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadBadge());
  }

  Future<void> _loadBadge() async {
    try {
      final result = await context.read<UserBadgeProvider>().get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      UserBadge? found;
      for (final badge in result.items ?? <UserBadge>[]) {
        final name = badge.badgeName.toLowerCase();
        if (name.contains('guardian') || name.contains('adoption')) {
          found = badge;
          break;
        }
      }
      setState(() => _badge = found);
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    final title = _success
        ? 'You adopted ${widget.animal.name}!'
        : 'Adoption pending';
    final message = _success
        ? _badge == null
            ? 'Thank you for supporting ${widget.animal.name}\'s care.'
            : 'You\'ve earned the "${_badge!.badgeName}" badge 🏅'
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
                    Text(
                      animalEmoji(widget.animal),
                      style: const TextStyle(fontSize: 64),
                    ),
                    const SizedBox(height: 16),
                    Container(
                      width: 72,
                      height: 72,
                      decoration: const BoxDecoration(
                        color: AppColors.mint,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        _success ? Icons.check_rounded : Icons.hourglass_top_rounded,
                        size: 40,
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
                    if (_success && _badge != null) ...[
                      const SizedBox(height: 22),
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.surface,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: Row(
                          children: [
                            Icon(
                              badgeIconFor(name: _badge!.badgeName),
                              color: const Color(0xFFD4A017),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    _badge!.badgeName,
                                    style: const TextStyle(fontWeight: FontWeight.w700),
                                  ),
                                  Text(
                                    _badge!.badgeDescription ??
                                        'Awarded for your first virtual adoption',
                                    style: const TextStyle(
                                      color: AppColors.textMuted,
                                      fontSize: 13,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                    const SizedBox(height: 28),
                    Row(
                      children: [
                        Expanded(
                          child: ElevatedButton(
                            onPressed: () {
                              Navigator.of(context).push(
                                MaterialPageRoute(
                                  builder: (_) => const MyBadgesScreen(),
                                ),
                              );
                            },
                            child: const FittedBox(child: Text('View Badges')),
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: OutlinedButton(
                            onPressed: () => Navigator.of(context).pop(),
                            child: const Text('Adopt Another'),
                          ),
                        ),
                      ],
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
