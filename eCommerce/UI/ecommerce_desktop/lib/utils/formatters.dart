String formatDate(DateTime date) {
  final day = date.day.toString().padLeft(2, '0');
  final month = date.month.toString().padLeft(2, '0');
  return '$day.$month.${date.year}';
}

String formatKm(double? value) {
  if (value == null) return '—';
  if (value == value.roundToDouble()) {
    return '${value.toInt()} KM';
  }
  return '${value.toStringAsFixed(2)} KM';
}

String formatAdoption(double? value) {
  if (value == null) return '—';
  if (value == value.roundToDouble()) {
    return value.toInt().toString();
  }
  return value.toStringAsFixed(2);
}
