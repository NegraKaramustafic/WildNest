import 'package:ecommerce_mobile/models/achievement_badge.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class BadgeProvider extends BaseProvider<AchievementBadge> {
  BadgeProvider() : super('api/Badge');

  @override
  AchievementBadge fromJson(data) {
    return AchievementBadge.fromJson(data as Map<String, dynamic>);
  }
}
