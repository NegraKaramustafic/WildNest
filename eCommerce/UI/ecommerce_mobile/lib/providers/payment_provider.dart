import 'dart:convert';

import 'package:ecommerce_mobile/models/payment.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class PaymentProvider extends BaseProvider<Payment> {
  PaymentProvider() : super('api/Payment');

  @override
  Payment fromJson(data) {
    return Payment.fromJson(data as Map<String, dynamic>);
  }

  Future<PaymentCheckout> checkoutReservation(int reservationId) {
    return checkout(
      relatedEntityType: 'Reservation',
      relatedEntityId: reservationId,
    );
  }

  Future<PaymentCheckout> checkoutAdoption(int adoptionId) {
    return checkout(
      relatedEntityType: 'Adoption',
      relatedEntityId: adoptionId,
    );
  }

  Future<PaymentCheckout> checkoutDonation(int donationId) {
    return checkout(
      relatedEntityType: 'Donation',
      relatedEntityId: donationId,
    );
  }

  Future<PaymentCheckout> checkoutTicket(int ticketId) {
    return checkout(
      relatedEntityType: 'Ticket',
      relatedEntityId: ticketId,
    );
  }

  Future<PaymentCheckout> checkout({
    required String relatedEntityType,
    required int relatedEntityId,
  }) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/checkout');
    final response = await http.post(
      uri,
      headers: createHeaders(),
      body: jsonEncode({
        'relatedEntityType': relatedEntityType,
        'relatedEntityId': relatedEntityId,
      }),
    );
    await validateResponse(response);
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return PaymentCheckout.fromJson(data);
  }
}
