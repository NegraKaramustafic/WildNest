import 'package:flutter/material.dart';

/// Euro amounts: €1,250.50. Stored numbers are not converted.
String formatMoney(num value) {
  final amount = value.toDouble();
  final negative = amount < 0;
  final parts = amount.abs().toStringAsFixed(2).split('.');
  final body = '${_groupDigits(parts[0])}.${parts[1]}';
  return negative ? '-€$body' : '€$body';
}

String _groupDigits(String digits) {
  final buffer = StringBuffer();
  for (var i = 0; i < digits.length; i++) {
    final remaining = digits.length - i;
    buffer.write(digits[i]);
    if (remaining > 1 && remaining % 3 == 1) {
      buffer.write(',');
    }
  }
  return buffer.toString();
}

String formatDurationHours(Duration duration) {
  final minutes = duration.inMinutes;
  if (minutes <= 0) return '—';
  if (minutes < 60) return '$minutes min';
  if (minutes % 60 == 0) {
    final hours = minutes ~/ 60;
    return hours == 1 ? '1 hour' : '$hours hours';
  }
  final hours = minutes / 60.0;
  final label = hours.toStringAsFixed(hours < 10 ? 1 : 0);
  return '$label hours';
}

String formatClock(Duration time) {
  var hours = time.inHours % 24;
  final minutes = time.inMinutes.remainder(60);
  final period = hours >= 12 ? 'PM' : 'AM';
  hours = hours % 12;
  if (hours == 0) hours = 12;
  final mm = minutes.toString().padLeft(2, '0');
  return '$hours:$mm $period';
}

String formatTimeRange(Duration start, Duration end) {
  return '${formatClock(start)} - ${formatClock(end)}';
}

String formatDateTime(DateTime value) {
  final local = value.toLocal();
  final clock = formatClock(Duration(hours: local.hour, minutes: local.minute));
  return '${formatReviewDate(local)}, $clock';
}

String formatReviewDate(DateTime date) {
  const months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];
  final local = date.toLocal();
  return '${local.day.toString().padLeft(2, '0')} ${months[local.month - 1]} ${local.year}';
}

String formatFriendlyDate(DateTime date) {
  final today = DateUtils.dateOnly(DateTime.now());
  final target = DateUtils.dateOnly(date);
  if (target == today) return 'Today';
  if (target == today.add(const Duration(days: 1))) return 'Tomorrow';
  if (target == today.subtract(const Duration(days: 1))) return 'Yesterday';
  const months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];
  return '${target.day} ${months[target.month - 1]} ${target.year}';
}

String abbreviatedReviewerName(String fullName) {
  final parts = fullName
      .trim()
      .split(RegExp(r'\s+'))
      .where((p) => p.isNotEmpty)
      .toList();
  if (parts.isEmpty) return 'Guest';
  if (parts.length == 1) return parts.first;
  final last = parts.last;
  final initial = last.isEmpty ? '' : '${last[0].toUpperCase()}.';
  return '${parts.first} $initial'.trim();
}

String initialsFor(String name) {
  final parts = name
      .trim()
      .split(RegExp(r'\s+'))
      .where((p) => p.isNotEmpty)
      .toList();
  if (parts.isEmpty) return 'G';
  if (parts.length == 1) return parts.first[0].toUpperCase();
  return (parts.first[0] + parts.last[0]).toUpperCase();
}

String guestLabel(int count) {
  return count == 1 ? '1 Adult' : '$count Adults';
}

String visitorLabel(int count) {
  return count == 1 ? '1 visitor' : '$count visitors';
}

String formatRelativeTime(DateTime date) {
  final diff = DateTime.now().difference(date.toLocal());
  if (diff.inMinutes < 1) return 'now';
  if (diff.inMinutes < 60) return '${diff.inMinutes}m';
  if (diff.inHours < 24) return '${diff.inHours}h';
  if (diff.inDays < 7) return '${diff.inDays}d';
  return formatReviewDate(date);
}

String spotsLeftLabel(int spots) {
  return spots == 1 ? '1 spot left' : '$spots spots left';
}

String formatDateApi(DateTime date) {
  final target = DateUtils.dateOnly(date);
  final mm = target.month.toString().padLeft(2, '0');
  final dd = target.day.toString().padLeft(2, '0');
  return '${target.year}-$mm-$dd';
}

String formatWeekdayShort(DateTime date) {
  const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  return days[date.weekday - 1];
}

String formatDayMonth(DateTime date) {
  const months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];
  final target = DateUtils.dateOnly(date);
  return '${target.day} ${months[target.month - 1]}';
}

String formatDateChipTitle(DateTime date) {
  final today = DateUtils.dateOnly(DateTime.now());
  final target = DateUtils.dateOnly(date);
  if (target == today) return 'Today';
  if (target == today.add(const Duration(days: 1))) return 'Tomorrow';
  return formatWeekdayShort(target);
}
