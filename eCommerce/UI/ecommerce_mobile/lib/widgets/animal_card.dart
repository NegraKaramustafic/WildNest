import 'package:ecommerce_mobile/models/animal.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/widgets/animal_visuals.dart';
import 'package:flutter/material.dart';

class AnimalCard extends StatelessWidget {
  const AnimalCard({super.key, required this.animal, required this.onTap});

  final Animal animal;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(24),
      child: Ink(
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
                child: animal.imageUrl != null && animal.imageUrl!.isNotEmpty
                    ? ClipRRect(
                        borderRadius: const BorderRadius.vertical(
                          top: Radius.circular(24),
                        ),
                        child: Image.network(
                          animal.imageUrl!,
                          fit: BoxFit.cover,
                          errorBuilder: (_, _, _) => _Emoji(animal: animal),
                        ),
                      )
                    : _Emoji(animal: animal),
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
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    animal.species,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textMuted,
                    ),
                  ),
                  const SizedBox(height: 8),
                  SizedBox(
                    height: 24,
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      children: [
                        if (animal.tag != AnimalTag.none) ...[
                          _MiniChip(
                            label: animal.tagLabel,
                            emphasized: animal.tag == AnimalTag.adoptable,
                          ),
                          const SizedBox(width: 6),
                        ],
                        if (animal.habitat != null && animal.habitat!.isNotEmpty)
                          _MiniChip(label: animal.habitat!),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Emoji extends StatelessWidget {
  const _Emoji({required this.animal});

  final Animal animal;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Text(animalEmoji(animal), style: const TextStyle(fontSize: 56)),
    );
  }
}

class _MiniChip extends StatelessWidget {
  const _MiniChip({required this.label, this.emphasized = false});

  final String label;
  final bool emphasized;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: emphasized ? AppColors.mint : AppColors.cream,
        borderRadius: BorderRadius.circular(20),
        border: emphasized
            ? Border.all(color: AppColors.sage.withValues(alpha: 0.35))
            : null,
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w600,
          color: emphasized ? AppColors.sageDark : AppColors.textMuted,
        ),
      ),
    );
  }
}
