import 'package:ecommerce_desktop/models/payment_status.dart';
import 'package:ecommerce_desktop/utils/json_parsers.dart';

enum ReservationStatus { pending, confirmed, completed, cancelled }

class Reservation {
  final int id;
  final int userId;
  final String userFullName;
  final int scheduleId;
  final String activityName;
  final String? activityImageUrl;
  final DateTime scheduleDate;
  final Duration scheduleStartTime;
  final int guestCount;
  final double totalPrice;
  final ReservationStatus status;
  final String code;
  final int? paymentId;
  final PaymentStatus? paymentStatus;
  final DateTime? createdAt;
  final int? confirmedByUserId;
  final String? confirmedByFullName;
  final DateTime? confirmedAt;
  final int? cancelledByUserId;
  final String? cancelledByFullName;
  final DateTime? cancelledAt;
  final String? cancellationReason;
  final List<String> allowedActions;

  Reservation({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.scheduleId,
    this.activityName = '',
    this.activityImageUrl,
    required this.scheduleDate,
    this.scheduleStartTime = Duration.zero,
    this.guestCount = 1,
    this.totalPrice = 0,
    this.status = ReservationStatus.pending,
    this.code = '',
    this.paymentId,
    this.paymentStatus,
    this.createdAt,
    this.confirmedByUserId,
    this.confirmedByFullName,
    this.confirmedAt,
    this.cancelledByUserId,
    this.cancelledByFullName,
    this.cancelledAt,
    this.cancellationReason,
    this.allowedActions = const [],
  });

  factory Reservation.fromJson(Map<String, dynamic> json) {
    return Reservation(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      scheduleId: asInt(json['scheduleId']),
      activityName: asString(json['activityName']),
      activityImageUrl: blankToNull(json['activityImageUrl'] as String?),
      scheduleDate: parseDateOnly(json['scheduleDate']),
      scheduleStartTime: parseDuration(json['scheduleStartTime']),
      guestCount: asInt(json['guestCount'], fallback: 1),
      totalPrice: asDouble(json['totalPrice']),
      status: parseReservationStatus(json['status']),
      code: asString(json['code']),
      paymentId: json['paymentId'] == null ? null : asInt(json['paymentId']),
      paymentStatus: parsePaymentStatus(json['paymentStatus']),
      createdAt: parseDateTime(json['createdAt']),
      confirmedByUserId: json['confirmedByUserId'] == null ? null : asInt(json['confirmedByUserId']),
      confirmedByFullName: blankToNull(json['confirmedByFullName'] as String?),
      confirmedAt: parseDateTime(json['confirmedAt']),
      cancelledByUserId: json['cancelledByUserId'] == null ? null : asInt(json['cancelledByUserId']),
      cancelledByFullName: blankToNull(json['cancelledByFullName'] as String?),
      cancelledAt: parseDateTime(json['cancelledAt']),
      cancellationReason: blankToNull(json['cancellationReason'] as String?),
      allowedActions: (json['allowedActions'] as List?)?.map((item) => item.toString()).toList() ?? const [],
    );
  }

  bool allows(String action) {
    return allowedActions.any((item) => item.toLowerCase() == action.toLowerCase());
  }

  bool get canConfirm =>
      allows('ConfirmAsync') && paymentStatus == PaymentStatus.completed;

  bool get canCancel => allows('CancelAsync');

  bool get canComplete => allows('CompleteAsync');

  String get confirmTooltip {
    if (canConfirm) return 'Confirm';
    if (status != ReservationStatus.pending) {
      return 'Only a pending reservation can be confirmed.';
    }
    return 'Reservation cannot be confirmed until payment is completed.';
  }

  String get cancelTooltip {
    if (canCancel) return 'Cancel';
    if (status == ReservationStatus.cancelled) {
      return 'This reservation is already cancelled.';
    }
    return 'A completed reservation cannot be cancelled.';
  }

  String get completeTooltip {
    if (canComplete) return 'Mark as completed';
    if (status == ReservationStatus.completed) {
      return 'This reservation is already completed.';
    }
    if (status == ReservationStatus.cancelled) {
      return 'A cancelled reservation cannot be completed.';
    }
    return 'Only a confirmed reservation can be completed.';
  }
}

ReservationStatus parseReservationStatus(dynamic value) {
  if (value is int) {
    switch (value) {
      case 1:
        return ReservationStatus.confirmed;
      case 2:
        return ReservationStatus.completed;
      case 3:
        return ReservationStatus.cancelled;
      default:
        return ReservationStatus.pending;
    }
  }
  switch (value?.toString().toLowerCase()) {
    case 'confirmed':
      return ReservationStatus.confirmed;
    case 'completed':
      return ReservationStatus.completed;
    case 'cancelled':
    case 'canceled':
      return ReservationStatus.cancelled;
    default:
      return ReservationStatus.pending;
  }
}

String reservationStatusLabel(ReservationStatus status) {
  switch (status) {
    case ReservationStatus.pending:
      return 'Pending';
    case ReservationStatus.confirmed:
      return 'Confirmed';
    case ReservationStatus.completed:
      return 'Completed';
    case ReservationStatus.cancelled:
      return 'Cancelled';
  }
}

String reservationStatusApiValue(ReservationStatus status) {
  switch (status) {
    case ReservationStatus.pending:
      return 'Pending';
    case ReservationStatus.confirmed:
      return 'Confirmed';
    case ReservationStatus.completed:
      return 'Completed';
    case ReservationStatus.cancelled:
      return 'Cancelled';
  }
}
