import 'dart:convert';

import 'package:ecommerce_desktop/models/reservation.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ReservationProvider extends BaseProvider<Reservation> {
  ReservationProvider() : super('api/Reservation');

  @override
  Reservation fromJson(data) => Reservation.fromJson(data as Map<String, dynamic>);

  Future<Reservation> confirm(int id) => _post(id, 'confirm');

  Future<Reservation> complete(int id) => _post(id, 'complete');

  Future<Reservation> cancelWithReason(int id, String reason) {
    return _post(id, 'cancel-with-reason', body: {'reason': reason});
  }

  Future<Reservation> _post(int id, String action, {Map<String, dynamic>? body}) async {
    final response = await http.post(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/$action'),
      headers: createHeaders(),
      body: body == null ? null : jsonEncode(body),
    );
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
