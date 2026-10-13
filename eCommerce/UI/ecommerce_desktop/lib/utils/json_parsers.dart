int asInt(dynamic value, {int fallback = 0}) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return int.tryParse(value?.toString() ?? '') ?? fallback;
}

double asDouble(dynamic value, {double fallback = 0}) {
  if (value is double) return value;
  if (value is num) return value.toDouble();
  return double.tryParse(value?.toString() ?? '') ?? fallback;
}

double? asDoubleOrNull(dynamic value) {
  if (value == null) return null;
  if (value is num) return value.toDouble();
  return double.tryParse(value.toString());
}

String asString(dynamic value, {String fallback = ''}) {
  if (value == null) return fallback;
  return value.toString();
}

String? blankToNull(String? value) {
  final trimmed = value?.trim();
  if (trimmed == null || trimmed.isEmpty) return null;
  return trimmed;
}

DateTime? parseDateTime(dynamic value) {
  if (value == null) return null;
  if (value is DateTime) return value;
  return DateTime.tryParse(value.toString());
}

DateTime parseDateOnly(dynamic value) {
  final parsed = parseDateTime(value);
  if (parsed == null) {
    return DateTime.fromMillisecondsSinceEpoch(0);
  }
  return DateTime(parsed.year, parsed.month, parsed.day);
}

/// Parses .NET `TimeSpan` / `TimeOnly` (`HH:mm:ss`, `d.HH:mm:ss`) or ISO-8601 (`PT1H30M`).
Duration parseDuration(dynamic value) {
  if (value == null) return Duration.zero;
  if (value is Duration) return value;
  if (value is num) {
    if (value.abs() > 1000000) {
      return Duration(microseconds: (value / 10).round());
    }
    return Duration(seconds: value.round());
  }

  final raw = value.toString().trim();
  if (raw.isEmpty) return Duration.zero;

  final iso = RegExp(
    r'^P(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?)?$',
    caseSensitive: false,
  ).firstMatch(raw);
  if (iso != null) {
    final days = int.tryParse(iso.group(1) ?? '') ?? 0;
    final hours = int.tryParse(iso.group(2) ?? '') ?? 0;
    final minutes = int.tryParse(iso.group(3) ?? '') ?? 0;
    final seconds = double.tryParse(iso.group(4) ?? '') ?? 0;
    return Duration(
      days: days,
      hours: hours,
      minutes: minutes,
      milliseconds: (seconds * 1000).round(),
    );
  }

  var days = 0;
  var timePart = raw;
  final dot = raw.indexOf('.');
  if (dot > 0 && raw.contains(':') && !raw.contains('T')) {
    final maybeDays = int.tryParse(raw.substring(0, dot));
    if (maybeDays != null) {
      days = maybeDays;
      timePart = raw.substring(dot + 1);
    }
  }

  final parts = timePart.split(':');
  if (parts.length >= 2) {
    final hours = int.tryParse(parts[0]) ?? 0;
    final minutes = int.tryParse(parts[1]) ?? 0;
    var seconds = 0;
    var millis = 0;
    if (parts.length >= 3) {
      final sec = double.tryParse(parts[2]) ?? 0;
      seconds = sec.floor();
      millis = ((sec - seconds) * 1000).round();
    }
    return Duration(
      days: days,
      hours: hours,
      minutes: minutes,
      seconds: seconds,
      milliseconds: millis,
    );
  }

  return Duration.zero;
}
