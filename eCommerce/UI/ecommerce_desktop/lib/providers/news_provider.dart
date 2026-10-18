import 'package:ecommerce_desktop/models/news_item.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class NewsProvider extends BaseProvider<NewsItem> {
  NewsProvider() : super('api/News');

  @override
  NewsItem fromJson(data) => NewsItem.fromJson(data as Map<String, dynamic>);
}
