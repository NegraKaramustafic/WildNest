import 'package:ecommerce_desktop/utils/json_parsers.dart';

enum TicketStatus { active, used, cancelled, pendingPayment }

class Ticket {
  final int id;
  final int userId;
  final String userFullName;
  final DateTime visitDate;
  final int visitorCount;
  final double pricePerPerson;
  final double totalPrice;
  final String code;
  final TicketStatus status;
  final DateTime? verifiedAt;
  final String? verifiedByFullName;
  final String? warning;

  Ticket({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.visitDate,
    this.visitorCount = 1,
    this.pricePerPerson = 0,
    this.totalPrice = 0,
    this.code = '',
    this.status = TicketStatus.active,
    this.verifiedAt,
    this.verifiedByFullName,
    this.warning,
  });

  factory Ticket.fromJson(Map<String, dynamic> json) {
    return Ticket(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      visitDate: parseDateOnly(json['visitDate']),
      visitorCount: asInt(json['visitorCount'], fallback: 1),
      pricePerPerson: asDouble(json['pricePerPerson']),
      totalPrice: asDouble(json['totalPrice']),
      code: asString(json['code']),
      status: parseTicketStatus(json['status']),
      verifiedAt: parseDateTime(json['verifiedAt']),
      verifiedByFullName: json['verifiedByFullName'] as String?,
      warning: blankToNull(json['warning'] as String?),
    );
  }

  String get statusLabel {
    switch (status) {
      case TicketStatus.active:
        return 'Active';
      case TicketStatus.used:
        return 'Used';
      case TicketStatus.cancelled:
        return 'Cancelled';
      case TicketStatus.pendingPayment:
        return 'Pending payment';
    }
  }
}

TicketStatus parseTicketStatus(dynamic value) {
  if (value is int) {
    switch (value) {
      case 1:
        return TicketStatus.used;
      case 2:
        return TicketStatus.cancelled;
      case 3:
        return TicketStatus.pendingPayment;
      default:
        return TicketStatus.active;
    }
  }

  switch (value?.toString().toLowerCase()) {
    case 'used':
      return TicketStatus.used;
    case 'cancelled':
    case 'canceled':
      return TicketStatus.cancelled;
    case 'pendingpayment':
    case 'pending_payment':
    case 'pending':
      return TicketStatus.pendingPayment;
    default:
      return TicketStatus.active;
  }
}
