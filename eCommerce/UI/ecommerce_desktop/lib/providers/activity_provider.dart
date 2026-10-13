import 'dart:convert';

import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ActivityProvider extends BaseProvider<Activity> {
  ActivityProvider() : super('api/Activity');

  @override
  Activity fromJson(data) {
    return Activity.fromJson(data as Map<String, dynamic>);
  }

  Future<Activity> activate(int id) => _postAction(id, 'activate');

  Future<Activity> deactivate(int id) => _postAction(id, 'deactivate');

  Future<Activity> _postAction(int id, String action) async {
    final response = await http.post(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/$action'),
      headers: createHeaders(),
    );
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
