import 'package:ecommerce_mobile/utils/json_parsers.dart';

enum ReservationStatus { pending, confirmed, completed, cancelled }

class Reservation {
  final int id;
  final int userId;
  final String userFullName;
  final int scheduleId;
  final String activityName;
  final DateTime scheduleDate;
  final Duration scheduleStartTime;
  final int guestCount;
  final double totalPrice;
  final ReservationStatus status;
  final String code;
  final int? paymentId;
  final DateTime? createdAt;

  Reservation({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.scheduleId,
    this.activityName = '',
    required this.scheduleDate,
    this.scheduleStartTime = Duration.zero,
    this.guestCount = 1,
    this.totalPrice = 0,
    this.status = ReservationStatus.pending,
    this.code = '',
    this.paymentId,
    this.createdAt,
  });

  factory Reservation.fromJson(Map<String, dynamic> json) {
    return Reservation(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      scheduleId: asInt(json['scheduleId']),
      activityName: asString(json['activityName']),
      scheduleDate: parseDateOnly(json['scheduleDate']),
      scheduleStartTime: parseTimeOfDay(json['scheduleStartTime']),
      guestCount: asInt(json['guestCount'], fallback: 1),
      totalPrice: asDouble(json['totalPrice']),
      status: parseReservationStatus(json['status']),
      code: asString(json['code']),
      paymentId: json['paymentId'] == null ? null : asInt(json['paymentId']),
      createdAt: parseDateTime(json['createdAt']),
    );
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
