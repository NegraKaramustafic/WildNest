import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:flutter/material.dart';

class StatusChip extends StatelessWidget {
  const StatusChip({
    super.key,
    required this.label,
    this.tone = StatusTone.neutral,
  });

  final String label;
  final StatusTone tone;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: tone.background,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w700,
          color: tone.foreground,
        ),
      ),
    );
  }
}

enum StatusTone { success, warning, danger, neutral }

extension on StatusTone {
  Color get background {
    switch (this) {
      case StatusTone.success:
        return AppColors.mint;
      case StatusTone.warning:
        return const Color(0xFFF7E7C8);
      case StatusTone.danger:
        return const Color(0xFFF8D7D7);
      case StatusTone.neutral:
        return AppColors.cream;
    }
  }

  Color get foreground {
    switch (this) {
      case StatusTone.success:
        return AppColors.sageDark;
      case StatusTone.warning:
        return const Color(0xFF8A5A12);
      case StatusTone.danger:
        return AppColors.heart;
      case StatusTone.neutral:
        return AppColors.textMuted;
    }
  }
}
