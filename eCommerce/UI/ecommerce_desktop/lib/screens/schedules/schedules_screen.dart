import 'dart:async';

import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/models/schedule.dart';
import 'package:ecommerce_desktop/providers/activity_provider.dart';
import 'package:ecommerce_desktop/providers/schedule_provider.dart';
import 'package:ecommerce_desktop/screens/schedules/schedule_form_dialog.dart';
import 'package:ecommerce_desktop/screens/schedules/schedule_week_view.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class SchedulesScreen extends StatefulWidget {
  const SchedulesScreen({super.key});

  @override
  State<SchedulesScreen> createState() => _SchedulesScreenState();
}

class _SchedulesScreenState extends State<SchedulesScreen> {
  static const _pageSize = 10;

  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  int? _activityId;
  ScheduleStatus? _status;
  DateTime? _dateFrom;
  DateTime? _dateTo;

  bool _loading = true;
  bool _loadingActivities = true;
  bool _calendar = true;
  bool _showingNearest = false;
  bool _noSchedules = false;
  String? _error;
  List<Schedule> _schedules = [];
  List<Schedule> _weekItems = [];
  List<Activity> _activities = [];
  late DateTime _weekStart;

  @override
  void initState() {
    super.initState();
    _weekStart = _monday(DateTime.now());
    _loadActivities();
    _loadWeek(allowNearest: true);
  }

  Future<void> _loadActivities() async {
    try {
      final provider = context.read<ActivityProvider>();
      final result = await provider.get(
        filter: const {'Page': 1, 'PageSize': 100},
      );
      if (!mounted) return;
      setState(() {
        _activities = result.items ?? [];
        _loadingActivities = false;
      });
    } on SessionExpiredException {
      return;
    } catch (_) {
      if (!mounted) return;
      setState(() => _loadingActivities = false);
    }
  }

  Future<void> _load() async {
    if (_dateFrom != null &&
        _dateTo != null &&
        DateUtils.dateOnly(_dateFrom!).isAfter(DateUtils.dateOnly(_dateTo!))) {
      setState(() {
        _loading = false;
        _error = 'From date cannot be after to date.';
        _schedules = [];
        _total = 0;
      });
      return;
    }

    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final provider = context.read<ScheduleProvider>();
      final result = await provider.get(
        filter: {
          'ActivityId': _activityId,
          'DateFrom': _dateFrom == null ? null : formatApiDate(_dateFrom!),
          'DateTo': _dateTo == null ? null : formatApiDate(_dateTo!),
          'Status': _status == null ? null : scheduleStatusApiValue(_status!),
          'SortBy': 'Date asc, StartTime asc',
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      final items = result.items ?? [];
      items.sort((a, b) {
        final byDate = a.date.compareTo(b.date);
        if (byDate != 0) return byDate;
        return a.startTime.compareTo(b.startTime);
      });
      setState(() {
        _schedules = items;
        _total = result.totalCount ?? items.length;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = _message(e);
        _loading = false;
      });
    }
  }

  DateTime _monday(DateTime day) {
    final date = DateUtils.dateOnly(day);
    return date.subtract(Duration(days: date.weekday - DateTime.monday));
  }

  DateTime _createDate() {
    final today = DateUtils.dateOnly(DateTime.now());
    final start = DateUtils.dateOnly(_weekStart);
    if (!start.isBefore(today)) return start;
    final end = start.add(const Duration(days: 6));
    if (!today.isAfter(end)) return today;
    return today;
  }

  Future<void> _reloadVisible() {
    if (_calendar) return _loadWeek(allowNearest: false);
    return _load();
  }

  void _showCalendar(bool calendar) {
    if (_calendar == calendar) return;
    setState(() => _calendar = calendar);
    _reloadVisible();
  }

  void _moveWeek(int deltaDays) {
    setState(() {
      _weekStart = _weekStart.add(Duration(days: deltaDays));
      _showingNearest = false;
      _noSchedules = false;
    });
    _loadWeek(allowNearest: false);
  }

  Future<List<Schedule>> _fetchRange(DateTime from, DateTime to) async {
    final provider = context.read<ScheduleProvider>();
    final all = <Schedule>[];
    var page = 1;
    while (page <= 50) {
      final result = await provider.get(
        filter: {
          'DateFrom': formatApiDate(from),
          'DateTo': formatApiDate(to),
          'SortBy': 'Date asc, StartTime asc',
          'Page': page,
          'PageSize': 100,
          'IncludeTotalCount': true,
        },
      );
      final items = result.items ?? [];
      all.addAll(items);
      final total = result.totalCount;
      if (items.length < 100 || (total != null && all.length >= total)) break;
      page++;
    }
    all.sort((a, b) {
      final byDate = a.date.compareTo(b.date);
      if (byDate != 0) return byDate;
      return a.startTime.compareTo(b.startTime);
    });
    return all;
  }

  Future<DateTime?> _nearestDate(DateTime today) async {
    final provider = context.read<ScheduleProvider>();
    final upcoming = await provider.get(
      filter: {
        'DateFrom': formatApiDate(today),
        'SortBy': 'Date asc',
        'Page': 1,
        'PageSize': 1,
      },
    );
    if (!mounted) return null;
    final previous = await provider.get(
      filter: {
        'DateTo': formatApiDate(today.subtract(const Duration(days: 1))),
        'SortBy': 'Date desc',
        'Page': 1,
        'PageSize': 1,
      },
    );
    final next = (upcoming.items?.isNotEmpty ?? false) ? upcoming.items!.first.date : null;
    final prev = (previous.items?.isNotEmpty ?? false) ? previous.items!.first.date : null;
    if (next == null) return prev;
    if (prev == null) return next;
    final nextDelta = DateUtils.dateOnly(next).difference(today).inDays.abs();
    final prevDelta = today.difference(DateUtils.dateOnly(prev)).inDays.abs();
    return prevDelta < nextDelta ? prev : next;
  }

  Future<void> _loadWeek({required bool allowNearest}) async {
    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      var start = DateUtils.dateOnly(_weekStart);
      var items = await _fetchRange(start, start.add(const Duration(days: 6)));
      if (!mounted || requestId != _requestId) return;
      var nearest = false;
      var none = false;
      if (allowNearest && items.isEmpty) {
        final today = DateUtils.dateOnly(DateTime.now());
        final found = await _nearestDate(today);
        if (!mounted || requestId != _requestId) return;
        if (found == null) {
          none = true;
        } else {
          final monday = _monday(found);
          if (!DateUtils.isSameDay(monday, start)) {
            start = monday;
            items = await _fetchRange(start, start.add(const Duration(days: 6)));
            nearest = true;
          }
        }
      }
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _weekStart = start;
        _weekItems = items;
        _showingNearest = nearest;
        _noSchedules = none;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = _message(e);
        _loading = false;
      });
    }
  }

  void _applyFilter(VoidCallback change) {
    setState(() {
      change();
      _page = 1;
    });
    _load();
  }

  void _changePage(int page) {
    setState(() => _page = page);
    _load();
  }

  Future<void> _add({DateTime? date}) async {
    final saved = await showScheduleFormDialog(context, initialDate: date ?? _createDate());
    if (saved && mounted) await _reloadVisible();
  }

  Future<void> _edit(Schedule schedule) async {
    final saved = await showScheduleFormDialog(context, schedule: schedule);
    if (saved && mounted) await _reloadVisible();
  }

  Future<void> _delete(Schedule schedule) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete schedule'),
        content: Text(
          'Delete the schedule for ${schedule.activityName} (${formatDate(schedule.date)})? '
          'An occupied schedule cannot be deleted until its reservations are cancelled.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      final provider = context.read<ScheduleProvider>();
      await provider.remove(schedule.id);
      if (mounted) await _reloadVisible();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  Future<void> _openDetails(Schedule schedule) async {
    final action = await showDialog<String>(
      context: context,
      builder: (context) {
        final canCancel = schedule.status == ScheduleStatus.active;
        return AlertDialog(
          title: Text(schedule.activityName),
          content: SizedBox(
            width: 420,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(formatDate(schedule.date)),
                const SizedBox(height: 4),
                Text('${formatClock(schedule.startTime)} – ${formatClock(schedule.endTime)}'),
                const SizedBox(height: 8),
                Text(scheduleOccupancyLabel(schedule)),
                const SizedBox(height: 12),
                ScheduleStatusChip(status: schedule.status),
                const SizedBox(height: 16),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  alignment: WrapAlignment.end,
                  children: [
                    TextButton(
                      onPressed: () => Navigator.pop(context),
                      child: const Text('Close'),
                    ),
                    FilledButton(
                      onPressed: () => Navigator.pop(context, 'edit'),
                      child: const Text('Edit'),
                    ),
                    _DialogAction(
                      label: 'Cancel schedule',
                      tooltip: canCancel
                          ? 'Cancel schedule'
                          : 'This schedule is already cancelled.',
                      enabled: canCancel,
                      onPressed: () => Navigator.pop(context, 'cancel'),
                    ),
                    _DialogAction(
                      label: 'Delete',
                      tooltip:
                          'An occupied schedule cannot be deleted until its reservations are cancelled.',
                      enabled: true,
                      danger: true,
                      onPressed: () => Navigator.pop(context, 'delete'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        );
      },
    );
    if (!mounted || action == null) return;
    switch (action) {
      case 'edit':
        await _edit(schedule);
        break;
      case 'cancel':
        await _cancel(schedule);
        break;
      case 'delete':
        await _delete(schedule);
        break;
    }
  }

  Future<void> _cancel(Schedule schedule) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel schedule'),
        content: const Text(
          'Cancelling this schedule also cancels its reservations. '
          'Payments are refunded the same way as when a customer cancels a reservation. Continue?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Cancel schedule'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      final provider = context.read<ScheduleProvider>();
      await provider.update(schedule.id, {
        'activityId': schedule.activityId,
        'date': formatApiDate(schedule.date),
        'startTime': formatApiClock(schedule.startTime),
        'endTime': formatApiClock(schedule.endTime),
        'capacityLeft': schedule.capacityLeft,
        'status': scheduleStatusApiValue(ScheduleStatus.cancelled),
      });
      if (mounted) await _reloadVisible();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  Future<void> _pickDate({required bool from}) async {
    final current = from ? _dateFrom : _dateTo;
    final today = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: current ?? DateTime(today.year, today.month, today.day),
      firstDate: DateTime(today.year - 2),
      lastDate: DateTime(today.year + 3, 12, 31),
    );
    if (picked == null || !mounted) return;
    final day = DateTime(picked.year, picked.month, picked.day);
    _applyFilter(() {
      if (from) {
        _dateFrom = day;
      } else {
        _dateTo = day;
      }
    });
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  String _message(Object error) {
    return error.toString().replaceFirst('Bad state: ', '');
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Schedules',
                      style: TextStyle(
                        fontSize: 26,
                        fontWeight: FontWeight.w700,
                        color: AppColors.text,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'Activities arranged by day for the selected week',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              FilledButton.icon(
                onPressed: () => _add(),
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Add schedule'),
              ),
            ],
          ),
          const SizedBox(height: 12),
          SegmentedButton<bool>(
            showSelectedIcon: false,
            segments: const [
              ButtonSegment<bool>(
                value: true,
                label: Text('Calendar'),
                icon: Icon(Icons.calendar_view_week, size: 18),
              ),
              ButtonSegment<bool>(
                value: false,
                label: Text('List'),
                icon: Icon(Icons.view_list, size: 18),
              ),
            ],
            selected: {_calendar},
            onSelectionChanged: (selection) => _showCalendar(selection.first),
          ),
          const SizedBox(height: 18),
          if (_calendar) _weekToolbar(),
          if (!_calendar)
          Wrap(
            spacing: 12,
            runSpacing: 12,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              SizedBox(width: 240, child: _activityFilter()),
              _dateButton(
                label: 'From date',
                value: _dateFrom,
                onPick: () => _pickDate(from: true),
                onClear: _dateFrom == null ? null : () => _applyFilter(() => _dateFrom = null),
              ),
              _dateButton(
                label: 'To date',
                value: _dateTo,
                onPick: () => _pickDate(from: false),
                onClear: _dateTo == null ? null : () => _applyFilter(() => _dateTo = null),
              ),
              SizedBox(width: 180, child: _statusFilter()),
            ],
          ),
          const SizedBox(height: 16),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _activityFilter() {
    if (_loadingActivities) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Activity', isDense: true),
        child: Text('Loading...'),
      );
    }

    final selected = _activities.any((activity) => activity.id == _activityId)
        ? _activityId
        : null;
    return DropdownButtonFormField<int?>(
      key: ValueKey('activity-$selected-${_activities.length}'),
      initialValue: selected,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Activity', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All activities')),
        for (final activity in _activities)
          DropdownMenuItem(value: activity.id, child: Text(activity.name)),
      ],
      onChanged: (value) => _applyFilter(() => _activityId = value),
    );
  }

  Widget _statusFilter() {
    return DropdownButtonFormField<ScheduleStatus?>(
      key: ValueKey('schedule-status-$_status'),
      initialValue: _status,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Status', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All statuses')),
        for (final status in ScheduleStatus.values)
          DropdownMenuItem(value: status, child: Text(scheduleStatusLabel(status))),
      ],
      onChanged: (value) => _applyFilter(() => _status = value),
    );
  }

  Widget _dateButton({
    required String label,
    required DateTime? value,
    required VoidCallback onPick,
    required VoidCallback? onClear,
  }) {
    return SizedBox(
      width: 210,
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          isDense: true,
          suffixIcon: onClear == null
              ? const Icon(Icons.calendar_today_outlined, size: 18)
              : IconButton(
                  tooltip: 'Clear',
                  onPressed: onClear,
                  icon: const Icon(Icons.close, size: 18),
                ),
        ),
        child: InkWell(
          onTap: onPick,
          child: Text(value == null ? 'Select' : formatDate(value)),
        ),
      ),
    );
  }

  Widget _weekToolbar() {
    final start = DateUtils.dateOnly(_weekStart);
    final end = start.add(const Duration(days: 6));
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 4,
          runSpacing: 8,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            IconButton(
              tooltip: 'Previous week',
              onPressed: () => _moveWeek(-7),
              icon: const Icon(Icons.chevron_left),
            ),
            Text(
              '${formatDate(start)} – ${formatDate(end)}',
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
            IconButton(
              tooltip: 'Next week',
              onPressed: () => _moveWeek(7),
              icon: const Icon(Icons.chevron_right),
            ),
            OutlinedButton(
              onPressed: () {
                setState(() {
                  _weekStart = _monday(DateTime.now());
                  _showingNearest = false;
                  _noSchedules = false;
                });
                _loadWeek(allowNearest: false);
              },
              child: const Text('Today'),
            ),
          ],
        ),
        if (_showingNearest) ...[
          const SizedBox(height: 8),
          _Notice(
            text:
                'Opened the nearest week with schedules. Today is ${formatDate(DateTime.now())}.',
          ),
        ],
        if (_noSchedules) ...[
          const SizedBox(height: 8),
          const _Notice(
            text: 'No schedules yet. Add a schedule to place it on the calendar.',
          ),
        ] else if (!_showingNearest && !_loading && _error == null && _weekItems.isEmpty) ...[
          const SizedBox(height: 8),
          const _Notice(text: 'No activities this week.'),
        ],
        const SizedBox(height: 12),
      ],
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return _ErrorState(
        message: _error!,
        onRetry: () => _calendar ? _loadWeek(allowNearest: false) : _load(),
      );
    }
    if (_calendar) {
      return ScheduleWeekView(
        weekStart: _weekStart,
        schedules: _weekItems,
        onOpen: _openDetails,
        onCreate: (day) => _add(date: day),
      );
    }
    return _SchedulesTable(
      schedules: _schedules,
      page: _page,
      pageSize: _pageSize,
      total: _total,
      onPage: _changePage,
      onEdit: _edit,
      onDelete: _delete,
      onCancel: _cancel,
    );
  }
}

class _SchedulesTable extends StatelessWidget {
  const _SchedulesTable({
    required this.schedules,
    required this.page,
    required this.pageSize,
    required this.total,
    required this.onPage,
    required this.onEdit,
    required this.onDelete,
    required this.onCancel,
  });

  final List<Schedule> schedules;
  final int page;
  final int pageSize;
  final int total;
  final ValueChanged<int> onPage;
  final ValueChanged<Schedule> onEdit;
  final ValueChanged<Schedule> onDelete;
  final ValueChanged<Schedule> onCancel;

  @override
  Widget build(BuildContext context) {
    final from = total == 0 ? 0 : ((page - 1) * pageSize) + 1;
    final to = (page * pageSize) > total ? total : page * pageSize;

    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          const _HeaderRow(),
          const Divider(height: 1),
          Expanded(
            child: schedules.isEmpty
                ? const Center(
                    child: Text(
                      'No schedules to show.',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  )
                : ListView.separated(
                    itemCount: schedules.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final schedule = schedules[index];
                      return _ScheduleRow(
                        schedule: schedule,
                        onEdit: () => onEdit(schedule),
                        onDelete: () => onDelete(schedule),
                        onCancel: () => onCancel(schedule),
                      );
                    },
                  ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            child: Row(
              children: [
                Text(
                  total == 0 ? '0 of 0' : '$from–$to of $total',
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
                const Spacer(),
                IconButton(
                  tooltip: 'Previous',
                  onPressed: page > 1 ? () => onPage(page - 1) : null,
                  icon: const Icon(Icons.chevron_left),
                ),
                Text('$page'),
                IconButton(
                  tooltip: 'Next',
                  onPressed: page * pageSize < total ? () => onPage(page + 1) : null,
                  icon: const Icon(Icons.chevron_right),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _HeaderRow extends StatelessWidget {
  const _HeaderRow();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(horizontal: 18, vertical: 14),
      child: Row(
        children: [
          Expanded(flex: 3, child: _HeaderLabel('Activity')),
          Expanded(flex: 2, child: _HeaderLabel('Date')),
          Expanded(flex: 2, child: _HeaderLabel('Time')),
          Expanded(flex: 2, child: _HeaderLabel('Capacity')),
          Expanded(flex: 2, child: _HeaderLabel('Status')),
          SizedBox(width: 148, child: _HeaderLabel('Actions')),
        ],
      ),
    );
  }
}

class _HeaderLabel extends StatelessWidget {
  const _HeaderLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text.toUpperCase(),
      style: const TextStyle(
        fontSize: 11,
        letterSpacing: 0.6,
        fontWeight: FontWeight.w600,
        color: AppColors.textMuted,
      ),
    );
  }
}

class _ScheduleRow extends StatelessWidget {
  const _ScheduleRow({
    required this.schedule,
    required this.onEdit,
    required this.onDelete,
    required this.onCancel,
  });

  final Schedule schedule;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    final canCancel = schedule.status == ScheduleStatus.active;
    final capacity = schedule.activityCapacity > 0
        ? '${schedule.capacityLeft} / ${schedule.activityCapacity}'
        : '${schedule.capacityLeft}';

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
      child: Row(
        children: [
          Expanded(
            flex: 3,
            child: Text(
              schedule.activityName,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
          Expanded(flex: 2, child: Text(formatDate(schedule.date))),
          Expanded(
            flex: 2,
            child: Text(
              '${formatClock(schedule.startTime)}–${formatClock(schedule.endTime)}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          Expanded(flex: 2, child: Text(capacity)),
          Expanded(
            flex: 2,
            child: Align(
              alignment: Alignment.centerLeft,
              child: ScheduleStatusChip(status: schedule.status),
            ),
          ),
          SizedBox(
            width: 148,
            child: Row(
              children: [
                _IconAction(
                  icon: Icons.edit_outlined,
                  color: AppColors.textMuted,
                  tooltip: 'Edit',
                  onPressed: onEdit,
                ),
                const SizedBox(width: 4),
                _IconAction(
                  icon: Icons.event_busy_outlined,
                  color: AppColors.warningText,
                  tooltip: canCancel
                      ? 'Cancel schedule'
                      : 'This schedule is already cancelled.',
                  enabled: canCancel,
                  onPressed: onCancel,
                ),
                const SizedBox(width: 4),
                _IconAction(
                  icon: Icons.delete_outline,
                  color: AppColors.danger,
                  tooltip: 'Delete',
                  onPressed: onDelete,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Notice extends StatelessWidget {
  const _Notice({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: AppColors.warningBg,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AppColors.warningBorder),
      ),
      child: Text(text, style: const TextStyle(color: AppColors.warningText)),
    );
  }
}

class _DialogAction extends StatelessWidget {
  const _DialogAction({
    required this.label,
    required this.tooltip,
    required this.enabled,
    required this.onPressed,
    this.danger = false,
  });

  final String label;
  final String tooltip;
  final bool enabled;
  final VoidCallback onPressed;
  final bool danger;

  @override
  Widget build(BuildContext context) {
    final ButtonStyle style;
    if (!enabled) {
      style = FilledButton.styleFrom(
        backgroundColor: AppColors.border,
        foregroundColor: AppColors.textMuted,
      );
    } else if (danger) {
      style = FilledButton.styleFrom(backgroundColor: AppColors.danger);
    } else {
      style = FilledButton.styleFrom();
    }
    return Tooltip(
      message: tooltip,
      child: FilledButton(
        style: style,
        onPressed: enabled ? onPressed : () {},
        child: Text(label),
      ),
    );
  }
}

class _IconAction extends StatelessWidget {
  const _IconAction({
    required this.icon,
    required this.color,
    required this.tooltip,
    required this.onPressed,
    this.enabled = true,
  });

  final IconData icon;
  final Color color;
  final String tooltip;
  final VoidCallback onPressed;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkWell(
        onTap: enabled ? onPressed : () {},
        borderRadius: BorderRadius.circular(8),
        child: Container(
          width: 34,
          height: 34,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: AppColors.border),
          ),
          child: Icon(
            icon,
            size: 18,
            color: enabled ? color : color.withValues(alpha: 0.35),
          ),
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
        ],
      ),
    );
  }
}
