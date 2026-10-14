import 'package:ecommerce_desktop/utils/json_parsers.dart';

class ActivityType {
  final int id;
  final String name;
  final String? description;
  final int activityCount;

  ActivityType({
    required this.id,
    required this.name,
    this.description,
    this.activityCount = 0,
  });

  bool get isInUse => activityCount > 0;

  factory ActivityType.fromJson(Map<String, dynamic> json) {
    return ActivityType(
      id: asInt(json['id']),
      name: asString(json['name']),
      description: json['description'] as String?,
      activityCount: asInt(json['activityCount']),
    );
  }
}
