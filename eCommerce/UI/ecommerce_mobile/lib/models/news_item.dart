import 'package:ecommerce_mobile/utils/json_parsers.dart';

class NewsItem {
  final int id;
  final String title;
  final String excerpt;
  final String text;
  final String? imageUrl;
  final DateTime publishedAt;
  final String authorName;

  const NewsItem({
    required this.id,
    required this.title,
    required this.publishedAt,
    this.excerpt = '',
    this.text = '',
    this.imageUrl,
    this.authorName = '',
  });

  String get summary {
    final shortText = excerpt.trim();
    if (shortText.isNotEmpty) return shortText;
    return text.trim();
  }

  factory NewsItem.fromJson(Map<String, dynamic> json) {
    final image = (json['imageUrl'] as String?)?.trim();
    return NewsItem(
      id: asInt(json['id']),
      title: asString(json['title']),
      excerpt: asString(json['excerpt']),
      text: asString(json['text']),
      imageUrl: image == null || image.isEmpty ? null : image,
      publishedAt: parseDateTime(json['publishedAt']) ?? DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
      authorName: asString(json['authorName']),
    );
  }
}
