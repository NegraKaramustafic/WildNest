import 'dart:convert';

import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:http/http.dart' as http;

class TicketProvider extends BaseProvider<Ticket> {
  TicketProvider() : super('api/Ticket');

  @override
  Ticket fromJson(data) {
    return Ticket.fromJson(data as Map<String, dynamic>);
  }

  Future<Ticket> createPending({
    required DateTime visitDate,
    required int visitorCount,
  }) {
    return insert({
      'visitDate': formatDateApi(visitDate),
      'visitorCount': visitorCount,
    });
  }

  Future<Ticket> verify(
    String code, {
    required TicketVerificationMethod method,
  }) async {
    final response = await http.post(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/verify'),
      headers: createHeaders(),
      body: jsonEncode({
        'code': code.trim(),
        'method': method.apiValue,
      }),
    );

    if (response.statusCode == 404) {
      throw ApiClientException(
        ApiErrorParser.messageFromBody(response.body) ?? 'Ticket not found',
      );
    }

    await validateResponse(response);
    return Ticket.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<Ticket> cancel(int id) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/cancel');
    final response = await http.post(uri, headers: createHeaders());
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
