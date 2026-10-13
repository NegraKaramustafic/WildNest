import 'package:ecommerce_desktop/utils/json_parsers.dart';

enum ActivityType { feeding, safari, workshop, experience, adventure }

enum ActivityStatus { draft, active, inactive }

class ActivityActions {
  static const update = 'UpdateAsync';
  static const activate = 'ActivateAsync';
  static const deactivate = 'DeactivateAsync';
  static const delete = 'DeleteAsync';
}

class Activity {
  final int id;
  final String name;
  final String? description;
  final int? animalId;
  final String? animalName;
  final ActivityType type;
  final Duration duration;
  final double price;
  final int capacity;
  final String? imageUrl;
  final ActivityStatus status;
  final DateTime? createdAt;
  final List<String> allowedActions;

  Activity({
    required this.id,
    required this.name,
    this.description,
    this.animalId,
    this.animalName,
    this.type = ActivityType.experience,
    this.duration = Duration.zero,
    this.price = 0,
    this.capacity = 0,
    this.imageUrl,
    this.status = ActivityStatus.draft,
    this.createdAt,
    this.allowedActions = const [],
  });

  bool allows(String action) => allowedActions.contains(action);

  factory Activity.fromJson(Map<String, dynamic> json) {
    return Activity(
      id: asInt(json['id']),
      name: asString(json['name']),
      description: json['description'] as String?,
      animalId: json['animalId'] == null ? null : asInt(json['animalId']),
      animalName: json['animalName'] as String?,
      type: parseActivityType(json['type']),
      duration: parseDuration(json['duration']),
      price: asDouble(json['price']),
      capacity: asInt(json['capacity']),
      imageUrl: json['imageUrl'] as String?,
      status: parseActivityStatus(json['status']),
      createdAt: parseDateTime(json['createdAt']),
      allowedActions: _stringList(json['allowedActions']),
    );
  }
}

List<String> _stringList(dynamic value) {
  if (value is! List) return const [];
  return value.map((item) => item.toString()).toList();
}

ActivityType parseActivityType(dynamic value) {
  if (value is int) {
    switch (value) {
      case 0:
        return ActivityType.feeding;
      case 1:
        return ActivityType.safari;
      case 2:
        return ActivityType.workshop;
      case 3:
        return ActivityType.experience;
      case 4:
        return ActivityType.adventure;
    }
  }

  switch (value?.toString().toLowerCase()) {
    case 'feeding':
      return ActivityType.feeding;
    case 'safari':
      return ActivityType.safari;
    case 'workshop':
      return ActivityType.workshop;
    case 'adventure':
      return ActivityType.adventure;
    default:
      return ActivityType.experience;
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

String activityTypeApiValue(ActivityType type) {
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

String activityTypeLabel(ActivityType type) {
  switch (type) {
    case ActivityType.feeding:
      return 'Hranjenje';
    case ActivityType.safari:
      return 'Safari';
    case ActivityType.workshop:
      return 'Radionica';
    case ActivityType.experience:
      return 'Iskustvo';
    case ActivityType.adventure:
      return 'Avantura';
  }
}

String activityStatusApiValue(ActivityStatus status) {
  switch (status) {
    case ActivityStatus.draft:
      return 'Draft';
    case ActivityStatus.active:
      return 'Active';
    case ActivityStatus.inactive:
      return 'Inactive';
  }
}

String activityStatusLabel(ActivityStatus status) {
  switch (status) {
    case ActivityStatus.draft:
      return 'Nacrt';
    case ActivityStatus.active:
      return 'Aktivna';
    case ActivityStatus.inactive:
      return 'Neaktivna';
  }
}
