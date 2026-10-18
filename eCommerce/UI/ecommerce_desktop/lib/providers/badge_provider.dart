import 'package:ecommerce_desktop/models/badge.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class BadgeProvider extends BaseProvider<Badge> {
  BadgeProvider() : super('api/Badge');

  @override
  Badge fromJson(data) => Badge.fromJson(data as Map<String, dynamic>);
}
