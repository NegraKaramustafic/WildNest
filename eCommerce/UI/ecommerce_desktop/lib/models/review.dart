import 'package:ecommerce_desktop/utils/json_parsers.dart';

class Review {
  final int id;
  final int userId;
  final String userFullName;
  final int activityId;
  final String activityName;
  final int rating;
  final String comment;
  final String? sentimentLabel;
  final double? sentimentScore;
  final DateTime? createdAt;

  Review({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.activityId,
    this.activityName = '',
    this.rating = 0,
    this.comment = '',
    this.sentimentLabel,
    this.sentimentScore,
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
      sentimentLabel: blankToNull(asString(json['sentimentLabel'])),
      sentimentScore: asDoubleOrNull(json['sentimentScore']),
      createdAt: parseDateTime(json['createdAt']),
    );
  }
}
