import 'package:ecommerce_mobile/utils/json_parsers.dart';

class UserBadge {
  final int id;
  final int userId;
  final int badgeId;
  final String badgeName;
  final String? badgeDescription;
  final String? badgeIconUrl;
  final DateTime earnedAt;

  UserBadge({
    required this.id,
    required this.userId,
    required this.badgeId,
    this.badgeName = '',
    this.badgeDescription,
    this.badgeIconUrl,
    required this.earnedAt,
  });

  factory UserBadge.fromJson(Map<String, dynamic> json) {
    return UserBadge(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      badgeId: asInt(json['badgeId']),
      badgeName: asString(json['badgeName']),
      badgeDescription: json['badgeDescription'] as String?,
      badgeIconUrl: json['badgeIconUrl'] as String?,
      earnedAt: parseDateTime(json['earnedAt']) ?? DateTime.now(),
    );
  }
}
