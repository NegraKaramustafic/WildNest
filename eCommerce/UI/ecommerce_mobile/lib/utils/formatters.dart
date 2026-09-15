import 'package:flutter/material.dart';

String formatMoney(num value, {bool decimals = false}) {
  final amount = value.toDouble();
  if (!decimals && amount == amount.roundToDouble()) {
    return '\$${amount.round()}';
  }
  return '\$${amount.toStringAsFixed(2)}';
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
