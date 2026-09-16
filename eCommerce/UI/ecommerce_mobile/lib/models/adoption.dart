import 'package:ecommerce_mobile/utils/json_parsers.dart';

enum AdoptionStatus { active, cancelled, pendingPayment }

class Adoption {
  final int id;
  final int userId;
  final String userFullName;
  final int animalId;
  final String animalName;
  final double monthlyAmount;
  final AdoptionStatus status;
  final DateTime startDate;
  final int? paymentId;

  Adoption({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.animalId,
    this.animalName = '',
    this.monthlyAmount = 0,
    this.status = AdoptionStatus.pendingPayment,
    required this.startDate,
    this.paymentId,
  });

  factory Adoption.fromJson(Map<String, dynamic> json) {
    return Adoption(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      animalId: asInt(json['animalId']),
      animalName: asString(json['animalName']),
      monthlyAmount: asDouble(json['monthlyAmount']),
      status: parseAdoptionStatus(json['status']),
      startDate: parseDateTime(json['startDate']) ?? DateTime.now(),
      paymentId: json['paymentId'] == null ? null : asInt(json['paymentId']),
    );
  }

  String get statusLabel {
    switch (status) {
      case AdoptionStatus.active:
        return 'Active';
      case AdoptionStatus.cancelled:
        return 'Cancelled';
      case AdoptionStatus.pendingPayment:
        return 'Pending';
    }
  }
}

AdoptionStatus parseAdoptionStatus(dynamic value) {
  if (value is int) {
    switch (value) {
      case 1:
        return AdoptionStatus.cancelled;
      case 2:
        return AdoptionStatus.pendingPayment;
      default:
        return AdoptionStatus.active;
    }
  }
  switch (value?.toString().toLowerCase()) {
    case 'cancelled':
    case 'canceled':
      return AdoptionStatus.cancelled;
    case 'pendingpayment':
    case 'pending_payment':
    case 'pending':
      return AdoptionStatus.pendingPayment;
    default:
      return AdoptionStatus.active;
  }
}
