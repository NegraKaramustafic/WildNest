import 'package:ecommerce_mobile/models/activity_schedule.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class ScheduleProvider extends BaseProvider<ActivitySchedule> {
  ScheduleProvider() : super('api/Schedule');

  @override
  ActivitySchedule fromJson(data) {
    return ActivitySchedule.fromJson(data as Map<String, dynamic>);
  }

  Future<List<ActivitySchedule>> getForActivity(int activityId) async {
    final result = await get(
      filter: {
        'page': 1,
        'pageSize': 50,
        'includeTotalCount': false,
        'activityId': activityId,
        'Status': 'Active',
      },
    );
    final items = result.items ?? [];
    items.sort((a, b) {
      if (a.isUpcoming != b.isUpcoming) {
        return a.isUpcoming ? -1 : 1;
      }
      final byDate = a.date.compareTo(b.date);
      if (byDate != 0) return byDate;
      return a.startTime.compareTo(b.startTime);
    });
    return items;
  }

  Future<List<ActivitySchedule>> getActiveUpcoming({int pageSize = 100}) async {
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
}
