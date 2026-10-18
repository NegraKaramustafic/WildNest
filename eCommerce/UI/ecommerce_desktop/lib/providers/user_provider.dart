import 'dart:convert';

import 'package:ecommerce_desktop/models/app_user.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class UserProvider extends BaseProvider<AppUser> {
  UserProvider() : super('Users');

  @override
  AppUser fromJson(data) => AppUser.fromJson(data as Map<String, dynamic>);

  Future<void> changePassword(Map<String, dynamic> body) async {
    final response = await http.put(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/ChangePassword'),
      headers: createHeaders(),
      body: jsonEncode(body),
    );
    await validateResponse(response);
  }

  Future<AppUser> setActive(int id, bool isActive) async {
    final response = await http.put(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/active'),
      headers: createHeaders(),
      body: jsonEncode({'isActive': isActive}),
    );
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
