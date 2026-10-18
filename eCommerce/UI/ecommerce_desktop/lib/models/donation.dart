import 'package:ecommerce_desktop/models/payment_status.dart';
import 'package:ecommerce_desktop/utils/json_parsers.dart';

class Donation {
  final int id;
  final int userId;
  final String userFullName;
  final int zooPartnerId;
  final String zooPartnerName;
  final String? zooPartnerImageUrl;
  final double amount;
  final int? paymentId;
  final PaymentStatus? paymentStatus;
  final DateTime createdAt;

  Donation({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.zooPartnerId,
    this.zooPartnerName = '',
    this.zooPartnerImageUrl,
    this.amount = 0,
    this.paymentId,
    this.paymentStatus,
    required this.createdAt,
  });

  factory Donation.fromJson(Map<String, dynamic> json) {
    return Donation(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      zooPartnerId: asInt(json['zooPartnerId']),
      zooPartnerName: asString(json['zooPartnerName']),
      zooPartnerImageUrl: blankToNull(json['zooPartnerImageUrl'] as String?),
      amount: asDouble(json['amount']),
      paymentId: json['paymentId'] == null ? null : asInt(json['paymentId']),
      paymentStatus: parsePaymentStatus(json['paymentStatus']),
      createdAt: parseDateTime(json['createdAt']) ?? DateTime.now(),
    );
  }
}
