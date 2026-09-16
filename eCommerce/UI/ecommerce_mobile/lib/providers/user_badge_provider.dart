import 'package:ecommerce_mobile/models/user_badge.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class UserBadgeProvider extends BaseProvider<UserBadge> {
  UserBadgeProvider() : super('api/UserBadge');

  @override
  UserBadge fromJson(data) {
    return UserBadge.fromJson(data as Map<String, dynamic>);
  }
}
