import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:flutter/material.dart';

class SentimentBadge extends StatelessWidget {
  const SentimentBadge({super.key, this.label, this.score});

  final String? label;
  final double? score;

  @override
  Widget build(BuildContext context) {
    final text = (label == null || label!.trim().isEmpty) ? 'Unlabeled' : label!.trim();
    final colors = sentimentColors(text);
    final tooltip = score == null
        ? text
        : '$text · ${(score! * 100).toStringAsFixed(0)}% confidence';
    return Tooltip(
      message: tooltip,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        decoration: BoxDecoration(
          color: colors.background,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: colors.border),
        ),
        child: Text(
          text,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: colors.foreground,
            fontSize: 12,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
    );
  }
}

class SentimentColors {
  const SentimentColors({
    required this.background,
    required this.foreground,
    required this.border,
    required this.chart,
  });

  final Color background;
  final Color foreground;
  final Color border;
  final Color chart;
}

SentimentColors sentimentColors(String? label) {
  switch (label) {
    case 'Positive':
      return const SentimentColors(
        background: AppColors.successBg,
        foreground: AppColors.successText,
        border: AppColors.successBorder,
        chart: AppColors.green,
      );
    case 'Negative':
      return const SentimentColors(
        background: AppColors.errorBg,
        foreground: AppColors.errorText,
        border: AppColors.errorBorder,
        chart: AppColors.danger,
      );
    case 'Neutral':
      return const SentimentColors(
        background: AppColors.warningBg,
        foreground: AppColors.warningText,
        border: AppColors.warningBorder,
        chart: Color(0xFFC48A2A),
      );
    default:
      return const SentimentColors(
        background: AppColors.background,
        foreground: AppColors.textMuted,
        border: AppColors.border,
        chart: AppColors.textMuted,
      );
  }
}
