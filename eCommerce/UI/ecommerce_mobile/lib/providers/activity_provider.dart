import 'package:ecommerce_mobile/models/activity.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class ActivityProvider extends BaseProvider<Activity> {
  ActivityProvider() : super('api/Activity');

  @override
  Activity fromJson(data) {
    return Activity.fromJson(data as Map<String, dynamic>);
  }

  Future<List<Activity>> getActive({int pageSize = 50}) async {
    final result = await get(
      filter: {
        'page': 1,
        'pageSize': pageSize,
        'includeTotalCount': false,
        'Status': 'Active',
      },
    );
    return result.items ?? [];
  }

  Future<Activity?> findActiveForAnimal(int animalId) async {
    final items = await getActive();
    for (final activity in items) {
      if (activity.animalId == animalId) {
        return activity;
      }
    }
    return null;
  }
}
