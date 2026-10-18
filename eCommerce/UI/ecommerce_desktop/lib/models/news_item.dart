import 'package:ecommerce_desktop/utils/json_parsers.dart';

class NewsItem {
  final int id;
  final String title;
  final String text;
  final String? imageUrl;
  final DateTime publishedAt;
  final String authorName;
  final bool isPublished;

  const NewsItem({
    required this.id,
    required this.title,
    required this.text,
    required this.publishedAt,
    this.imageUrl,
    this.authorName = '',
    this.isPublished = true,
  });

  factory NewsItem.fromJson(Map<String, dynamic> json) {
    return NewsItem(
      id: asInt(json['id']),
      title: asString(json['title']),
      text: asString(json['text']),
      imageUrl: blankToNull(json['imageUrl'] as String?),
      publishedAt: parseDateTime(json['publishedAt']) ?? DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
      authorName: asString(json['authorName']),
      isPublished: json['isPublished'] as bool? ?? true,
    );
  }
}
