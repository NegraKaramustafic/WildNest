import 'package:ecommerce_mobile/models/animal.dart';

String animalEmoji(Animal animal) {
  final haystack =
      '${animal.name} ${animal.species} ${animal.categoryName}'.toLowerCase();

  if (haystack.contains('elephant')) return '🐘';
  if (haystack.contains('red panda')) return '🐾';
  if (haystack.contains('lemur')) return '🐒';
  if (haystack.contains('leopard')) return '🐆';
  if (haystack.contains('panda')) return '🐼';
  if (haystack.contains('macaw') ||
      haystack.contains('parrot') ||
      haystack.contains('bird')) {
    return '🦜';
  }
  if (haystack.contains('lion')) return '🦁';
  if (haystack.contains('snake') || haystack.contains('reptile')) return '🐍';
  if (haystack.contains('primate') || haystack.contains('monkey')) return '🐒';
  return '🐾';
}

String formatWeight(double weight) {
  if (weight <= 0) return '—';
  if (weight == weight.roundToDouble()) {
    return '${_thousands(weight.round())} kg';
  }
  final text = weight.toStringAsFixed(weight < 10 ? 1 : 0);
  return '$text kg';
}

String _thousands(int value) {
  final raw = value.toString();
  final buffer = StringBuffer();
  for (var i = 0; i < raw.length; i++) {
    final remaining = raw.length - i;
    buffer.write(raw[i]);
    if (remaining > 1 && remaining % 3 == 1) {
      buffer.write('.');
    }
  }
  return buffer.toString();
}
