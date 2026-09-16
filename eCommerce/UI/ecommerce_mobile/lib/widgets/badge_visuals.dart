import 'package:ecommerce_mobile/models/achievement_badge.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:flutter/material.dart';

IconData badgeIconFor({
  BadgeCriteria? criteria,
  String? name,
}) {
  final haystack = name?.toLowerCase() ?? '';
  if (haystack.contains('guardian') || haystack.contains('shield')) {
    return Icons.shield_rounded;
  }
  if (haystack.contains('explorer') || haystack.contains('compass')) {
    return Icons.explore_rounded;
  }
  if (haystack.contains('heart') || haystack.contains('generous')) {
    return Icons.favorite_rounded;
  }
  if (haystack.contains('whisperer') || haystack.contains('paw')) {
    return Icons.pets_rounded;
  }
  if (haystack.contains('hero') || haystack.contains('conservation')) {
    return Icons.emoji_people_rounded;
  }

  switch (criteria) {
    case BadgeCriteria.firstAdoption:
      return Icons.shield_rounded;
    case BadgeCriteria.firstBooking:
      return Icons.explore_rounded;
    case BadgeCriteria.threeDonations:
      return Icons.favorite_rounded;
    case BadgeCriteria.tenAnimalVisits:
      return Icons.pets_rounded;
    case BadgeCriteria.fiveAdoptions:
      return Icons.emoji_people_rounded;
    case null:
      return Icons.workspace_premium_rounded;
  }
}

Color badgeAccentFor({BadgeCriteria? criteria, String? name}) {
  final haystack = name?.toLowerCase() ?? '';
  if (haystack.contains('heart') ||
      criteria == BadgeCriteria.threeDonations) {
    return AppColors.sage;
  }
  if (haystack.contains('explorer') ||
      criteria == BadgeCriteria.firstBooking) {
    return const Color(0xFFD4A017);
  }
  return AppColors.sage;
}
