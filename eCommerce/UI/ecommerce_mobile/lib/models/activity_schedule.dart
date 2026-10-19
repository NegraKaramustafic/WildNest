import 'package:ecommerce_mobile/utils/json_parsers.dart';

enum ScheduleStatus { active, cancelled }

class ActivitySchedule {
  final int id;
  final int activityId;
  final String activityName;
  final double activityPrice;
  final DateTime date;
  final Duration startTime;
  final Duration endTime;
  final int capacityLeft;
  final ScheduleStatus status;

  ActivitySchedule({
    required this.id,
    required this.activityId,
    required this.activityName,
    this.activityPrice = 0,
    required this.date,
    this.startTime = Duration.zero,
    this.endTime = Duration.zero,
    this.capacityLeft = 0,
    this.status = ScheduleStatus.active,
  });

  factory ActivitySchedule.fromJson(Map<String, dynamic> json) {
    return ActivitySchedule(
      id: asInt(json['id']),
      activityId: asInt(json['activityId']),
      activityName: asString(json['activityName']),
      activityPrice: asDouble(json['activityPrice']),
      date: parseDateOnly(json['date']),
      startTime: parseTimeOfDay(json['startTime']),
      endTime: parseTimeOfDay(json['endTime']),
      capacityLeft: asInt(json['capacityLeft']),
      status: parseScheduleStatus(json['status']),
    );
  }

  DateTime get startDateTime => date.add(startTime);
  DateTime get endDateTime => date.add(endTime);

  bool get isUpcoming => endDateTime.isAfter(DateTime.now());

  bool get isBookable =>
      status == ScheduleStatus.active && capacityLeft > 0 && isUpcoming;
}

ScheduleStatus parseScheduleStatus(dynamic value) {
  if (value is int) {
    return value == 1 ? ScheduleStatus.cancelled : ScheduleStatus.active;
  }
  switch (value?.toString().toLowerCase()) {
    case 'cancelled':
    case 'canceled':
      return ScheduleStatus.cancelled;
    default:
      return ScheduleStatus.active;
  }
}
