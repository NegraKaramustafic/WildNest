import 'package:ecommerce_desktop/utils/json_parsers.dart';

class ZooPartner {
  final int id;
  final String name;
  final String? imageUrl;

  ZooPartner({required this.id, required this.name, this.imageUrl});

  factory ZooPartner.fromJson(Map<String, dynamic> json) {
    return ZooPartner(
      id: asInt(json['id']),
      name: asString(json['name']),
      imageUrl: blankToNull(json['imageUrl'] as String?),
    );
  }
}
