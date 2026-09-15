import 'package:ecommerce_mobile/utils/json_parsers.dart';

enum PaymentStatus { pending, completed, failed, refunded }

class Payment {
  final int id;
  final int? userId;
  final double amount;
  final PaymentStatus status;
  final String? relatedEntityType;
  final int? relatedEntityId;
  final String? stripeCheckoutSessionId;
  final DateTime? createdAt;

  Payment({
    required this.id,
    this.userId,
    this.amount = 0,
    this.status = PaymentStatus.pending,
    this.relatedEntityType,
    this.relatedEntityId,
    this.stripeCheckoutSessionId,
    this.createdAt,
  });

  factory Payment.fromJson(Map<String, dynamic> json) {
    return Payment(
      id: asInt(json['id'] ?? json['paymentId']),
      userId: json['userId'] == null ? null : asInt(json['userId']),
      amount: asDouble(json['amount']),
      status: parsePaymentStatus(json['status']),
      relatedEntityType: json['relatedEntityType'] as String?,
      relatedEntityId: json['relatedEntityId'] == null
          ? null
          : asInt(json['relatedEntityId']),
      stripeCheckoutSessionId: json['stripeCheckoutSessionId'] as String?,
      createdAt: parseDateTime(json['createdAt']),
    );
  }
}

class PaymentCheckout {
  final int paymentId;
  final double amount;
  final String currency;
  final PaymentStatus status;
  final String? sessionUrl;
  final String? stripeCheckoutSessionId;
  final String relatedEntityType;
  final int relatedEntityId;

  PaymentCheckout({
    required this.paymentId,
    this.amount = 0,
    this.currency = 'usd',
    this.status = PaymentStatus.pending,
    this.sessionUrl,
    this.stripeCheckoutSessionId,
    this.relatedEntityType = 'Reservation',
    this.relatedEntityId = 0,
  });

  factory PaymentCheckout.fromJson(Map<String, dynamic> json) {
    return PaymentCheckout(
      paymentId: asInt(json['paymentId']),
      amount: asDouble(json['amount']),
      currency: asString(json['currency'], fallback: 'usd'),
      status: parsePaymentStatus(json['status']),
      sessionUrl: json['sessionUrl'] as String?,
      stripeCheckoutSessionId: json['stripeCheckoutSessionId'] as String?,
      relatedEntityType: asString(
        json['relatedEntityType'],
        fallback: 'Reservation',
      ),
      relatedEntityId: asInt(json['relatedEntityId']),
    );
  }
}

PaymentStatus parsePaymentStatus(dynamic value) {
  if (value is int) {
    switch (value) {
      case 1:
        return PaymentStatus.completed;
      case 2:
        return PaymentStatus.failed;
      case 3:
        return PaymentStatus.refunded;
      default:
        return PaymentStatus.pending;
    }
  }
  switch (value?.toString().toLowerCase()) {
    case 'completed':
      return PaymentStatus.completed;
    case 'failed':
      return PaymentStatus.failed;
    case 'refunded':
      return PaymentStatus.refunded;
    default:
      return PaymentStatus.pending;
  }
}
