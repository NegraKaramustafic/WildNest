import 'package:ecommerce_mobile/models/activity.dart';
import 'package:ecommerce_mobile/utils/json_parsers.dart';

class RecommendationItem {
  final int activityId;
  final String name;
  final ActivityType type;
  final int? animalId;
  final String? animalName;
  final String? imageUrl;
  final double price;
  final double score;
  final double avgRating;
  final int reviewCount;
  final List<RecommendationReason> reasons;

  RecommendationItem({
    required this.activityId,
    required this.name,
    this.type = ActivityType.experience,
    this.animalId,
    this.animalName,
    this.imageUrl,
    this.price = 0,
    this.score = 0,
    this.avgRating = 0,
    this.reviewCount = 0,
    this.reasons = const [],
  });

  factory RecommendationItem.fromJson(Map<String, dynamic> json) {
    final rawReasons = json['reasons'];
    return RecommendationItem(
      activityId: asInt(json['activityId']),
      name: asString(json['name']),
      type: parseActivityType(json['type']),
      animalId: json['animalId'] == null ? null : asInt(json['animalId']),
      animalName: json['animalName'] as String?,
      imageUrl: json['imageUrl'] as String?,
      price: asDouble(json['price']),
      score: asDouble(json['score']),
      avgRating: asDouble(json['avgRating']),
      reviewCount: asInt(json['reviewCount']),
      reasons: rawReasons is List
          ? rawReasons
              .whereType<Map>()
              .map(
                (item) => RecommendationReason.fromJson(
                  Map<String, dynamic>.from(item),
                ),
              )
              .toList()
          : const [],
    );
  }

  String get typeLabel {
    switch (type) {
      case ActivityType.feeding:
        return 'Feeding';
      case ActivityType.safari:
        return 'Safari';
      case ActivityType.workshop:
        return 'Workshop';
      case ActivityType.experience:
        return 'Experience';
      case ActivityType.adventure:
        return 'Adventure';
    }
  }

  String? get primaryReason {
    for (final reason in reasons) {
      if (reason.text.trim().isNotEmpty) return reason.text.trim();
    }
    return null;
  }
}

class RecommendationReason {
  final String code;
  final String text;

  const RecommendationReason({this.code = '', this.text = ''});

  factory RecommendationReason.fromJson(Map<String, dynamic> json) {
    return RecommendationReason(
      code: asString(json['code']),
      text: asString(json['text']),
    );
  }
}
