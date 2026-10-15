import 'package:ecommerce_desktop/utils/json_parsers.dart';

class ReviewSentimentStatistics {
  final int totalReviews;
  final int labeledReviews;
  final int unlabeledReviews;
  final double? overallAverageRating;
  final List<SentimentLabelStatistic> labels;
  final List<SentimentTrendPoint> monthlyTrend;
  final List<SentimentActivityBreakdown> activities;
  final List<SentimentAnimalBreakdown> animals;

  const ReviewSentimentStatistics({
    this.totalReviews = 0,
    this.labeledReviews = 0,
    this.unlabeledReviews = 0,
    this.overallAverageRating,
    this.labels = const [],
    this.monthlyTrend = const [],
    this.activities = const [],
    this.animals = const [],
  });

  factory ReviewSentimentStatistics.fromJson(Map<String, dynamic> json) {
    return ReviewSentimentStatistics(
      totalReviews: asInt(json['totalReviews']),
      labeledReviews: asInt(json['labeledReviews']),
      unlabeledReviews: asInt(json['unlabeledReviews']),
      overallAverageRating: asDoubleOrNull(json['overallAverageRating']),
      labels: _maps(json['labels']).map(SentimentLabelStatistic.fromJson).toList(),
      monthlyTrend: _maps(json['monthlyTrend']).map(SentimentTrendPoint.fromJson).toList(),
      activities: _maps(json['activities']).map(SentimentActivityBreakdown.fromJson).toList(),
      animals: _maps(json['animals']).map(SentimentAnimalBreakdown.fromJson).toList(),
    );
  }

  SentimentLabelStatistic? label(String name) {
    for (final item in labels) {
      if (item.label == name) return item;
    }
    return null;
  }
}

class SentimentLabelStatistic {
  final String label;
  final int count;
  final double percentage;
  final double? averageRating;

  const SentimentLabelStatistic({
    required this.label,
    required this.count,
    required this.percentage,
    this.averageRating,
  });

  factory SentimentLabelStatistic.fromJson(Map<String, dynamic> json) {
    return SentimentLabelStatistic(
      label: asString(json['label']),
      count: asInt(json['count']),
      percentage: asDouble(json['percentage']),
      averageRating: asDoubleOrNull(json['averageRating']),
    );
  }
}

class SentimentTrendPoint {
  final int year;
  final int month;
  final int positiveCount;
  final int neutralCount;
  final int negativeCount;

  const SentimentTrendPoint({
    required this.year,
    required this.month,
    required this.positiveCount,
    required this.neutralCount,
    required this.negativeCount,
  });

  factory SentimentTrendPoint.fromJson(Map<String, dynamic> json) {
    return SentimentTrendPoint(
      year: asInt(json['year']),
      month: asInt(json['month']),
      positiveCount: asInt(json['positiveCount']),
      neutralCount: asInt(json['neutralCount']),
      negativeCount: asInt(json['negativeCount']),
    );
  }
}

class SentimentActivityBreakdown {
  final int activityId;
  final String activityName;
  final int positiveCount;
  final int neutralCount;
  final int negativeCount;
  final double? averageRating;

  const SentimentActivityBreakdown({
    required this.activityId,
    required this.activityName,
    required this.positiveCount,
    required this.neutralCount,
    required this.negativeCount,
    this.averageRating,
  });

  factory SentimentActivityBreakdown.fromJson(Map<String, dynamic> json) {
    return SentimentActivityBreakdown(
      activityId: asInt(json['activityId']),
      activityName: asString(json['activityName']),
      positiveCount: asInt(json['positiveCount']),
      neutralCount: asInt(json['neutralCount']),
      negativeCount: asInt(json['negativeCount']),
      averageRating: asDoubleOrNull(json['averageRating']),
    );
  }
}

class SentimentAnimalBreakdown {
  final int animalId;
  final String animalName;
  final int positiveCount;
  final int neutralCount;
  final int negativeCount;

  const SentimentAnimalBreakdown({
    required this.animalId,
    required this.animalName,
    required this.positiveCount,
    required this.neutralCount,
    required this.negativeCount,
  });

  factory SentimentAnimalBreakdown.fromJson(Map<String, dynamic> json) {
    return SentimentAnimalBreakdown(
      animalId: asInt(json['animalId']),
      animalName: asString(json['animalName']),
      positiveCount: asInt(json['positiveCount']),
      neutralCount: asInt(json['neutralCount']),
      negativeCount: asInt(json['negativeCount']),
    );
  }
}

List<Map<String, dynamic>> _maps(dynamic value) {
  if (value is! List) return const [];
  return value
      .whereType<Map>()
      .map((item) => Map<String, dynamic>.from(item))
      .toList();
}
