import 'dart:convert';

import 'package:ecommerce_desktop/models/dashboard.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class DashboardProvider extends BaseProvider<DashboardSnapshot> {
  DashboardProvider() : super('api/Dashboard');

  @override
  DashboardSnapshot fromJson(data) {
    return DashboardSnapshot.fromJson(data as Map<String, dynamic>);
  }

  Future<DashboardSnapshot> load() async {
    final response = await http.get(
      Uri.parse('${BaseProvider.baseUrl}$endpoint'),
      headers: createHeaders(),
    );
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
