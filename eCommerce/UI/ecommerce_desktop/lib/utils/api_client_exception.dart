import 'dart:convert';

class ApiClientException implements Exception {
  ApiClientException(this.message);

  final String message;

  @override
  String toString() => message;
}

class SessionExpiredException extends ApiClientException {
  SessionExpiredException()
      : super('Sesija je istekla. Prijavite se ponovo.');
}

class ApiErrorParser {
  ApiErrorParser._();

  static String? messageFromBody(String body) {
    final trimmed = body.trim();
    if (trimmed.isEmpty) {
      return null;
    }
    try {
      final decoded = jsonDecode(trimmed);
      if (decoded is! Map) {
        return null;
      }
      final map = Map<String, dynamic>.from(decoded);

      final root = map['message'];
      if (root is String && root.trim().isNotEmpty) {
        return root.trim();
      }

      final errors = map['errors'];
      if (errors is Map) {
        final errMap = Map<String, dynamic>.from(errors);
        final parts = <String>[];

        void addKey(String key) {
          parts.addAll(_messagesFromValue(errMap[key]));
        }

        addKey('clientError');
        for (final key in errMap.keys) {
          if (key == 'clientError') {
            continue;
          }
          addKey(key);
        }

        final unique = parts.toSet().toList();
        if (unique.isNotEmpty) {
          return unique.join('\n');
        }
      }

      return null;
    } catch (_) {
      return null;
    }
  }

  static List<String> _messagesFromValue(dynamic value) {
    if (value is List) {
      return value
          .map((e) => e.toString().trim())
          .where((s) => s.isNotEmpty)
          .toList();
    }
    if (value is String) {
      final text = value.trim();
      return text.isEmpty ? [] : [text];
    }
    return [];
  }
}
