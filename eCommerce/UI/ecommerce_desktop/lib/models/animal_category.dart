import 'package:ecommerce_desktop/utils/json_parsers.dart';

class AnimalCategory {
  final int id;
  final String name;

  AnimalCategory({required this.id, required this.name});

  factory AnimalCategory.fromJson(Map<String, dynamic> json) {
    return AnimalCategory(
      id: asInt(json['id']),
      name: asString(json['name']),
    );
  }
}
