import 'dart:convert';

import 'package:ecommerce_mobile/models/recommendation.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class RecommendationProvider extends BaseProvider<RecommendationItem> {
  RecommendationProvider() : super('api/Recommendation');

  @override
  RecommendationItem fromJson(data) {
    return RecommendationItem.fromJson(data as Map<String, dynamic>);
  }

  Future<List<RecommendationItem>> getRecommended({int limit = 4}) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint').replace(
      queryParameters: {'limit': '$limit'},
    );
    final response = await http.get(uri, headers: createHeaders());
    await validateResponse(response);
    final data = jsonDecode(response.body);
    if (data is! Map) return const [];
    final items = data['items'];
    if (items is! List) return const [];
    return items
        .whereType<Map>()
        .map((item) => RecommendationItem.fromJson(Map<String, dynamic>.from(item)))
        .toList();
  }
}
