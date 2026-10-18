import 'package:ecommerce_desktop/models/user_badge.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class UserBadgeProvider extends BaseProvider<UserBadge> {
  UserBadgeProvider() : super('api/UserBadge');

  @override
  UserBadge fromJson(data) => UserBadge.fromJson(data as Map<String, dynamic>);
}
