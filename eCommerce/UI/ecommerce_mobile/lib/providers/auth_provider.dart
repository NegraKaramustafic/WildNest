import 'dart:convert';

import 'package:ecommerce_mobile/config/api_config.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:jwt_decoder/jwt_decoder.dart';

class AuthProvider extends ChangeNotifier {
  static const _accessKey = 'wildnest_access_token';
  static const _refreshKey = 'wildnest_refresh_token';

  static AuthProvider? _instance;
  static AuthProvider? get instance => _instance;

  static String? _accesstoken;
  static Map<String, dynamic>? _accessTokenDecoded;

  static String? get accesstoken => _accesstoken;
  static Map<String, dynamic>? get accessTokenDecoded => _accessTokenDecoded;

  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  bool _initialized = false;
  bool _isAuthenticated = false;
  String? _refreshtoken;
  bool _handlingUnauthorized = false;

  AuthProvider() {
    _instance = this;
  }

  bool get isInitialized => _initialized;
  bool get isAuthenticated => _isAuthenticated;
  String? get refreshtoken => _refreshtoken;

  int? get currentUserId {
    final raw = _claim(const ['Id', 'id', 'nameid', 'sub']);
    if (raw == null) return null;
    return int.tryParse(raw);
  }

  /// JWT role claim. The API writes `Role`; some tokens use `role`.
  String? get role => _claim(const ['Role', 'role']);

  bool get isAdmin => role?.toLowerCase() == 'admin';

  /// Staff member name from the token, available after login and after [initialize].
  String? get displayName {
    final name = [
      _claim(const ['FirstName', 'firstName', 'given_name']),
      _claim(const ['LastName', 'lastName', 'family_name']),
    ].whereType<String>().join(' ').trim();
    return name.isEmpty ? null : name;
  }

  String? _claim(List<String> keys) {
    final decoded = _accessTokenDecoded;
    if (decoded == null) return null;
    for (final key in keys) {
      final raw = decoded[key];
      final value = raw is List && raw.isNotEmpty ? raw.first : raw;
      final text = value?.toString().trim();
      if (text != null && text.isNotEmpty) return text;
    }
    return null;
  }

  Future<void> initialize() async {
    try {
      final token = await _storage.read(key: _accessKey);
      final refresh = await _storage.read(key: _refreshKey);
      if (token != null && token.isNotEmpty && !JwtDecoder.isExpired(token)) {
        _applyTokens(token, refresh);
      } else {
        await _clearStoredTokens();
      }
    } catch (_) {
      _accesstoken = null;
      _refreshtoken = null;
      _accessTokenDecoded = null;
      _isAuthenticated = false;
    } finally {
      _initialized = true;
      notifyListeners();
    }
  }

  Future<void> login(String username, String password) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}Access/Login');
    final response = await http.post(
      uri,
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({'username': username, 'password': password}),
    );

    if (response.statusCode >= 299) {
      throw ApiClientException(
        ApiErrorParser.messageFromBody(response.body) ??
            (response.statusCode == 401
                ? 'Invalid username or password.'
                : 'Could not sign in. Please try again.'),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final access = (data['accesstoken'] ?? data['Accesstoken']) as String?;
    final refresh = (data['refreshtoken'] ?? data['Refreshtoken']) as String?;
    if (access == null || access.isEmpty) {
      throw ApiClientException('Sign-in succeeded but no token was returned.');
    }

    _applyTokens(access, refresh);
    await _persistTokens();
    notifyListeners();
  }

  Future<void> register({
    required String firstName,
    required String lastName,
    required String email,
    required String username,
    required String password,
    String? phoneNumber,
  }) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}Access/Register');
    final response = await http.post(
      uri,
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'username': username,
        'password': password,
        'phoneNumber': phoneNumber,
        'isActive': true,
      }),
    );

    if (response.statusCode >= 299) {
      throw ApiClientException(
        ApiErrorParser.messageFromBody(response.body) ??
            'Could not create the account. Please try again.',
      );
    }
  }

  Future<int> requestPasswordReset(String email) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}Access/ForgotPassword'),
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email}),
    );

    if (response.statusCode >= 299) {
      throw ApiClientException(
        ApiErrorParser.messageFromBody(response.body) ??
            'Could not send a reset code. Please try again.',
      );
    }

    return _cooldownSeconds(response.body);
  }

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
    required String confirmNewPassword,
  }) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}Access/ResetPassword'),
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({
        'email': email,
        'code': code,
        'newPassword': newPassword,
        'confirmNewPassword': confirmNewPassword,
      }),
    );

    if (response.statusCode >= 299) {
      throw ApiClientException(
        ApiErrorParser.messageFromBody(response.body) ??
            'Could not reset the password. Please try again.',
      );
    }
  }

  int _cooldownSeconds(String body) {
    try {
      final decoded = jsonDecode(body);
      if (decoded is Map) {
        final seconds = decoded['cooldownSeconds'];
        if (seconds is num && seconds > 0) {
          return seconds.toInt();
        }
      }
    } catch (_) {}
    return 60;
  }

  Future<void> logout() async {
    final token = _accesstoken;
    if (token != null && token.isNotEmpty) {
      try {
        await http.post(
          Uri.parse('${ApiConfig.baseUrl}Access/Logout'),
          headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer $token',
          },
        );
      } catch (_) {
        // Network failure must not block clearing the local session.
      }
    }

    _accesstoken = null;
    _refreshtoken = null;
    _accessTokenDecoded = null;
    _isAuthenticated = false;
    await _clearStoredTokens();
    notifyListeners();
  }

  Future<void> handleUnauthorized() async {
    if (_handlingUnauthorized || !_isAuthenticated) {
      return;
    }
    _handlingUnauthorized = true;
    try {
      await logout();
    } finally {
      _handlingUnauthorized = false;
    }
  }

  void _applyTokens(String access, String? refresh) {
    _accesstoken = access;
    _refreshtoken = refresh;
    _isAuthenticated = true;
    try {
      _accessTokenDecoded = JwtDecoder.decode(access);
    } catch (_) {
      _accessTokenDecoded = null;
    }
  }

  Future<void> _persistTokens() async {
    try {
      if (_accesstoken != null) {
        await _storage.write(key: _accessKey, value: _accesstoken);
      }
      if (_refreshtoken != null) {
        await _storage.write(key: _refreshKey, value: _refreshtoken);
      }
    } catch (_) {
      // Tokens remain in memory if secure storage is unavailable.
    }
  }

  Future<void> _clearStoredTokens() async {
    try {
      await _storage.delete(key: _accessKey);
      await _storage.delete(key: _refreshKey);
    } catch (_) {}
  }
}
