import 'package:ecommerce_desktop/models/schedule.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class ScheduleProvider extends BaseProvider<Schedule> {
  ScheduleProvider() : super('api/Schedule');

  @override
  Schedule fromJson(data) {
    return Schedule.fromJson(data as Map<String, dynamic>);
  }
}
