import 'dart:convert';

import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ReservationProvider extends BaseProvider<Reservation> {
  ReservationProvider() : super('api/Reservation');

  @override
  Reservation fromJson(data) {
    return Reservation.fromJson(data as Map<String, dynamic>);
  }

  Future<Reservation> createPending({
    required int scheduleId,
    required int guestCount,
  }) {
    return insert({
      'scheduleId': scheduleId,
      'guestCount': guestCount,
    });
  }

  Future<Reservation> cancel(int id) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/cancel');
    final response = await http.post(uri, headers: createHeaders());
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
