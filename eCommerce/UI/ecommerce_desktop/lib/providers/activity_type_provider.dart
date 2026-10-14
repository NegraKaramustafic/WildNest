import 'package:ecommerce_desktop/models/activity_type.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class ActivityTypeProvider extends BaseProvider<ActivityType> {
  ActivityTypeProvider() : super('api/ActivityType');

  @override
  ActivityType fromJson(data) {
    return ActivityType.fromJson(data as Map<String, dynamic>);
  }

  Future<List<ActivityType>> getAll() async {
    const pageSize = 100;
    final all = <ActivityType>[];
    var page = 1;
    while (true) {
      final result = await get(
        filter: {
          'Page': page,
          'PageSize': pageSize,
          'IncludeTotalCount': true,
        },
      );
      final items = result.items ?? [];
      all.addAll(items);
      final total = result.totalCount ?? all.length;
      if (items.isEmpty || all.length >= total) {
        return all;
      }
      page++;
    }
  }
}
