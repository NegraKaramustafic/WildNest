import 'package:ecommerce_mobile/models/news_item.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class NewsProvider extends BaseProvider<NewsItem> {
  NewsProvider() : super('api/News/published');

  @override
  NewsItem fromJson(data) => NewsItem.fromJson(data as Map<String, dynamic>);
}
