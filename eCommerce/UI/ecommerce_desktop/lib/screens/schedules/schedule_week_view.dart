import 'package:ecommerce_desktop/models/schedule.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/status_chip.dart';
import 'package:flutter/material.dart';

const _dayNames = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

String scheduleOccupancyLabel(Schedule schedule) {
  final total = schedule.activityCapacity;
  if (total <= 0) {
    return '${schedule.capacityLeft} left';
  }
  final booked = (total - schedule.capacityLeft).clamp(0, total);
  return '$booked of $total booked';
}

class ScheduleStatusChip extends StatelessWidget {
  const ScheduleStatusChip({super.key, required this.status});

  final ScheduleStatus status;

  @override
  Widget build(BuildContext context) {
    switch (status) {
      case ScheduleStatus.active:
        return const StatusChip(active: true, label: 'Active');
      case ScheduleStatus.cancelled:
        return const StatusChip(
          active: false,
          label: 'Cancelled',
          background: AppColors.danger,
          foreground: Colors.white,
        );
    }
  }
}

class ScheduleWeekView extends StatelessWidget {
  const ScheduleWeekView({
    super.key,
    required this.weekStart,
    required this.schedules,
    required this.onOpen,
    required this.onCreate,
  });

  final DateTime weekStart;
  final List<Schedule> schedules;
  final ValueChanged<Schedule> onOpen;
  final ValueChanged<DateTime> onCreate;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        const columnWidth = 176.0;
        final width = constraints.maxWidth < columnWidth * 7
            ? columnWidth * 7
            : constraints.maxWidth;
        return SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: SizedBox(
            width: width,
            height: constraints.maxHeight,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (var index = 0; index < 7; index++)
                  Expanded(
                    child: _DayColumn(
                      day: DateUtils.dateOnly(weekStart).add(Duration(days: index)),
                      schedules: _forDay(index),
                      onOpen: onOpen,
                      onCreate: onCreate,
                    ),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }

  List<Schedule> _forDay(int index) {
    final day = DateUtils.dateOnly(weekStart).add(Duration(days: index));
    final items = schedules
        .where((schedule) => DateUtils.isSameDay(schedule.date, day))
        .toList();
    items.sort((a, b) => a.startTime.compareTo(b.startTime));
    return items;
  }
}

class _DayColumn extends StatelessWidget {
  const _DayColumn({
    required this.day,
    required this.schedules,
    required this.onOpen,
    required this.onCreate,
  });

  final DateTime day;
  final List<Schedule> schedules;
  final ValueChanged<Schedule> onOpen;
  final ValueChanged<DateTime> onCreate;

  @override
  Widget build(BuildContext context) {
    final today = DateUtils.dateOnly(DateTime.now());
    final isToday = DateUtils.isSameDay(day, today);
    final past = day.isBefore(today);
    return Container(
      margin: const EdgeInsets.only(right: 8),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isToday ? AppColors.green : AppColors.border,
          width: isToday ? 1.5 : 1,
        ),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(10, 10, 4, 8),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        _dayNames[day.weekday - 1],
                        style: TextStyle(
                          fontWeight: FontWeight.w700,
                          color: isToday ? AppColors.greenDark : AppColors.text,
                        ),
                      ),
                      Text(
                        formatDate(day),
                        style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                      ),
                    ],
                  ),
                ),
                Tooltip(
                  message: past
                      ? 'Schedules cannot be created in the past.'
                      : 'Add a schedule on this day.',
                  child: IconButton(
                    onPressed: past ? () {} : () => onCreate(day),
                    icon: Icon(
                      Icons.add,
                      size: 18,
                      color: past ? AppColors.textMuted.withValues(alpha: 0.45) : AppColors.green,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: schedules.isEmpty
                ? const Center(
                    child: Padding(
                      padding: EdgeInsets.all(12),
                      child: Text(
                        'No activities.',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: AppColors.textMuted, fontSize: 13),
                      ),
                    ),
                  )
                : ListView.separated(
                    padding: const EdgeInsets.all(8),
                    itemCount: schedules.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 8),
                    itemBuilder: (context, index) {
                      return _SlotCard(
                        schedule: schedules[index],
                        onOpen: () => onOpen(schedules[index]),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}

class _SlotCard extends StatelessWidget {
  const _SlotCard({required this.schedule, required this.onOpen});

  final Schedule schedule;
  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context) {
    final active = schedule.status == ScheduleStatus.active;
    return Material(
      color: active ? AppColors.successBg : AppColors.errorBg,
      borderRadius: BorderRadius.circular(10),
      child: InkWell(
        onTap: onOpen,
        borderRadius: BorderRadius.circular(10),
        child: Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: active ? AppColors.successBorder : AppColors.errorBorder),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '${formatClock(schedule.startTime)} – ${formatClock(schedule.endTime)}',
                style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 4),
              Text(
                schedule.activityName,
                softWrap: true,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 4),
              Text(
                scheduleOccupancyLabel(schedule),
                softWrap: true,
                style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
              ),
              const SizedBox(height: 8),
              ScheduleStatusChip(status: schedule.status),
            ],
          ),
        ),
      ),
    );
  }
}
