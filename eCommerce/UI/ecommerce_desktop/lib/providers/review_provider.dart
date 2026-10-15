import 'dart:convert';

import 'package:ecommerce_desktop/models/review.dart';
import 'package:ecommerce_desktop/models/review_sentiment_statistics.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ReviewProvider extends BaseProvider<Review> {
  ReviewProvider() : super('api/Review');

  @override
  Review fromJson(data) {
    return Review.fromJson(data as Map<String, dynamic>);
  }

  Future<ReviewSentimentStatistics> sentimentStatistics() async {
    final response = await http.get(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/sentiment-statistics'),
      headers: createHeaders(),
    );
    await validateResponse(response);
    return ReviewSentimentStatistics.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }
}
