class ApiConfig {
  static const String _raw = String.fromEnvironment('API_BASE_URL');

  static String get baseUrl {
    final value = _raw.trim();
    if (value.isEmpty) {
      throw StateError(
        'API_BASE_URL is not set. Run with --dart-define=API_BASE_URL=<host>/',
      );
    }
    return value.endsWith('/') ? value : '$value/';
  }
}
