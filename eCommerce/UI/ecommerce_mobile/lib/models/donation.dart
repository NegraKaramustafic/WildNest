import 'package:ecommerce_mobile/utils/json_parsers.dart';

class Donation {
  final int id;
  final int userId;
  final String userFullName;
  final int zooPartnerId;
  final String zooPartnerName;
  final double amount;
  final int? paymentId;
  final DateTime createdAt;

  Donation({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.zooPartnerId,
    this.zooPartnerName = '',
    this.amount = 0,
    this.paymentId,
    required this.createdAt,
  });

  factory Donation.fromJson(Map<String, dynamic> json) {
    return Donation(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      zooPartnerId: asInt(json['zooPartnerId']),
      zooPartnerName: asString(json['zooPartnerName']),
      amount: asDouble(json['amount']),
      paymentId: json['paymentId'] == null ? null : asInt(json['paymentId']),
      createdAt: parseDateTime(json['createdAt']) ?? DateTime.now(),
    );
  }
}
