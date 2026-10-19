import 'package:ecommerce_desktop/utils/json_parsers.dart';

class Badge {
  final int id;
  final String name;
  final String? description;
  final String? iconUrl;
  final String criteria;
  final int holderCount;

  const Badge({
    required this.id,
    required this.name,
    this.description,
    this.iconUrl,
    this.criteria = '',
    this.holderCount = 0,
  });

  bool get isInUse => holderCount > 0;

  factory Badge.fromJson(Map<String, dynamic> json) {
    return Badge(
      id: asInt(json['id']),
      name: asString(json['name']),
      description: blankToNull(json['description'] as String?),
      iconUrl: blankToNull(json['iconUrl'] as String?),
      criteria: asString(json['criteria']),
      holderCount: asInt(json['holderCount']),
    );
  }
}

const badgeCriteriaValues = [
  'FirstAdoption',
  'FirstBooking',
  'ThreeDonations',
  'FiveAdoptions',
];

String badgeCriteriaLabel(String criteria) {
  switch (criteria) {
    case 'FirstAdoption':
      return 'First adoption';
    case 'FirstBooking':
      return 'First booking';
    case 'ThreeDonations':
      return 'Three donations';
    case 'FiveAdoptions':
      return 'Five adoptions';
    default:
      return criteria.isEmpty ? '—' : criteria;
  }
}

String badgeAwardRule(String criteria) {
  switch (criteria) {
    case 'FirstAdoption':
      return 'Awarded after the first completed adoption.';
    case 'FirstBooking':
      return 'Awarded after the first completed activity booking.';
    case 'ThreeDonations':
      return 'Awarded after three completed donations.';
    case 'FiveAdoptions':
      return 'Awarded after five completed adoptions.';
    default:
      return 'Award rule is not configured.';
  }
}
