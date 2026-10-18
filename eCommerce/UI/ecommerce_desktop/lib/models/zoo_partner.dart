import 'package:ecommerce_desktop/utils/json_parsers.dart';

class ZooPartner {
  final int id;
  final String name;
  final String location;
  final String? description;
  final String? imageUrl;
  final double goalAmount;
  final double currentAmount;
  final bool isActive;
  final int donationCount;

  const ZooPartner({
    required this.id,
    required this.name,
    this.location = '',
    this.description,
    this.imageUrl,
    this.goalAmount = 0,
    this.currentAmount = 0,
    this.isActive = true,
    this.donationCount = 0,
  });

  bool get isInUse => donationCount > 0;

  factory ZooPartner.fromJson(Map<String, dynamic> json) {
    return ZooPartner(
      id: asInt(json['id']),
      name: asString(json['name']),
      location: asString(json['location']),
      description: blankToNull(json['description'] as String?),
      imageUrl: blankToNull(json['imageUrl'] as String?),
      goalAmount: asDouble(json['goalAmount']),
      currentAmount: asDouble(json['currentAmount']),
      isActive: json['isActive'] as bool? ?? true,
      donationCount: asInt(json['donationCount']),
    );
  }
}
