import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:flutter/material.dart';

class StatusChip extends StatelessWidget {
  const StatusChip({
    super.key,
    required this.active,
    this.label,
    this.background,
    this.foreground,
  });

  final bool active;
  final String? label;
  final Color? background;
  final Color? foreground;

  @override
  Widget build(BuildContext context) {
    final bg = background ?? (active ? AppColors.green : AppColors.chipInactive);
    final fg = foreground ?? (active ? Colors.white : AppColors.chipInactiveText);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label ?? (active ? 'Active' : 'Inactive'),
        style: TextStyle(
          color: fg,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
