import 'package:ecommerce_mobile/utils/json_parsers.dart';

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
  final String qrCodeData;
  final TicketStatus status;
  final int? paymentId;
  final DateTime? createdAt;
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
    this.qrCodeData = '',
    this.status = TicketStatus.pendingPayment,
    this.paymentId,
    this.createdAt,
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
      qrCodeData: asString(json['qrCodeData']),
      status: parseTicketStatus(json['status']),
      paymentId: json['paymentId'] == null ? null : asInt(json['paymentId']),
      createdAt: parseDateTime(json['createdAt']),
      verifiedAt: parseDateTime(json['verifiedAt']),
      verifiedByFullName: json['verifiedByFullName'] as String?,
      warning: json['warning'] as String?,
    );
  }

  String get qrPayload => qrCodeData.isNotEmpty
      ? qrCodeData
      : (code.isNotEmpty ? code : 'WN-$id');

  bool get canCancel =>
      status == TicketStatus.active || status == TicketStatus.pendingPayment;

  String get statusLabel {
    switch (status) {
      case TicketStatus.active:
        return 'Active';
      case TicketStatus.used:
        return 'Used';
      case TicketStatus.cancelled:
        return 'Cancelled';
      case TicketStatus.pendingPayment:
        return 'Pending';
    }
  }
}

enum TicketVerificationMethod {
  manualCode,
  qrScan;

  /// Wire value for `POST api/Ticket/verify`.
  String get apiValue {
    switch (this) {
      case TicketVerificationMethod.manualCode:
        return 'ManualCode';
      case TicketVerificationMethod.qrScan:
        return 'QrScan';
    }
  }
}

/// Matches backend `ZooPricing.ZooEntryPricePerPerson` for local price preview.
const double zooEntryPricePerPerson = 10;

const Duration zooEntryOpenTime = Duration(hours: 10);
const Duration zooEntryCloseTime = Duration(hours: 18);

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
