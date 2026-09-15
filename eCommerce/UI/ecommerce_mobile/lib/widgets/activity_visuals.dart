import 'package:ecommerce_mobile/models/activity.dart';

String activityEmoji(Activity activity) {
  final haystack =
      '${activity.name} ${activity.animalName ?? ''} ${activity.typeLabel}'
          .toLowerCase();

  if (haystack.contains('snorkel') ||
      haystack.contains('marine') ||
      haystack.contains('ocean')) {
    return '🤿';
  }
  if (haystack.contains('photo') || haystack.contains('camera')) return '📷';
  if (haystack.contains('nocturnal') || haystack.contains('night')) return '🌙';
  if (haystack.contains('elephant')) return '🐘';
  if (haystack.contains('red panda')) return '🐾';
  if (haystack.contains('panda')) return '🐼';
  if (haystack.contains('lemur') || haystack.contains('monkey')) return '🐒';
  if (haystack.contains('leopard')) return '🐆';
  if (haystack.contains('lion')) return '🦁';
  if (haystack.contains('macaw') ||
      haystack.contains('bird') ||
      haystack.contains('parrot')) {
    return '🦜';
  }
  if (haystack.contains('keeper')) return '🐼';

  switch (activity.type) {
    case ActivityType.feeding:
      return '🥬';
    case ActivityType.safari:
      return '🌙';
    case ActivityType.workshop:
      return '📷';
    case ActivityType.adventure:
      return '🧭';
    case ActivityType.experience:
      return '🌿';
  }
}
