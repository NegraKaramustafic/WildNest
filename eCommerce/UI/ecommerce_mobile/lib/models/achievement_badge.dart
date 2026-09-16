import 'package:ecommerce_mobile/utils/json_parsers.dart';

enum BadgeCriteria {
  firstAdoption,
  firstBooking,
  threeDonations,
  tenAnimalVisits,
  fiveAdoptions,
}

class AchievementBadge {
  final int id;
  final String name;
  final String? description;
  final String? iconUrl;
  final BadgeCriteria criteria;

  AchievementBadge({
    required this.id,
    required this.name,
    this.description,
    this.iconUrl,
    this.criteria = BadgeCriteria.firstAdoption,
  });

  factory AchievementBadge.fromJson(Map<String, dynamic> json) {
    return AchievementBadge(
      id: asInt(json['id']),
      name: asString(json['name']),
      description: json['description'] as String?,
      iconUrl: json['iconUrl'] as String?,
      criteria: parseBadgeCriteria(json['criteria']),
    );
  }
}

BadgeCriteria parseBadgeCriteria(dynamic value) {
  if (value is int) {
    switch (value) {
      case 1:
        return BadgeCriteria.firstBooking;
      case 2:
        return BadgeCriteria.threeDonations;
      case 3:
        return BadgeCriteria.tenAnimalVisits;
      case 4:
        return BadgeCriteria.fiveAdoptions;
      default:
        return BadgeCriteria.firstAdoption;
    }
  }
  switch (value?.toString().toLowerCase()) {
    case 'firstbooking':
      return BadgeCriteria.firstBooking;
    case 'threedonations':
      return BadgeCriteria.threeDonations;
    case 'tenanimalvisits':
      return BadgeCriteria.tenAnimalVisits;
    case 'fiveadoptions':
      return BadgeCriteria.fiveAdoptions;
    default:
      return BadgeCriteria.firstAdoption;
  }
}
