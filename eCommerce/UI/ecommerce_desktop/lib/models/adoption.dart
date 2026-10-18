import 'package:ecommerce_desktop/models/payment_status.dart';
import 'package:ecommerce_desktop/utils/json_parsers.dart';

enum AdoptionStatus { active, cancelled, pendingPayment }

class Adoption {
  final int id;
  final int userId;
  final String userFullName;
  final int animalId;
  final String animalName;
  final String? animalImageUrl;
  final double monthlyAmount;
  final AdoptionStatus status;
  final DateTime startDate;
  final int? paymentId;
  final PaymentStatus? paymentStatus;
  final int? rejectedByUserId;
  final String? rejectedByFullName;
  final DateTime? rejectedAt;
  final String? rejectionReason;
  final List<String> allowedActions;

  Adoption({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.animalId,
    this.animalName = '',
    this.animalImageUrl,
    this.monthlyAmount = 0,
    this.status = AdoptionStatus.pendingPayment,
    required this.startDate,
    this.paymentId,
    this.paymentStatus,
    this.rejectedByUserId,
    this.rejectedByFullName,
    this.rejectedAt,
    this.rejectionReason,
    this.allowedActions = const [],
  });

  factory Adoption.fromJson(Map<String, dynamic> json) {
    return Adoption(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      animalId: asInt(json['animalId']),
      animalName: asString(json['animalName']),
      animalImageUrl: blankToNull(json['animalImageUrl'] as String?),
      monthlyAmount: asDouble(json['monthlyAmount']),
      status: parseAdoptionStatus(json['status']),
      startDate: parseDateTime(json['startDate']) ?? DateTime.now(),
      paymentId: json['paymentId'] == null ? null : asInt(json['paymentId']),
      paymentStatus: parsePaymentStatus(json['paymentStatus']),
      rejectedByUserId: json['rejectedByUserId'] == null ? null : asInt(json['rejectedByUserId']),
      rejectedByFullName: blankToNull(json['rejectedByFullName'] as String?),
      rejectedAt: parseDateTime(json['rejectedAt']),
      rejectionReason: blankToNull(json['rejectionReason'] as String?),
      allowedActions: (json['allowedActions'] as List?)?.map((item) => item.toString()).toList() ?? const [],
    );
  }

  bool get canReject =>
      allowedActions.any((action) => action.toLowerCase() == 'rejectasync');

  String get approveTooltip {
    switch (status) {
      case AdoptionStatus.pendingPayment:
        return 'This adoption cannot be approved by an administrator. It becomes active when payment is completed.';
      case AdoptionStatus.active:
        return 'This adoption is already active.';
      case AdoptionStatus.cancelled:
        return 'A cancelled adoption cannot be approved.';
    }
  }

  String get rejectTooltip {
    if (canReject) return 'Reject';
    return 'This adoption is already cancelled.';
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

String adoptionStatusLabel(AdoptionStatus status) {
  switch (status) {
    case AdoptionStatus.active:
      return 'Active';
    case AdoptionStatus.cancelled:
      return 'Cancelled';
    case AdoptionStatus.pendingPayment:
      return 'Pending payment';
  }
}

String adoptionStatusApiValue(AdoptionStatus status) {
  switch (status) {
    case AdoptionStatus.active:
      return 'Active';
    case AdoptionStatus.cancelled:
      return 'Cancelled';
    case AdoptionStatus.pendingPayment:
      return 'PendingPayment';
  }
}
