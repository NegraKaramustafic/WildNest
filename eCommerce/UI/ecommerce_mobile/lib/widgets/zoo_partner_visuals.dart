import 'package:ecommerce_mobile/models/zoo_partner.dart';

String zooPartnerEmoji(ZooPartner partner) {
  final haystack = '${partner.name} ${partner.location}'.toLowerCase();
  if (haystack.contains('sunbird') ||
      haystack.contains('bird') ||
      haystack.contains('parrot')) {
    return '🦜';
  }
  if (haystack.contains('mountain') || haystack.contains('nepal')) {
    return '⛰️';
  }
  if (haystack.contains('coral') ||
      haystack.contains('koala') ||
      haystack.contains('australia')) {
    return '🐨';
  }
  if (haystack.contains('ocean') || haystack.contains('coast')) {
    return '🌊';
  }
  return '🌿';
}
