import 'package:ecommerce_desktop/utils/json_parsers.dart';

enum ScheduleStatus { active, cancelled }

class Schedule {
  final int id;
  final int activityId;
  final String activityName;
  final int activityCapacity;
  final DateTime date;
  final Duration startTime;
  final Duration endTime;
  final int capacityLeft;
  final ScheduleStatus status;

  Schedule({
    required this.id,
    required this.activityId,
    required this.activityName,
    this.activityCapacity = 0,
    required this.date,
    this.startTime = Duration.zero,
    this.endTime = Duration.zero,
    this.capacityLeft = 0,
    this.status = ScheduleStatus.active,
  });

  factory Schedule.fromJson(Map<String, dynamic> json) {
    return Schedule(
      id: asInt(json['id']),
      activityId: asInt(json['activityId']),
      activityName: asString(json['activityName']),
      activityCapacity: asInt(json['activityCapacity']),
      date: parseDateOnly(json['date']),
      startTime: parseDuration(json['startTime']),
      endTime: parseDuration(json['endTime']),
      capacityLeft: asInt(json['capacityLeft']),
      status: parseScheduleStatus(json['status']),
    );
  }
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

String scheduleStatusApiValue(ScheduleStatus status) {
  switch (status) {
    case ScheduleStatus.active:
      return 'Active';
    case ScheduleStatus.cancelled:
      return 'Cancelled';
  }
}

String scheduleStatusLabel(ScheduleStatus status) {
  switch (status) {
    case ScheduleStatus.active:
      return 'Active';
    case ScheduleStatus.cancelled:
      return 'Cancelled';
  }
}
