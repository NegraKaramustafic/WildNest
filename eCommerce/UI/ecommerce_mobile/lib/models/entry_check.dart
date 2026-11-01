import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/utils/json_parsers.dart';

class ReservationCheck {
  const ReservationCheck({
    required this.code,
    required this.visitorName,
    required this.activityName,
    required this.scheduleDate,
    required this.guestCount,
    required this.total,
  });

  final String code;
  final String visitorName;
  final String activityName;
  final DateTime scheduleDate;
  final int guestCount;
  final double total;

  factory ReservationCheck.fromJson(Map<String, dynamic> json) {
    return ReservationCheck(
      code: asString(json['code']),
      visitorName: asString(json['visitorName']),
      activityName: asString(json['activityName']),
      scheduleDate: parseDateOnly(json['scheduleDate']),
      guestCount: asInt(json['guestCount']),
      total: asDouble(json['total']),
    );
  }
}

class EntryCheck {
  const EntryCheck({required this.kind, this.ticket, this.reservation});

  final String kind;
  final Ticket? ticket;
  final ReservationCheck? reservation;

  bool get isReservation => kind.toLowerCase() == 'reservation' && reservation != null;

  factory EntryCheck.fromJson(Map<String, dynamic> json) {
    final ticketJson = json['ticket'];
    final reservationJson = json['reservation'];
    return EntryCheck(
      kind: asString(json['kind']),
      ticket: ticketJson is Map<String, dynamic> ? Ticket.fromJson(ticketJson) : null,
      reservation: reservationJson is Map<String, dynamic>
          ? ReservationCheck.fromJson(reservationJson)
          : null,
    );
  }
}
