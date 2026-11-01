import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/widgets/authorized_image.dart';
import 'package:flutter/material.dart';

class ActivityVisual extends StatelessWidget {
  const ActivityVisual({
    super.key,
    required this.imageUrl,
    this.iconSize = 28,
  });

  final String? imageUrl;
  final double iconSize;

  @override
  Widget build(BuildContext context) {
    final url = imageUrl?.trim();
    if (url != null && url.isNotEmpty) {
      return AuthorizedImage(
        url: url,
        fit: BoxFit.cover,
        fallbackIcon: Icons.local_activity_outlined,
      );
    }
    return _fallback();
  }

  Widget _fallback() {
    return Center(
      child: Icon(Icons.local_activity_outlined, size: iconSize, color: AppColors.sage),
    );
  }
}
