import 'dart:convert';

import 'package:ecommerce_desktop/models/ticket.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:http/http.dart' as http;

class TicketProvider extends BaseProvider<Ticket> {
  TicketProvider() : super('api/Ticket');

  @override
  Ticket fromJson(data) {
    return Ticket.fromJson(data as Map<String, dynamic>);
  }

  Future<Ticket> verifyManualCode(String code) async {
    final response = await http.post(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/verify'),
      headers: createHeaders(),
      body: jsonEncode({
        'code': code.trim(),
        'method': 'ManualCode',
      }),
    );

    if (response.statusCode == 404) {
      throw ApiClientException(
        ApiErrorParser.messageFromBody(response.body) ?? 'Ticket not found.',
      );
    }

    await validateResponse(response);
    return Ticket.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }
}
