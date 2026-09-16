import 'dart:convert';

import 'package:ecommerce_mobile/models/adoption.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class AdoptionProvider extends BaseProvider<Adoption> {
  AdoptionProvider() : super('api/Adoption');

  @override
  Adoption fromJson(data) {
    return Adoption.fromJson(data as Map<String, dynamic>);
  }

  Future<Adoption> createPending(int animalId) {
    return insert({'animalId': animalId});
  }

  Future<Adoption?> findOpenForAnimal(int animalId) async {
    final result = await get(
      filter: {
        'page': 1,
        'pageSize': 20,
        'includeTotalCount': false,
        'AnimalId': animalId,
      },
    );
    final items = result.items ?? [];
    Adoption? pending;
    for (final adoption in items) {
      if (adoption.status == AdoptionStatus.active) return adoption;
      if (adoption.status == AdoptionStatus.pendingPayment) {
        pending ??= adoption;
      }
    }
    return pending;
  }

  Future<Adoption> cancel(int id) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/cancel');
    final response = await http.post(uri, headers: createHeaders());
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
