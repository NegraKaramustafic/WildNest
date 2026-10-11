import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:flutter/material.dart';

class StatusChip extends StatelessWidget {
  const StatusChip({super.key, required this.active});

  final bool active;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: active ? AppColors.green : AppColors.chipInactive,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        active ? 'Aktivna' : 'Neaktivna',
        style: TextStyle(
          color: active ? Colors.white : AppColors.chipInactiveText,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
