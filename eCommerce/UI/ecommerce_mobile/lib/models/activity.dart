import 'package:ecommerce_mobile/utils/json_parsers.dart';

enum ActivityStatus { draft, active, inactive }

class Activity {
  final int id;
  final String name;
  final String? description;
  final int? animalId;
  final String? animalName;
  final int activityTypeId;
  final String activityTypeName;
  final Duration duration;
  final double price;
  final int capacity;
  final String? imageUrl;
  final ActivityStatus status;
  final DateTime? createdAt;

  Activity({
    required this.id,
    required this.name,
    this.description,
    this.animalId,
    this.animalName,
    this.activityTypeId = 0,
    this.activityTypeName = '',
    this.duration = Duration.zero,
    this.price = 0,
    this.capacity = 0,
    this.imageUrl,
    this.status = ActivityStatus.active,
    this.createdAt,
  });

  factory Activity.fromJson(Map<String, dynamic> json) {
    return Activity(
      id: asInt(json['id']),
      name: asString(json['name']),
      description: json['description'] as String?,
      animalId: json['animalId'] == null ? null : asInt(json['animalId']),
      animalName: json['animalName'] as String?,
      activityTypeId: asInt(json['activityTypeId']),
      activityTypeName: asString(json['activityTypeName']),
      duration: parseDuration(json['duration']),
      price: asDouble(json['price']),
      capacity: asInt(json['capacity']),
      imageUrl: json['imageUrl'] as String?,
      status: parseActivityStatus(json['status']),
      createdAt: parseDateTime(json['createdAt']),
    );
  }
}

ActivityStatus parseActivityStatus(dynamic value) {
  if (value is int) {
    switch (value) {
      case 0:
        return ActivityStatus.draft;
      case 2:
        return ActivityStatus.inactive;
      default:
        return ActivityStatus.active;
    }
  }
  switch (value?.toString().toLowerCase()) {
    case 'draft':
      return ActivityStatus.draft;
    case 'inactive':
      return ActivityStatus.inactive;
    default:
      return ActivityStatus.active;
  }
}
