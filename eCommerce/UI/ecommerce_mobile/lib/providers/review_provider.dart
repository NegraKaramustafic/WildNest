import 'package:ecommerce_mobile/models/review.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class ReviewProvider extends BaseProvider<Review> {
  ReviewProvider() : super('api/Review');

  @override
  Review fromJson(data) {
    return Review.fromJson(data as Map<String, dynamic>);
  }

  Future<List<Review>> getForActivity(int activityId) async {
    final result = await get(
      filter: {
        'page': 1,
        'pageSize': 50,
        'includeTotalCount': true,
        'activityId': activityId,
      },
    );
    final items = result.items ?? [];
    items.sort((a, b) {
      final aDate = a.createdAt ?? DateTime.fromMillisecondsSinceEpoch(0);
      final bDate = b.createdAt ?? DateTime.fromMillisecondsSinceEpoch(0);
      return bDate.compareTo(aDate);
    });
    return items;
  }

  Future<Review?> getMineForActivity(int activityId, int userId) async {
    final result = await get(
      filter: {
        'page': 1,
        'pageSize': 1,
        'includeTotalCount': false,
        'activityId': activityId,
        'userId': userId,
      },
    );
    final items = result.items ?? [];
    return items.isEmpty ? null : items.first;
  }
}
