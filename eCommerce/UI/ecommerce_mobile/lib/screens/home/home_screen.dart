import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/widgets/coming_soon.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key, required this.onOpenTab});

  final ValueChanged<int> onOpenTab;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        children: [
          const WildNestHeader(),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 88),
              children: [
                Container(
                  padding: const EdgeInsets.all(22),
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(28),
                    gradient: const LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [Color(0xFF4C8A58), Color(0xFF2F6840)],
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Discover and protect',
                        style: GoogleFonts.playfairDisplay(
                          fontSize: 28,
                          color: Colors.white,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'Explore animals, adopt virtually, and support wildlife conservation.',
                        style: TextStyle(color: Colors.white70, height: 1.4),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 20),
                Row(
                  children: [
                    _QuickAction(
                      emoji: '🦁',
                      label: 'Animals',
                      onTap: () => onOpenTab(1),
                    ),
                    const SizedBox(width: 10),
                    const _QuickAction(emoji: '🎟️', label: 'Tickets'),
                    const SizedBox(width: 10),
                    _QuickAction(
                      emoji: '💚',
                      label: 'Adopt',
                      onTap: () => onOpenTab(2),
                    ),
                    const SizedBox(width: 10),
                    const _QuickAction(emoji: '🌿', label: 'Donate'),
                  ],
                ),
                const SizedBox(height: 28),
                const ComingSoonView(
                  title: 'Home feed coming soon',
                  message:
                      'Featured animals and upcoming activities will live here in a later session.',
                  emoji: '🏡',
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
  const _QuickAction({required this.emoji, required this.label, this.onTap});

  final String emoji;
  final String label;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: InkWell(
        onTap: onTap ??
            () {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text('$label is coming soon.')),
              );
            },
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
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
