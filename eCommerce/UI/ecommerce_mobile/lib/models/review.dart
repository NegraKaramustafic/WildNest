import 'package:ecommerce_mobile/utils/json_parsers.dart';

class Review {
  final int id;
  final int userId;
  final String userFullName;
  final int activityId;
  final String activityName;
  final int rating;
  final String comment;
  final DateTime? createdAt;

  Review({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.activityId,
    this.activityName = '',
    this.rating = 0,
    this.comment = '',
    this.createdAt,
  });

  factory Review.fromJson(Map<String, dynamic> json) {
    return Review(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      activityId: asInt(json['activityId']),
      activityName: asString(json['activityName']),
      rating: asInt(json['rating']),
      comment: asString(json['comment']),
      createdAt: parseDateTime(json['createdAt']),
    );
  }
}
