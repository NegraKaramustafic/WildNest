import 'package:ecommerce_mobile/utils/json_parsers.dart';

class ZooPartner {
  final int id;
  final String name;
  final String location;
  final String? description;
  final String? imageUrl;
  final double goalAmount;
  final double currentAmount;
  final bool isActive;

  ZooPartner({
    required this.id,
    required this.name,
    this.location = '',
    this.description,
    this.imageUrl,
    this.goalAmount = 0,
    this.currentAmount = 0,
    this.isActive = true,
  });

  factory ZooPartner.fromJson(Map<String, dynamic> json) {
    return ZooPartner(
      id: asInt(json['id']),
      name: asString(json['name']),
      location: asString(json['location']),
      description: json['description'] as String?,
      imageUrl: json['imageUrl'] as String?,
      goalAmount: asDouble(json['goalAmount']),
      currentAmount: asDouble(json['currentAmount']),
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  double get progress {
    if (goalAmount <= 0) return 0;
    final value = currentAmount / goalAmount;
    if (value < 0) return 0;
    if (value > 1) return 1;
    return value;
  }

  int get progressPercent => (progress * 100).round();

  String get displayDescription {
    final text = description?.trim() ?? '';
    if (text.isNotEmpty) return text;
    return 'Your donation helps $name rescue wildlife and provide food, shelter, and veterinary care.';
  }
}
