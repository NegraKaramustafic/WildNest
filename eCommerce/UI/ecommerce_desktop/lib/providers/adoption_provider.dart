import 'dart:convert';

import 'package:ecommerce_desktop/models/adoption.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class AdoptionProvider extends BaseProvider<Adoption> {
  AdoptionProvider() : super('api/Adoption');

  @override
  Adoption fromJson(data) => Adoption.fromJson(data as Map<String, dynamic>);

  Future<Adoption> reject(int id, String reason) async {
    final response = await http.post(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/reject'),
      headers: createHeaders(),
      body: jsonEncode({'reason': reason}),
    );
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
