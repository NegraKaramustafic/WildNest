const _months = [
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

String formatDate(DateTime date) {
  final day = date.day.toString().padLeft(2, '0');
  return '$day ${_months[date.month - 1]} ${date.year}';
}

String formatDateTime(DateTime? value) {
  if (value == null) return '—';
  final local = value.toLocal();
  final clock = formatClock(Duration(hours: local.hour, minutes: local.minute));
  return '${formatDate(local)}, $clock';
}

String formatKm(double? value) {
  if (value == null) return '—';
  if (value == value.roundToDouble()) {
    return '${_groupDigits(value.toInt().toString())} KM';
  }
  final parts = value.toStringAsFixed(2).split('.');
  return '${_groupDigits(parts[0])}.${parts[1]} KM';
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

String formatCount(num value) {
  final rounded = value.round();
  final grouped = _groupDigits(rounded.abs().toString());
  return rounded < 0 ? '-$grouped' : grouped;
}

String formatRating(double? value) {
  if (value == null) return '—';
  return value.toStringAsFixed(1);
}

String formatPercent(double value) {
  if (value == value.roundToDouble()) {
    return '${value.toInt()}%';
  }
  return '${value.toStringAsFixed(1)}%';
}

String formatMonth(int year, int month) {
  return '${_months[month - 1]} $year';
}

String formatAdoption(double? value) {
  if (value == null) return '—';
  if (value == value.roundToDouble()) {
    return value.toInt().toString();
  }
  return value.toStringAsFixed(2);
}

String formatApiDate(DateTime date) {
  final year = date.year.toString().padLeft(4, '0');
  final month = date.month.toString().padLeft(2, '0');
  final day = date.day.toString().padLeft(2, '0');
  return '$year-$month-$day';
}

String formatClock(Duration duration) {
  var hours = duration.inHours % 24;
  final minutes = duration.inMinutes.remainder(60).toString().padLeft(2, '0');
  final period = hours >= 12 ? 'PM' : 'AM';
  hours = hours % 12;
  if (hours == 0) hours = 12;
  return '$hours:$minutes $period';
}

String formatApiClock(Duration duration) {
  final hours = duration.inHours.toString().padLeft(2, '0');
  final minutes = duration.inMinutes.remainder(60).toString().padLeft(2, '0');
  final seconds = duration.inSeconds.remainder(60).toString().padLeft(2, '0');
  return '$hours:$minutes:$seconds';
}

String formatDurationLabel(Duration duration) {
  final minutes = duration.inMinutes;
  if (minutes < 60) return '$minutes min';
  final hours = minutes ~/ 60;
  final rest = minutes % 60;
  if (rest == 0) return hours == 1 ? '1 hour' : '$hours hours';
  final hourLabel = hours == 1 ? '1 hr' : '$hours hr';
  return '$hourLabel $rest min';
}

String minutesToTimeSpan(int totalMinutes) {
  final days = totalMinutes ~/ (24 * 60);
  final minutesInDay = totalMinutes % (24 * 60);
  final hours = minutesInDay ~/ 60;
  final minutes = minutesInDay % 60;
  final clock =
      '${hours.toString().padLeft(2, '0')}:${minutes.toString().padLeft(2, '0')}:00';
  if (days > 0) return '$days.$clock';
  return clock;
}
