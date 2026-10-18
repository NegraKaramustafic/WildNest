import 'dart:convert';

import 'package:ecommerce_desktop/config/api_config.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:jwt_decoder/jwt_decoder.dart';

class AuthProvider extends ChangeNotifier {
  static const _accessKey = 'wildnest_admin_access_token';
  static const _refreshKey = 'wildnest_admin_refresh_token';
  static const adminRole = 'Admin';

  /// Backend login is `AccessController` at `POST /Access/Login`.
  /// This API does not expose `POST /api/Auth/login`.
  static const loginPath = 'Access/Login';

  static AuthProvider? _instance;
  static AuthProvider? get instance => _instance;

  static String? _accessToken;
  static Map<String, dynamic>? _claims;

  static String? get accesstoken => _accessToken;

  final FlutterSecureStorage _storage;

  bool _initialized = false;
  bool _isAuthenticated = false;
  String? _refreshToken;
  String? _sessionNotice;
  bool _handlingUnauthorized = false;

  AuthProvider({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage() {
    _instance = this;
  }

  bool get isInitialized => _initialized;
  bool get isAuthenticated => _isAuthenticated;
  String? get sessionNotice => _sessionNotice;

  int? get userId {
    final raw = _claims?['Id'] ?? _claims?['id'];
    if (raw is int) return raw;
    return int.tryParse(raw?.toString() ?? '');
  }

  String? get displayName {
    final first = _claims?['FirstName'] ?? _claims?['firstName'];
    final last = _claims?['LastName'] ?? _claims?['lastName'];
    final name = '${first ?? ''} ${last ?? ''}'.trim();
    return name.isEmpty ? null : name;
  }

  static bool isAdminRole(Map<String, dynamic>? claims) {
    final role = claims?['Role'] ?? claims?['role'];
    return role?.toString() == adminRole;
  }

  Future<void> initialize() async {
    try {
      final token = await _storage.read(key: _accessKey);
      final refresh = await _storage.read(key: _refreshKey);
      if (token != null &&
          token.isNotEmpty &&
          !JwtDecoder.isExpired(token) &&
          isAdminRole(JwtDecoder.decode(token))) {
        _applyTokens(token, refresh);
        _isAuthenticated = true;
      } else {
        await _clearStoredTokens();
        _clearMemory();
      }
    } catch (_) {
      _clearMemory();
    } finally {
      _initialized = true;
      notifyListeners();
    }
  }

  Future<void> login(String username, String password) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$loginPath');
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
      throw ApiClientException('Sign-in succeeded, but no token was returned.');
    }

    Map<String, dynamic> claims;
    try {
      claims = JwtDecoder.decode(access);
    } catch (_) {
      throw ApiClientException('The token that was received is not valid.');
    }

    if (!isAdminRole(claims)) {
      _clearMemory();
      throw ApiClientException(
        'Only administrators can use this app.',
      );
    }

    _applyTokens(access, refresh);
    try {
      await _persistTokens();
    } catch (_) {
      _clearMemory();
      throw ApiClientException(
        'The token could not be saved to secure storage.',
      );
    }
    _isAuthenticated = true;
    _sessionNotice = null;
    notifyListeners();
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

    try {
      final decoded = jsonDecode(response.body);
      if (decoded is Map) {
        final seconds = decoded['cooldownSeconds'];
        if (seconds is num && seconds > 0) {
          return seconds.toInt();
        }
      }
    } catch (_) {}
    return 60;
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

  Future<void> logout() async {
    final token = _accessToken;
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

    _clearMemory();
    _isAuthenticated = false;
    await _clearStoredTokens();
    notifyListeners();
  }

  Future<void> handleUnauthorized() async {
    if (_handlingUnauthorized) {
      return;
    }
    _handlingUnauthorized = true;
    try {
      _sessionNotice = 'Your session has expired. Please sign in again.';
      _clearMemory();
      _isAuthenticated = false;
      await _clearStoredTokens();
      notifyListeners();
    } finally {
      _handlingUnauthorized = false;
    }
  }

  void clearSessionNotice() {
    if (_sessionNotice == null) return;
    _sessionNotice = null;
    notifyListeners();
  }

  void _applyTokens(String access, String? refresh) {
    _accessToken = access;
    _refreshToken = refresh;
    try {
      _claims = JwtDecoder.decode(access);
    } catch (_) {
      _claims = null;
    }
  }

  void _clearMemory() {
    _accessToken = null;
    _refreshToken = null;
    _claims = null;
    _isAuthenticated = false;
  }

  Future<void> _persistTokens() async {
    if (_accessToken != null) {
      await _storage.write(key: _accessKey, value: _accessToken);
    }
    if (_refreshToken != null) {
      await _storage.write(key: _refreshKey, value: _refreshToken);
    }
  }

  Future<void> _clearStoredTokens() async {
    try {
      await _storage.delete(key: _accessKey);
      await _storage.delete(key: _refreshKey);
    } catch (_) {}
  }

  @visibleForTesting
  static void debugReset() {
    _instance = null;
    _accessToken = null;
    _claims = null;
  }
}
