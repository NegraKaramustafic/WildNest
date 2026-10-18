import 'package:ecommerce_desktop/utils/json_parsers.dart';

class UserBadge {
  final int id;
  final int userId;
  final int badgeId;
  final String userFullName;
  final String badgeName;
  final DateTime? earnedAt;

  const UserBadge({
    required this.id,
    required this.userId,
    required this.badgeId,
    this.userFullName = '',
    this.badgeName = '',
    this.earnedAt,
  });

  factory UserBadge.fromJson(Map<String, dynamic> json) {
    return UserBadge(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      badgeId: asInt(json['badgeId']),
      userFullName: asString(json['userFullName']),
      badgeName: asString(json['badgeName']),
      earnedAt: parseDateTime(json['earnedAt']),
    );
  }
}
